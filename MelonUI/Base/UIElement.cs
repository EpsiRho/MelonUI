using MelonUI.Attributes;
using MelonUI.Enums;
using MelonUI.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MelonUI.Base
{
    public abstract partial class UIElement : IDisposable, IBindingHost
    {
        // CWM Interactions
        public virtual bool DefaultKeyControl { get; set; } = true;
        public virtual bool EnableCaching { get; set; } = true;
        public virtual bool NeedsRecalculation { get; set; } = true;
        public virtual bool RespectBackgroundOnDraw { get; set; } = true;
        private bool _IsFocused = false;
        public bool IsFocused { get => _IsFocused; internal set => SetBoundValue(nameof(IsFocused), value, ref _IsFocused); }
        [Binding]
        private bool _ConsiderForFocus = true;
        [Binding]
        private bool _IsEnabled = true;
        [Binding]
        private int _TabIndex = 0;
        [Binding]
        private bool _LockControls = false;
        [Binding]
        private bool _IsVisible = true;
        [Binding]
        private List<KeyboardControl> _KeyboardControls = new List<KeyboardControl>();
        [Binding]
        private string _Name = "";

        // Element Tree
        public UIElement? Parent { get; internal set; }
        public ConsoleWindowManager? ParentWindow { get; private set; }
        public ElementCollection Children { get; }
        internal ElementCollection? OwnerCollection { get; set; }
        internal int AttachmentVersion { get; set; }
        private long _invalidationVersion;
        private readonly List<IDisposable> _ownedResources = new();
        internal void OwnResource(IDisposable resource) => _ownedResources.Add(resource);
        public bool IsDisposed { get; private set; }
        public bool IsEffectivelyVisible => IsVisible && !IsDisposed && (Parent?.IsEffectivelyVisible ?? true);
        public bool IsEffectivelyEnabled => IsEnabled && (Parent?.IsEffectivelyEnabled ?? true);
        internal bool HasLayout { get; private set; }
        internal bool CanFocus => ConsiderForFocus && IsEffectivelyVisible && IsEffectivelyEnabled &&
            (!HasLayout || (ActualWidth > (ShowBorder ? 2 : 0) && ActualHeight > (ShowBorder ? 2 : 0) && HasVisibleArea));
        private bool HasVisibleArea
        {
            get
            {
                var clip = Parent?.ContentRect ?? (0, 0, int.MaxValue, int.MaxValue);
                return ActualX + ActualWidth > clip.Item1 && ActualY + ActualHeight > clip.Item2 &&
                    ActualX < (long)clip.Item1 + clip.Item3 && ActualY < (long)clip.Item2 + clip.Item4 && (Parent?.HasVisibleArea ?? true);
            }
        }
        internal bool RequiresContinuousRendering => !EnableCaching || _bindings.Values.Any(x => x.RequiresPolling) || Children.Any(x => x.IsVisible && x.RequiresContinuousRendering);
        protected internal virtual IEnumerable<UIElement> FocusChildren => Children;
        internal bool IsWithin(UIElement ancestor) => ReferenceEquals(this, ancestor) || (Parent?.IsWithin(ancestor) ?? false);
        protected (int X, int Y, int Width, int Height) ContentRect
        {
            get { int edge = ShowBorder ? 1 : 0; return (edge, edge, Math.Max(0, ActualWidth - edge * 2), Math.Max(0, ActualHeight - edge * 2)); }
        }


        // Binding Manager
        protected Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();

        // Position Properties
        [Binding]
        private Alignment _XYAlignment = Alignment.TopLeft;
        public string UID { get; } = Guid.NewGuid().ToString();
        [Binding]
        private string _X = "";
        [Binding]
        private string _Y = "";
        [Binding]
        private string _Width = "100%";
        [Binding]
        private string _Height = "100%";
        [Binding]
        private string _MinWidth = "";
        [Binding]
        private string _MinHeight = "";
        [Binding]
        private string _MaxWidth = "";
        [Binding]
        private string _MaxHeight = "";
        [Binding]
        private int _Z = 0;

        // Calculated Properties
        public int ActualX { get; protected set; }
        public int ActualY { get; protected set; }
        public int ActualWidth { get; protected set; }
        public int ActualHeight { get; protected set; }
        public int ActualMinWidth { get; protected set; }
        public int ActualMinHeight { get; protected set; }
        public int ActualMaxWidth { get; protected set; }
        public int ActualMaxHeight { get; protected set; }

        // Box drawing configuration
        [Binding]
        private bool _ShowBorder = true;
        [Binding]
        private Color _BorderColor = Color.Gray;
        [Binding]
        private Color _FocusedBorderColor = Color.Cyan;
        [Binding]
        private Color _Foreground = Color.White;
        [Binding]
        private Color _Background = Color.FromArgb(0, 0, 0, 0);
        [Binding]
        private Color _FocusedForeground = Color.Cyan;
        [Binding]
        private Color _FocusedBackground = Color.FromArgb(0, 0, 0, 0);

        // Box drawing characters - can be overridden by derived classes
        protected virtual char BoxTopLeft => '┌';
        protected virtual char BoxTopRight => '┐';
        protected virtual char BoxBottomLeft => '└';
        protected virtual char BoxBottomRight => '┘';
        protected virtual char BoxHorizontal => '─';
        protected virtual char BoxVertical => '│';


        public UIElement()
        {
            Children = new ElementCollection(this);
        }

        internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);
        protected void VerifyAccess() { ThrowIfDisposed(); ParentWindow?.VerifyAccess(); }

        public T? Find<T>(string name) where T : UIElement
        {
            VerifyAccess();
            return ElementTree.Find<T>(Children, name);
        }

        public UIElement? FindById(string id)
        {
            VerifyAccess();
            return ElementTree.Descendants(Children).FirstOrDefault(x => x.UID == id);
        }

        public void Invalidate()
        {
            VerifyAccess();
            ParentWindow?.RequestRender();
            for (UIElement? current = this; current is not null; current = current.Parent)
            {
                current._invalidationVersion++;
                current.NeedsRecalculation = true;
            }
        }

        /// <summary>Detaches this element. It can be attached again, including to another window.</summary>
        public void Remove() { VerifyAccess(); OwnerCollection?.Remove(this); }

        /// <summary>Queues removal on the UI loop, replacing the old render-thread deletion flag.</summary>
        public void RequestRemove()
        {
            ThrowIfDisposed();
            var owner = OwnerCollection;
            var version = AttachmentVersion;
            if (owner is null) return;
            if (ParentWindow is { IsRunning: true } window)
                window.Post(() => { if (OwnerCollection == owner && AttachmentVersion == version) Remove(); });
            else Remove();
        }

        internal void SetWindow(ConsoleWindowManager? window)
        {
            var previous = ParentWindow;
            if (ReferenceEquals(previous, window)) return;
            ParentWindow = window;
            AttachmentVersion++; // Descendants also acquire a new attachment when their ancestor moves.
            foreach (var binding in _bindings.Values.Distinct())
                if (window is null) binding.Suspend(); else binding.Resume();
            foreach (var child in Children) child.SetWindow(window);
            if (previous is not null) OnDetachedFromWindow(previous);
            if (window is not null) OnAttachedToWindow(window);
        }

        protected virtual void OnAttachedToWindow(ConsoleWindowManager window) { }
        protected virtual void OnDetachedFromWindow(ConsoleWindowManager window) { }
        protected virtual void OnChildAdded(UIElement child) { }
        protected virtual void OnChildRemoved(UIElement child) { }
        internal void ChildAdded(UIElement child) => OnChildAdded(child);
        internal void ChildRemoved(UIElement child) => OnChildRemoved(child);

        public void Dispose()
        {
            if (IsDisposed) return;
            VerifyAccess();
            Remove();
            List<Exception>? failures = null;
            foreach (var child in Children.ToArray())
            {
                try { child.Dispose(); }
                catch (Exception ex) { (failures ??= new()).Add(ex); }
            }
            Children.Clear();
            foreach (var binding in _bindings.Values.Distinct()) binding.Dispose();
            _bindings.Clear();
            _KeyboardControls.Clear();
            foreach (var resource in _ownedResources.AsEnumerable().Reverse())
            {
                try { resource.Dispose(); }
                catch (Exception ex) { (failures ??= new()).Add(ex); }
            }
            _ownedResources.Clear();
            try { DisposeCore(); }
            catch (Exception ex) { (failures ??= new()).Add(ex); }
            finally { IsDisposed = true; GC.SuppressFinalize(this); }
            if (failures is not null) throw new AggregateException("Element disposal failed.", failures);
        }

        /// <summary>Release resources permanently. Remove/reattach does not call this hook.</summary>
        protected virtual void DisposeCore() { }

        /// <summary>
        /// Sets a binding for a property or event.
        /// </summary>
        public void SetBinding(string propertyName, Binding binding)
        {
            VerifyAccess();
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            if (_bindings.TryGetValue(propertyName, out var previous))
            {
                if (ReferenceEquals(previous, binding)) return;
                previous.Dispose();
            }
            _bindings[propertyName] = binding;
            binding.Watch(() =>
            {
                var window = ParentWindow;
                var version = AttachmentVersion;
                void Update()
                {
                    if (IsDisposed || AttachmentVersion != version || !_bindings.TryGetValue(propertyName, out var current) || current != binding) return;
                    Invalidate(); OnPropertyChanged(propertyName);
                }
                if (window is { IsRunning: true } && !window.CheckAccess())
                {
                    try { window.Post(Update); }
                    catch (InvalidOperationException) when (!window.IsRunning || window.IsStopping) { }
                }
                else Update();
            });
            if (ParentWindow is null) binding.Suspend(); else binding.Resume();
            Invalidate();
        }

        public bool RemoveBinding(string propertyName)
        {
            VerifyAccess();
            if (!_bindings.Remove(propertyName, out var binding)) return false;
            binding.Dispose(); Invalidate(); return true;
        }

        internal Binding? DetachBinding(string propertyName)
        {
            VerifyAccess();
            if (!_bindings.Remove(propertyName, out var binding)) return null;
            binding.Suspend(); return binding;
        }

        /// <summary>
        /// Gets the bound value or the local value.
        /// </summary>
        protected object? GetBoundValue(string propertyName, object? localValue)
        {
            return _bindings.TryGetValue(propertyName, out var binding) && !binding.IsEvent ? binding.GetValue() : localValue;
        }

        /// <summary>
        /// Sets the bound value or the local value. ?
        /// </summary>
        protected void SetBoundValue<T>(string propertyName, T value, ref T localStorage)
        {
            VerifyAccess();
            if (_bindings.TryGetValue(propertyName, out var binding))
            {
                if (binding.IsProperty)
                {
                    binding.SetValue(value);
                    Invalidate();
                    OnPropertyChanged(propertyName);
                    return;
                }
                // Event bindings are handled separately
            }

            // Not bound, set locally
            if (EqualityComparer<T>.Default.Equals(localStorage, value)) return;
            localStorage = value;
            Invalidate();
            OnPropertyChanged(propertyName);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (propertyName is nameof(IsVisible) or nameof(IsEnabled) or nameof(ConsiderForFocus)) ParentWindow?.TreeChanged();
        }

        // Base rendering method
        public ConsoleBuffer Render()
        {
            VerifyAccess();
            var buffer = new ConsoleBuffer(ActualWidth, ActualHeight);
            var background = IsFocused ? FocusedBackground : Background;

            if (!IsVisible || ActualWidth == 0 || ActualHeight == 0)
            {
                return buffer;
            }
            buffer.Clear(background);

            // Draw border if enabled
            if (ShowBorder)
            {
                DrawBorder(buffer);
            }

            // Call the derived class's rendering implementation
            long version = _invalidationVersion;
            RenderContent(buffer);
            NeedsRecalculation = version != _invalidationVersion;

            return buffer;
        }

        protected void DrawBorder(ConsoleBuffer buffer)
        {
            var foreground = IsFocused ? FocusedBorderColor : BorderColor;
            var background = IsFocused ? FocusedBackground : Background;

            // Draw corners
            buffer.SetPixel(0, 0, BoxTopLeft, foreground, background);
            buffer.SetPixel(ActualWidth - 1, 0, BoxTopRight, foreground, background);
            buffer.SetPixel(0, ActualHeight - 1, BoxBottomLeft, foreground, background);
            buffer.SetPixel(ActualWidth - 1, ActualHeight - 1, BoxBottomRight, foreground, background);

            // Draw top and bottom edges
            for (int x = 1; x < ActualWidth - 1; x++)
            {
                buffer.SetPixel(x, 0, BoxHorizontal, foreground, background);
                buffer.SetPixel(x, ActualHeight - 1, BoxHorizontal, foreground, background);
            }

            // Draw left and right edges
            for (int y = 1; y < ActualHeight - 1; y++)
            {
                buffer.SetPixel(0, y, BoxVertical, foreground, background);
                buffer.SetPixel(ActualWidth - 1, y, BoxVertical, foreground, background);
            }
        }
        // Classes should implement this themselves
        protected abstract void RenderContent(ConsoleBuffer buffer);

        public virtual void CalculateLayout(int parentX, int parentY, int parentWidth, int parentHeight)
        {
            VerifyAccess();
            var previous = (ActualX, ActualY, ActualWidth, ActualHeight);
            parentWidth = Math.Max(0, parentWidth);
            parentHeight = Math.Max(0, parentHeight);
            ActualMaxWidth = Math.Clamp(string.IsNullOrEmpty(MaxWidth) ? parentWidth : ParseRelativeValue(MaxWidth, parentWidth), 0, parentWidth);
            ActualMaxHeight = Math.Clamp(string.IsNullOrEmpty(MaxHeight) ? parentHeight : ParseRelativeValue(MaxHeight, parentHeight), 0, parentHeight);
            ActualMinWidth = Math.Clamp(ParseRelativeValue(MinWidth, parentWidth), 0, ActualMaxWidth);
            ActualMinHeight = Math.Clamp(ParseRelativeValue(MinHeight, parentHeight), 0, ActualMaxHeight);
            int width = Math.Clamp(ParseRelativeValue(Width, parentWidth), ActualMinWidth, ActualMaxWidth);
            int height = Math.Clamp(ParseRelativeValue(Height, parentHeight), ActualMinHeight, ActualMaxHeight);
            int x = ParseRelativeValue(X, parentWidth), y = ParseRelativeValue(Y, parentHeight);
            bool right = XYAlignment is Alignment.TopRight or Alignment.CenterRight or Alignment.BottomRight;
            bool bottom = XYAlignment is Alignment.BottomLeft or Alignment.BottomCenter or Alignment.BottomRight;
            bool centerX = XYAlignment is Alignment.TopCenter or Alignment.Centered or Alignment.BottomCenter;
            bool centerY = XYAlignment is Alignment.CenterLeft or Alignment.Centered or Alignment.CenterRight;
            x = X == "center" ? (parentWidth - width) / 2 : right ? parentWidth - width - x : centerX ? (parentWidth - width) / 2 + x : x;
            y = Y == "center" ? (parentHeight - height) / 2 : bottom ? parentHeight - height - y : centerY ? (parentHeight - height) / 2 + y : y;
            x = Math.Clamp(x, -parentWidth, parentWidth);
            y = Math.Clamp(y, -parentHeight, parentHeight);
            ActualX = parentX + x;
            ActualY = parentY + y;
            ActualWidth = Math.Max(0, Math.Min(width, parentWidth - Math.Max(0, x)));
            ActualHeight = Math.Max(0, Math.Min(height, parentHeight - Math.Max(0, y)));
            HasLayout = true;
            if (previous != (ActualX, ActualY, ActualWidth, ActualHeight)) Invalidate();
            LayoutChildren();
        }

        protected virtual void LayoutChildren()
        {
            var area = ContentRect;
            foreach (var child in Children.ToArray())
                if (!child.IsDisposed && child.Parent == this) child.CalculateLayout(area.X, area.Y, area.Width, area.Height);
        }

        public int GetAbsoluteX() => ActualX + (Parent?.GetAbsoluteX() ?? 0);
        public int GetAbsoluteY() => ActualY + (Parent?.GetAbsoluteY() ?? 0);

        public (int X, int Y) GetAbsolutePosition()
        {
            return (GetAbsoluteX(), GetAbsoluteY());
        }

        public string GetRootParentRelativeWidth()
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current.Width;
        }

        public string GetRootParentRelativeHeight()
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current.Height;
        }

        public string GetRootParentRelativeX()
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current.X;
        }

        public string GetRootParentRelativeY()
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current.Y;
        }

        public (string Width, string Height, string X, string Y) GetRootParentRelativeDimensions()
        {
            return (GetRootParentRelativeWidth(), GetRootParentRelativeHeight(), GetRootParentRelativeX(), GetRootParentRelativeY());
        }
        public int ParseRelativeValue(string value, int parentSize)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Trim();
            if (value == "center") return parentSize / 2;
            if (value == "auto") return parentSize;
            bool percent = value.EndsWith('%');
            if (percent) value = value[..^1];
            if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount) || !double.IsFinite(amount)) return 0;
            if (percent) amount = parentSize * (amount / 100d);
            return (int)Math.Clamp(amount, int.MinValue, int.MaxValue);
        }

        public void RegisterKeyboardControl(ConsoleKey key, Action action, string description,
            bool requireShift = false, bool requireControl = false, bool requireAlt = false)
        {
            VerifyAccess();
            ArgumentNullException.ThrowIfNull(action);
            KeyboardControls.Add(new KeyboardControl
            {
                Key = key,
                Action = action,
                Description = description,
                RequireShift = requireShift,
                RequireControl = requireControl,
                RequireAlt = requireAlt
            });
        }
        public void RegisterKeyboardControl(KeyboardControl keyControl)
        {
            VerifyAccess();
            ArgumentNullException.ThrowIfNull(keyControl);
            KeyboardControls.Add(keyControl);
        }

        public virtual IEnumerable<KeyboardControl> GetKeyboardControls()
        {
            return KeyboardControls.ToArray();
        }
        public virtual void HandleKey(ConsoleKeyInfo keyInfo) => TryHandleKey(keyInfo);

        /// <summary>Returns true when this element consumes the key. Unhandled keys may bubble or navigate focus.</summary>
        public virtual bool TryHandleKey(ConsoleKeyInfo keyInfo)
        {
            VerifyAccess();
            if (LockControls || !IsEffectivelyVisible || !IsEffectivelyEnabled) return false;
            foreach (var control in KeyboardControls.ToArray())
            {
                if (!control.Matches(keyInfo) || control.Action is null) continue;
                control.Action();
                return true;
            }
            return false;
        }


    }
}
