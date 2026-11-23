using System.Collections.Concurrent;
using System.Diagnostics;
using MelonUI.Base;
using MelonUI.Enums;

namespace MelonUI.Managers;

/// <summary>Owns a terminal UI, its element tree, and the loop that updates it.</summary>
public class ConsoleWindowManager : IAsyncDisposable
{
    private sealed record UiWork(Action Action, TaskCompletionSource? Completion);
    private readonly object _lifetime = new();
    private readonly ConcurrentQueue<UiWork> _work = new();
    private readonly AutoResetEvent _wake = new(false);
    private readonly IConsoleTerminal _terminal;
    private readonly Dictionary<UIElement, ConsoleBuffer> _buffers = new();
    private readonly List<KeyboardControl> _keyboardControls = new();
    private readonly List<PopupHandle> _popups = new();
    private PopupHandle? _lastModal;
    private CancellationTokenSource? _stop;
    private Task? _run;
    private Task? _dispose;
    private volatile bool _isRunning;
    private volatile bool _isDisposed;
    private int _uiThread;
    private string _title = "";
    private string? _appliedTitle;
    private int _targetFrameRate = 60;
    private bool _platformRenderer = true;
    private long _renderVersion;
    internal void RequestRender() => _renderVersion++;

    public ConsoleWindowManager() : this(new ConsoleTerminal()) { }
    internal ConsoleWindowManager(IConsoleTerminal terminal)
    {
        _terminal = terminal;
        Elements = new ElementCollection(this);
    }

    public ElementCollection Elements { get; }
    public UIElement? FocusedElement { get; private set; }
    public bool IsRunning => _isRunning;
    public bool IsDisposed => _isDisposed;
    internal bool IsStopping => _stop?.IsCancellationRequested == true || _isDisposed || _dispose is not null;
    public long FrameNumber { get; private set; }
    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }
    internal bool IsCapturing { get; private set; }
    public bool CheckAccess() => !_isRunning || Environment.CurrentManagedThreadId == _uiThread;
    public bool EnableSystemFocusControls { get; set; } = true;
    public bool EnableGlobalControls { get; set; } = true;

    /// <summary>The requested console-window/terminal title. Does not draw inside the UI.</summary>
    public string Title
    {
        get => _title;
        set { VerifyAccess(); _title = value ?? ""; }
    }

    public int TargetFrameRate
    {
        get => _targetFrameRate;
        set
        {
            VerifyAccess();
            if (value is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(value), "Use a frame rate from 1 to 1000.");
            _targetFrameRate = value;
        }
    }

    public bool UsePlatformSpecificRenderer
    {
        get => _platformRenderer;
        set { VerifyAccess(); if (_platformRenderer != value) { _platformRenderer = value; RequestRender(); } }
    }

    public event EventHandler<UiUpdateEventArgs>? Updating;
    public event EventHandler? FrameRendered;
    public event EventHandler<UiExceptionEventArgs>? UnhandledException;

    internal void VerifyAccess()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (!CheckAccess()) throw new InvalidOperationException("Use Post or InvokeAsync to update attached UI from a background thread.");
    }

    /// <summary>Runs until stopped or cancelled, including terminal restoration and worker shutdown.</summary>
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifetime)
        {
            ObjectDisposedException.ThrowIf(_isDisposed || _dispose is not null, this);
            if (_isRunning) throw new InvalidOperationException("This window is already running.");
            if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;
            _stop?.Dispose();
            _stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isRunning = true;
            _run = Task.Factory.StartNew(() => RunLoop(_stop.Token), CancellationToken.None,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
            return _run;
        }
    }

    public void Run(CancellationToken cancellationToken = default) => RunAsync(cancellationToken).GetAwaiter().GetResult();

    public void Stop()
    {
        lock (_lifetime)
        {
            if (!_isRunning) return;
            _stop!.Cancel();
            _wake.Set();
        }
    }

    /// <summary>Queues one short UI action. Available while RunAsync is active.</summary>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Enqueue(new UiWork(action, null));
    }

    /// <summary>Completes after the action runs, or faults with its exception.</summary>
    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_isRunning && CheckAccess())
        {
            try { action(); return Task.CompletedTask; }
            catch (Exception ex) { return Task.FromException(ex); }
        }
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Enqueue(new UiWork(action, completion));
        return completion.Task;
    }

    private void Enqueue(UiWork work)
    {
        lock (_lifetime)
        {
            ObjectDisposedException.ThrowIf(_isDisposed || _dispose is not null, this);
            if (!_isRunning || _stop!.IsCancellationRequested)
                throw new InvalidOperationException("UI work can only be queued while RunAsync is active and not stopping.");
            _work.Enqueue(work);
            _wake.Set();
        }
    }

    private void RunLoop(CancellationToken cancellationToken)
    {
        _uiThread = Environment.CurrentManagedThreadId;
        try
        {
            _terminal.Begin();
            _appliedTitle = null;
            var clock = Stopwatch.StartNew();
            var previousUpdate = TimeSpan.Zero;
            var nextFrame = TimeSpan.Zero;
            long presentedVersion = -1;
            (int Width, int Height) presentedSize = (-1, -1);
            while (!cancellationToken.IsCancellationRequested)
            {
                for (var count = 0; count < 256 && !cancellationToken.IsCancellationRequested && _work.TryDequeue(out var work); count++)
                {
                    try { work.Action(); work.Completion?.TrySetResult(); }
                    catch (Exception ex)
                    {
                        if (work.Completion is not null) work.Completion.TrySetException(ex);
                        else ReportException(ex);
                    }
                }
                for (var count = 0; count < 64 && !cancellationToken.IsCancellationRequested && _terminal.TryReadKey(out var key); count++)
                {
                    try { HandleInput(key); }
                    catch (Exception ex) { ReportException(ex); }
                }
                if (cancellationToken.IsCancellationRequested) break;

                var now = clock.Elapsed;
                if (now >= nextFrame)
                {
                    try
                    {
                        Updating?.Invoke(this, new UiUpdateEventArgs(now - previousUpdate));
                        previousUpdate = now;
                        if (cancellationToken.IsCancellationRequested) break;
                        if (_appliedTitle != Title) { _terminal.SetTitle(Title); _appliedTitle = Title; }
                        var size = _terminal.GetSize();
                        if (presentedVersion != _renderVersion || size != presentedSize || Elements.Any(e => e.IsVisible && (e.NeedsRecalculation || e.RequiresContinuousRendering)))
                        {
                            _terminal.Present(ComposeFrame(size.Width, size.Height), UsePlatformSpecificRenderer);
                            presentedVersion = _renderVersion; presentedSize = size;
                            FrameNumber++;
                            FrameRendered?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    catch (Exception ex) { ReportException(ex); }
                    nextFrame = clock.Elapsed + TimeSpan.FromSeconds(1d / TargetFrameRate);
                }
                var wait = Math.Clamp((nextFrame - clock.Elapsed).TotalMilliseconds, 1, 16);
                _wake.WaitOne((int)Math.Ceiling(wait));
            }
        }
        finally
        {
            try { _terminal.End(); }
            finally
            {
                lock (_lifetime)
                {
                    _isRunning = false;
                    _uiThread = 0;
                    while (_work.TryDequeue(out var work)) work.Completion?.TrySetCanceled();
                }
            }
        }
    }

    private void ReportException(Exception exception)
    {
        var args = new UiExceptionEventArgs(exception);
        UnhandledException?.Invoke(this, args);
        if (!args.Handled) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <summary>Captures the same composition used for rendering. Can be used before RunAsync.</summary>
    public string CaptureText(bool includeColor = false, int? width = null, int? height = null, bool includeSensitive = false)
    {
        VerifyAccess();
        var size = width.HasValue && height.HasValue ? (width.Value, height.Value) : _terminal.GetSize();
        bool previous = IsCapturing;
        IsCapturing = !includeSensitive;
        try { return ComposeFrame(width ?? size.Item1, height ?? size.Item2).Screenshot(includeColor); }
        finally { IsCapturing = previous; }
    }

    internal ConsoleBuffer ComposeFrame(int width, int height)
    {
        VerifyAccess();
        if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width), "Frame dimensions must be positive.");
        ViewportWidth = width; ViewportHeight = height;
        var frame = new ConsoleBuffer(width, height);
        var roots = Elements.Where(x => x.IsVisible).OrderBy(x => x.Z).ToArray();
        var changed = new HashSet<UIElement>();
        foreach (var element in roots)
        {
            if (element.IsDisposed || element.ParentWindow != this || !element.IsVisible) continue;
            var previous = (element.ActualWidth, element.ActualHeight, element.ActualX, element.ActualY);
            element.CalculateLayout(0, 0, width, height);
            if (previous != (element.ActualWidth, element.ActualHeight, element.ActualX, element.ActualY)) changed.Add(element);
        }
        TreeChanged();
        var popupRoots = _popups.Where(p => p.IsActive && p.Content.IsVisible).Select(p => p.Content).ToArray();
        foreach (var element in roots.Except(popupRoots).Concat(popupRoots))
        {
            if (element.IsDisposed || element.ParentWindow != this || !element.IsVisible || element.ActualWidth == 0 || element.ActualHeight == 0) continue;
            if (element.RequiresContinuousRendering || changed.Contains(element) || element.NeedsRecalculation || !_buffers.TryGetValue(element, out var buffer))
            {
                buffer = element.Render();
                if (element.EnableCaching && element.ParentWindow == this) _buffers[element] = buffer;
            }
            frame.WriteBuffer(element.ActualX, element.ActualY, buffer, element.RespectBackgroundOnDraw);
        }
        return frame;
    }

    public T? Find<T>(string name) where T : UIElement { VerifyAccess(); return ElementTree.Find<T>(Elements, name); }
    public UIElement? FindById(string id) { VerifyAccess(); return ElementTree.Descendants(Elements).FirstOrDefault(x => x.UID == id); }

    private UIElement? ModalRoot => _popups.LastOrDefault(p => p.IsActive && p.IsModal &&
        p.Content.IsEffectivelyVisible && p.Content.IsEffectivelyEnabled)?.Content;
    private IEnumerable<UIElement> FocusRoots => ModalRoot is { } modal ? [modal] : Elements;

    internal void TreeChanged()
    {
        _popups.RemoveAll(p => !p.IsActive);
        var modal = _popups.LastOrDefault(p => p.IsModal && p.Content.IsEffectivelyVisible && p.Content.IsEffectivelyEnabled);
        if (!ReferenceEquals(modal, _lastModal))
        {
            var previous = _lastModal?.PreviousFocus;
            _lastModal = modal;
            if (previous is not null && Focusable(FocusRoots).Contains(previous)) SetFocus(previous);
        }
        if (FocusedElement is null || FocusedElement.ParentWindow != this || !FocusedElement.CanFocus ||
            (ModalRoot is { } root && !FocusedElement.IsWithin(root)) || !Focusable(FocusRoots).Contains(FocusedElement))
            SetFocus(Focusable(FocusRoots).FirstOrDefault());
    }

    internal void TreeRemoved(UIElement subtree)
    {
        foreach (var element in ElementTree.Descendants([subtree]))
        {
            _buffers.Remove(element);
            element.IsFocused = false;
            if (ReferenceEquals(FocusedElement, element)) FocusedElement = null;
        }
        TreeChanged();
    }

    private static IEnumerable<UIElement> Focusable(IEnumerable<UIElement> elements)
    {
        foreach (var element in elements.OrderBy(e => e.TabIndex))
        {
            if (!element.IsEffectivelyVisible || !element.IsEffectivelyEnabled) continue;
            if (element.CanFocus) yield return element;
            foreach (var child in Focusable(element.FocusChildren)) yield return child;
        }
    }

    public void Focus(UIElement element)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        if (element.ParentWindow != this || !element.IsEffectivelyVisible || !element.IsEffectivelyEnabled ||
            (ModalRoot is { } modal && !element.IsWithin(modal)))
            throw new InvalidOperationException("Focus requires visible, enabled content within the active focus scope.");
        var target = Focusable([element]).FirstOrDefault();
        if (target is not null)
        {
            if (!Focusable(FocusRoots).Contains(target)) throw new InvalidOperationException("This element is managed as part of a composite control and is not an independent focus stop.");
            SetFocus(target);
        }
    }

    private void SetFocus(UIElement? element)
    {
        if (ReferenceEquals(FocusedElement, element)) return;
        var previous = FocusedElement;
        FocusedElement = element;
        if (previous is { IsDisposed: false }) previous.IsFocused = false;
        if (element is not null) element.IsFocused = true;
    }

    public void BringToFront(UIElement element) => Reorder(element, toFront: true);
    public void SendToBack(UIElement element) => Reorder(element, toFront: false);
    private void Reorder(UIElement element, bool toFront)
    {
        VerifyAccess();
        if (element.ParentWindow != this) throw new InvalidOperationException("The element belongs to another tree.");
        var siblings = element.OwnerCollection!.OrderBy(x => x.Z).Where(x => x != element).ToList();
        if (toFront) siblings.Add(element); else siblings.Insert(0, element);
        for (var i = 0; i < siblings.Count; i++) siblings[i].Z = i;
    }

    public PopupHandle ShowPopup(UIElement content, bool isModal = true)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(content);
        if (!content.IsVisible || !content.IsEnabled) throw new ArgumentException("Popup content must be visible and enabled.", nameof(content));
        var previousFocus = FocusedElement;
        Elements.Add(content);
        BringToFront(content);
        var popup = new PopupHandle(this, content, previousFocus, isModal);
        _popups.Add(popup);
        Focus(content);
        TreeChanged();
        return popup;
    }

    internal void RestorePopupFocus(UIElement? previous)
    {
        TreeChanged();
        if (previous is not null && Focusable(FocusRoots).Contains(previous)) SetFocus(previous);
    }

    public void RegisterKeyboardControl(ConsoleKey key, Action action, string description,
        bool requireShift = false, bool requireControl = false, bool requireAlt = false) =>
        RegisterKeyboardControl(new KeyboardControl
        {
            Key = key, Action = action, Description = description,
            RequireShift = requireShift, RequireControl = requireControl, RequireAlt = requireAlt
        });

    public void RegisterKeyboardControl(KeyboardControl control)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(control);
        _keyboardControls.Add(control);
    }
    public bool UnregisterKeyboardControl(KeyboardControl control) { VerifyAccess(); return _keyboardControls.Remove(control); }
    public IEnumerable<KeyboardControl> GetKeyboardControls() => _keyboardControls.ToArray();

    internal void HandleInput(ConsoleKeyInfo key)
    {
        VerifyAccess();
        if (ViewportWidth > 0 && ViewportHeight > 0)
            foreach (var root in Elements.ToArray())
                if (!root.IsDisposed && root.ParentWindow == this) root.CalculateLayout(0, 0, ViewportWidth, ViewportHeight);
        TreeChanged();
        if (EnableSystemFocusControls && key.Modifiers.HasFlag(ConsoleModifiers.Alt) &&
            !key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            var direction = key.Key switch
            {
                ConsoleKey.UpArrow => Direction.Up, ConsoleKey.DownArrow => Direction.Down,
                ConsoleKey.LeftArrow => Direction.Left, ConsoleKey.RightArrow => Direction.Right,
                ConsoleKey.Oem3 => Direction.Any, _ => (Direction?)null
            };
            if (direction.HasValue) { MoveFocus(direction.Value, key.Modifiers.HasFlag(ConsoleModifiers.Shift)); return; }
        }
        var scope = ModalRoot;
        for (var current = FocusedElement; current is not null; current = current.Parent)
        {
            if (current.TryHandleKey(key)) { TreeChanged(); return; }
            if (ReferenceEquals(current, scope)) break;
        }
        if (EnableSystemFocusControls && key.Key == ConsoleKey.Tab && !key.Modifiers.HasFlag(ConsoleModifiers.Alt))
        {
            MoveFocus(Direction.Any, key.Modifiers.HasFlag(ConsoleModifiers.Shift));
            return;
        }
        if (scope is not null)
        {
            if (key.Key == ConsoleKey.Escape) _popups.Last(p => p.IsActive && p.Content == scope).Close();
            return; // A modal cannot invoke background window shortcuts.
        }
        if (EnableGlobalControls)
            foreach (var control in _keyboardControls.ToArray())
                if (control.Matches(key) && control.Action is { } action) { action(); break; }
        TreeChanged();
    }

    private void MoveFocus(Direction direction, bool reverse = false)
    {
        var elements = Focusable(FocusRoots).ToArray();
        if (elements.Length == 0) { SetFocus(null); return; }
        if (direction == Direction.Any || FocusedElement is null)
        {
            int current = Array.IndexOf(elements, FocusedElement);
            int next = current < 0 ? (reverse ? elements.Length - 1 : 0) : (current + (reverse ? -1 : 1) + elements.Length) % elements.Length;
            SetFocus(elements[next]);
            return;
        }
        var position = FocusedElement.GetAbsolutePosition();
        double x = position.X + FocusedElement.ActualWidth / 2d;
        double y = position.Y + FocusedElement.ActualHeight / 2d;
        var nearest = elements.Where(e => e != FocusedElement).Select(e =>
        {
            var point = e.GetAbsolutePosition();
            double dx = point.X + e.ActualWidth / 2d - x;
            double dy = point.Y + e.ActualHeight / 2d - y;
            double primary = direction switch { Direction.Left => -dx, Direction.Right => dx, Direction.Up => -dy, _ => dy };
            double perpendicular = direction is Direction.Left or Direction.Right ? Math.Abs(dy) : Math.Abs(dx);
            return (Element: e, Primary: primary, Perpendicular: perpendicular);
        }).Where(e => e.Primary > 0)
          .OrderBy(e => e.Perpendicular / e.Primary).ThenBy(e => e.Primary).FirstOrDefault();
        if (nearest.Element is not null) SetFocus(nearest.Element);
    }

    public ValueTask DisposeAsync()
    {
        lock (_lifetime)
        {
            if (_dispose is not null) return new ValueTask(_dispose);
            if (_isRunning && CheckAccess())
                throw new InvalidOperationException("Call Stop in UI callbacks and dispose the window after RunAsync completes.");
            Stop();
            _dispose = DisposeCoreAsync();
            return new ValueTask(_dispose);
        }
    }

    private async Task DisposeCoreAsync()
    {
        Exception? failure = null;
        try { if (_run is not null) await _run.ConfigureAwait(false); }
        catch (Exception ex) { failure = ex; }
        try
        {
            foreach (var element in Elements.ToArray())
            {
                try { element.Dispose(); }
                catch (Exception ex) { failure = failure is null ? ex : new AggregateException(failure, ex); }
            }
            Elements.Clear();
            _buffers.Clear();
            _keyboardControls.Clear();
            _popups.Clear();
            Updating = null;
            FrameRendered = null;
            UnhandledException = null;
        }
        finally
        {
            _stop?.Dispose();
            _wake.Dispose();
            _isDisposed = true;
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
