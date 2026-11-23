using System.Collections.Concurrent;
using System.Drawing;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;
using MelonUI.Shaders;

#if MELONUI_GPU_TESTS
var hardwareTests = args.Contains("--gpu")
    ? new (string Name, Func<Task> Run)[] { ("actual Direct3D compute execution, resize and disposal", HardwareTests.Compute) } : [];
#else
if (args.Contains("--gpu")) { Console.Error.WriteLine("--gpu requires the Windows shader-test build."); return 1; }
var hardwareTests = Array.Empty<(string Name, Func<Task> Run)>();
#endif
var tests = new (string Name, Func<Task> Run)[]
{
    ("construction and unattached composition have no terminal side effects", Construction),
    ("late attachment propagates ownership; detached subtrees can be reused", Ownership),
    ("duplicate ownership, cycles and invalid replacements leave trees intact", InvalidTrees),
    ("empty pages attach; removal repairs focus without destroying content", EmptyPagesAndFocus),
    ("scoped lookup diagnoses ambiguity and type mismatch", Lookup),
    ("removing grid children clears placement/render state", GridRemoval),
    ("queue pop returns detached reusable content", QueueReuse),
    ("nested changes invalidate caches and no status rows are reserved", Invalidation),
    ("bottom-aligned status survives full-height frame capture", BottomRow),
    ("detach suspends event bindings; reattach resumes once; disposal releases them", Bindings),
    ("popup close permits reuse and explicit disposal is final", Popups),
    ("overflow popup registration stays bounded and detaches with its panel", OverflowLifetime),
    ("flat MXML still uses managed children", FlatMxml),
    ("shader startup tolerates no frame and the initial 120x1 frame", ShaderStartup),
    ("shader resize uses published dimensions and clips correctly", ShaderResize),
    ("shader readback publishes complete frames without blocking UI copies", ShaderPublication),
    ("run, post, invoke, access checks, stop and terminal cleanup", Dispatch),
    ("queued removal runs on the UI owner and ignores stale attachments", QueuedRemoval),
    ("cancellation and restart have joined lifetimes", Restart),
    ("handled posted errors keep the loop alive", HandledError),
    ("keyboard callback errors use the observable UI error path", KeyboardError),
    ("fatal errors restore terminal and cancel queued awaiters", FatalError),
    ("terminal initialization failure still cleans up", BeginFailure),
    ("a pre-cancelled run does not touch the terminal", PreCancelled),
    ("window disposal destroys owned elements and releases detached ones to their owner", Disposal)
}.Concat(ControlTests.All).Concat(MxmlTests.All).Concat(GeneratorTests.All).Concat(hardwareTests)
    .Concat(args.Contains("--soak") ? new (string Name, Func<Task> Run)[] { ("500 MXML reloads release old trees, bindings and backend subscriptions", SoakTests.Run) } : []).ToArray();
var failures = 0;
foreach (var test in tests)
{
    try { await test.Run().WaitAsync(TimeSpan.FromSeconds(args.Contains("--soak") ? 60 : 10)); Console.WriteLine($"PASS: {test.Name}"); }
    catch (Exception ex) { failures++; Console.Error.WriteLine($"FAIL: {test.Name}\n{ex}"); }
}
Console.WriteLine($"{tests.Length - failures}/{tests.Length} regression groups passed.");
return failures == 0 ? 0 : 1;

static async Task Construction()
{
    var terminal = new FakeTerminal();
    await using var window = new ConsoleWindowManager(terminal) { Title = "Library" };
    var page = Page();
    page.Children.Add(new Probe());
    page.Children.Clear();
    window.Elements.Add(page);
    Equal(0, terminal.BeginCount);
    Equal(0, terminal.SizeRequests);
    Check(window.CaptureText(width: 30, height: 8).Length > 0);
    Equal(0, terminal.SizeRequests);
    Throws<InvalidOperationException>(() => window.Post(() => { }));
}

static async Task Ownership()
{
    await using var first = Window();
    await using var second = Window();
    var root = Page();
    var nested = Page();
    root.Children.Add(nested);
    first.Elements.Add(root);
    var late = Page();
    var leaf = new Probe();
    late.Children.Add(leaf);
    nested.Children.Add(late);
    Check(leaf.Parent == late && late.Parent == nested && leaf.ParentWindow == first);
    root.Remove();
    Check(root.Parent is null && root.ParentWindow is null && leaf.ParentWindow is null);
    Check(leaf.Parent == late && late.Parent == nested && nested.Parent == root);
    second.Elements.Add(root);
    Check(leaf.ParentWindow == second && !root.IsDisposed);
}

static async Task InvalidTrees()
{
    await using var window = Window();
    var root = Page();
    var child = Page();
    root.Children.Add(child);
    window.Elements.Add(root);
    Throws<InvalidOperationException>(() => window.Elements.Add(root));
    Throws<InvalidOperationException>(() => child.Children.Add(root));
    Throws<InvalidOperationException>(() => child.Children.Add(child));
    var replacement = Page();
    window.Elements.Add(replacement);
    Throws<InvalidOperationException>(() => root.Children[0] = replacement);
    Check(root.Children.Count == 1 && root.Children[0] == child && replacement.ParentWindow == window);
    Throws<ArgumentOutOfRangeException>(() => root.Children.Insert(99, new Probe()));
    Equal(2, window.Elements.Count);
}

static async Task EmptyPagesAndFocus()
{
    await using var window = Window();
    var empty = Page();
    window.Elements.Add(empty);
    Equal(1, window.Elements.Count);
    Check(window.FocusedElement is null);
    var page = Page();
    var leaf = new Probe();
    page.Children.Add(leaf);
    window.Elements.Add(page);
    Check(window.FocusedElement == leaf);
    page.Remove();
    Check(window.FocusedElement is null && !leaf.IsFocused && !leaf.IsDisposed);
    empty.Children.Add(leaf = new Probe());
    Check(window.FocusedElement == leaf);
}

static async Task Lookup()
{
    await using var window = Window();
    var one = Page();
    var two = Page();
    var first = new Probe { Name = "Status" };
    var second = new Probe { Name = "Status" };
    one.Children.Add(first);
    two.Children.Add(second);
    window.Elements.Add(one);
    window.Elements.Add(two);
    Check(one.Find<Probe>("Status") == first);
    Check(window.FindById(second.UID) == second);
    Check(two.Find<Probe>("missing") is null);
    Throws<InvalidOperationException>(() => window.Find<Probe>("Status"));
    Throws<InvalidOperationException>(() => one.Find<TextBlock>("Status"));
}

static async Task GridRemoval()
{
    await using var window = Window();
    var grid = new GridContainer(1, false, 1) { Width = "100%", Height = "100%" };
    var leaf = new Probe();
    var keyCalls = 0;
    leaf.RegisterKeyboardControl(ConsoleKey.D, () => { keyCalls++; leaf.Remove(); }, "Remove self");
    grid.AddElement(leaf, 0, 0);
    window.Elements.Add(grid);
    window.ComposeFrame(30, 10);
    Check(leaf.Renders > 0);
    var before = leaf.Renders;
    grid.Children.Remove(leaf);
    window.ComposeFrame(30, 10);
    Equal(before, leaf.Renders);
    Check(leaf.Parent is null && leaf.ParentWindow is null);
    Throws<InvalidOperationException>(() => grid.SetCell(leaf, 0, 0));
    var badCell = new Probe();
    Throws<ArgumentOutOfRangeException>(() => grid.AddElement(badCell, 1, 0));
    Check(badCell.Parent is null);
    grid.Children.Add(leaf);
    var key = new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false);
    leaf.HandleKey(key);
    grid.HandleKey(key);
    Equal(1, keyCalls);
    Check(leaf.Parent is null);
    leaf.Dispose();
}

static async Task Invalidation()
{
    await using var window = Window();
    var page = Page();
    var text = new TextBlock { Text = "Before", Name = "Status", Width = "100%", Height = "1", ShowBorder = false };
    page.Children.Add(text);
    window.Elements.Add(page);
    Check(window.CaptureText(width: 40, height: 10).Contains("Before"));
    page.NeedsRecalculation = false;
    text.NeedsRecalculation = false;
    text.Text = "After";
    Check(page.NeedsRecalculation);
    Check(window.CaptureText(width: 40, height: 10).Contains("After"));
    Equal(0, text.GetAbsoluteY());
    text.IsVisible = false;
    Check(!window.CaptureText(width: 40, height: 10).Contains("After"));
}

static async Task Bindings()
{
    await using var window = Window();
    var source = new EventSource();
    var leaf = new Probe();
    var calls = 0;
    var binding = Binding.CreateEventBinding(source, nameof(EventSource.Changed));
    binding.Subscribe(new EventHandler((_, _) => calls++));
    leaf.SetBinding("Tick", binding);
    Equal(0, source.Subscribers);
    window.Elements.Add(leaf);
    source.Raise();
    Equal(1, calls);
    leaf.Remove();
    Equal(0, source.Subscribers);
    source.Raise();
    Equal(1, calls);
    window.Elements.Add(leaf);
    Equal(1, source.Subscribers);
    source.Raise();
    Equal(2, calls);
    leaf.Dispose();
    Equal(0, source.Subscribers);
    Check(leaf.IsDisposed && leaf.Disposals == 1);
    leaf.Dispose();
    Equal(1, leaf.Disposals);
    Throws<ObjectDisposedException>(() => window.Elements.Add(leaf));
}

static async Task Popups()
{
    await using var window = Window();
    var normal = new Probe();
    window.Elements.Add(normal);
    var content = Page();
    var input = new Probe();
    content.Children.Add(input);
    var popup = window.ShowPopup(content);
    Check(window.FocusedElement == input);
    popup.Close();
    Check(!content.IsDisposed && content.ParentWindow is null && window.FocusedElement == normal);
    window.Elements.Add(content);
    content.Remove();
    popup.Dispose();
    Check(content.IsDisposed && input.IsDisposed);
    popup.Dispose();
    var secondContent = Page();
    var second = window.ShowPopup(secondContent);
    secondContent.Remove();
    window.Elements.Add(secondContent);
    second.Close(); // A stale handle must not detach a later attachment.
    Check(secondContent.ParentWindow == window);
    await window.DisposeAsync();
    second.Dispose(); // Window-owned content may already have been destroyed.
}

static async Task OverflowLifetime()
{
    await using var window = Window();
    var panel = new ButtonPanel { Width = "25", Height = "3" };
    panel.Children.Add(new Button(ConsoleKey.A) { Text = "A long action that overflows" });
    window.Elements.Add(panel);
    window.CaptureText(width: 40, height: 10);
    panel.HandleKey(new ConsoleKeyInfo('`', ConsoleKey.Oem3, false, false, false));
    window.CaptureText(width: 40, height: 10);
    window.CaptureText(width: 40, height: 10);
    Equal(1, window.Elements.OfType<OverflowMenu>().Count());
    panel.Remove();
    Equal(0, window.Elements.Count);
    Check(!panel.IsDisposed);
    panel.Dispose();
}

static async Task BottomRow()
{
    await using var window = Window();
    window.Elements.Add(new TextBlock
    {
        Name = "Status", Text = "Bottom", Width = "20", Height = "1", ShowBorder = false,
        XYAlignment = MelonUI.Enums.Alignment.BottomLeft
    });
    var capture = window.CaptureText(width: 20, height: 5);
    Check(capture.Split(Environment.NewLine)[4].Contains("Bottom"));
    Equal(5, capture.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
}

static async Task QueueReuse()
{
    await using var window = Window();
    var queue = new QueueContainer { Width = "100%", Height = "100%" };
    window.Elements.Add(queue);
    var child = new Probe();
    queue.QueueElement(child);
    Check(child.ParentWindow == window);
    Check(queue.PopElement() == child);
    Check(!child.IsDisposed && child.Parent is null && child.ParentWindow is null);
    Check(queue.PopElement() is null);
    queue.QueueElement(child);
    queue.Children.Clear();
    child.Dispose();
}

static async Task FlatMxml()
{
    await using var window = Window();
    var page = Page();
    Check(page.Compile("<MUIPage Width=\"100%\" Height=\"100%\" Assemblies=\"MelonUI\" Namespaces=\"MelonUI.Base,MelonUI.Default\"><TextBlock Name=\"Message\" Text=\"Hello\" Width=\"20\" Height=\"3\" /></MUIPage>"));
    window.Elements.Add(page);
    Check(page.Find<TextBlock>("Message")?.ParentWindow == window);
    Check(window.CaptureText(width: 40, height: 10).Contains("Hello"));
}

static async Task Dispatch()
{
    var terminal = new FakeTerminal();
    await using var window = new ConsoleWindowManager(terminal) { Title = "First" };
    var leaf = new Probe();
    window.Elements.Add(leaf);
    var frame = Signal();
    window.FrameRendered += (_, _) => frame.TrySetResult();
    var run = window.RunAsync();
    await frame.Task.WaitAsync(TimeSpan.FromSeconds(3));
    Check(!run.IsCompleted && window.IsRunning);
    Throws<InvalidOperationException>(() => window.RunAsync());
    Throws<InvalidOperationException>(() => leaf.Name = "WrongThread");
    Throws<InvalidOperationException>(() => window.Elements.Add(new Probe()));
    var posted = 0;
    var uiThread = 0;
    window.Post(() => { posted++; uiThread = Environment.CurrentManagedThreadId; });
    await window.InvokeAsync(() => { Equal(1, posted); Check(window.CheckAccess()); window.Title = "Second"; });
    await Fails<InvalidOperationException>(() => window.InvokeAsync(() => throw new InvalidOperationException("Expected")));
    var anotherFrame = Signal();
    await window.InvokeAsync(() => { window.FrameRendered += (_, _) => anotherFrame.TrySetResult(); leaf.Invalidate(); });
    await anotherFrame.Task.WaitAsync(TimeSpan.FromSeconds(3));
    Equal(1, posted);
    Equal(terminal.PresentThread, uiThread);
    Check(terminal.Titles.Contains("First") && terminal.Titles.Contains("Second"));
    window.Stop();
    await run.WaitAsync(TimeSpan.FromSeconds(3));
    Check(!window.IsRunning && terminal.EndCount == 1);
    Throws<InvalidOperationException>(() => window.Post(() => { }));
}

static async Task QueuedRemoval()
{
    await using var window = Window();
    var page = Page();
    var leaf = new Probe();
    page.Children.Add(leaf);
    window.Elements.Add(page);
    var frame = Signal();
    window.FrameRendered += (_, _) => frame.TrySetResult();
    var run = window.RunAsync();
    await frame.Task;
    leaf.RequestRemove();
    await window.InvokeAsync(() => Check(leaf.ParentWindow is null && !leaf.IsDisposed));
    await window.InvokeAsync(() =>
    {
        page.Children.Add(leaf);
        leaf.RequestRemove();
        leaf.Remove();
        page.Children.Add(leaf); // Old removal must not remove this new attachment.
    });
    await window.InvokeAsync(() => Check(leaf.Parent == page));
    await window.InvokeAsync(() =>
    {
        leaf.RequestRemove();
        page.Remove();
        window.Elements.Add(page); // Moving an ancestor also invalidates the old removal request.
    });
    await window.InvokeAsync(() => Check(leaf.Parent == page));
    window.Stop();
    await run;
}

static async Task Restart()
{
    var terminal = new FakeTerminal();
    await using var window = new ConsoleWindowManager(terminal);
    for (var i = 0; i < 2; i++)
    {
        using var cancel = new CancellationTokenSource();
        var frame = Signal();
        EventHandler handler = (_, _) => frame.TrySetResult();
        window.FrameRendered += handler;
        var run = window.RunAsync(cancel.Token);
        await frame.Task;
        cancel.Cancel();
        await run;
        window.FrameRendered -= handler;
        Equal(i + 1, terminal.EndCount);
    }
}

static async Task HandledError()
{
    await using var window = Window();
    var observed = Signal();
    window.UnhandledException += (_, error) => { error.Handled = true; observed.TrySetResult(); };
    var run = window.RunAsync();
    window.Post(() => throw new InvalidOperationException("Handled"));
    await observed.Task;
    await window.InvokeAsync(() => { });
    Check(window.IsRunning);
    window.Stop();
    await run;
}

static async Task KeyboardError()
{
    var terminal = new FakeTerminal();
    await using var window = new ConsoleWindowManager(terminal);
    window.RegisterKeyboardControl(ConsoleKey.F1, () => throw new InvalidOperationException("Key failure"), "Throw");
    window.UnhandledException += (_, error) => { error.Handled = true; window.Stop(); };
    terminal.Keys.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.F1, false, false, false));
    await window.RunAsync();
    Equal(1, terminal.EndCount);
}

static async Task FatalError()
{
    var terminal = new FakeTerminal();
    var window = new ConsoleWindowManager(terminal);
    using var gate = new ManualResetEventSlim();
    var entered = Signal();
    var run = window.RunAsync();
    window.Post(() => { entered.TrySetResult(); if (!gate.Wait(3000)) throw new TimeoutException(); });
    await entered.Task;
    window.Post(() => throw new InvalidOperationException("Expected fatal error"));
    var pending = window.InvokeAsync(() => throw new Exception("Must not execute"));
    gate.Set();
    await Fails<InvalidOperationException>(() => run);
    await Fails<TaskCanceledException>(() => pending);
    Equal(1, terminal.EndCount);
    await Fails<InvalidOperationException>(() => window.DisposeAsync().AsTask());
    Check(window.IsDisposed);
}

static async Task BeginFailure()
{
    var terminal = new FakeTerminal { FailBegin = true };
    var window = new ConsoleWindowManager(terminal);
    await Fails<IOException>(() => window.RunAsync());
    Equal(1, terminal.EndCount);
    await Fails<IOException>(() => window.DisposeAsync().AsTask());
}

static async Task PreCancelled()
{
    var terminal = new FakeTerminal();
    await using var window = new ConsoleWindowManager(terminal);
    using var cancel = new CancellationTokenSource();
    cancel.Cancel();
    await window.RunAsync(cancel.Token);
    Equal(0, terminal.BeginCount);
}

static async Task Disposal()
{
    var window = Window();
    var owned = new Probe();
    var cached = new Probe();
    window.Elements.Add(owned);
    window.Elements.Add(cached);
    cached.Remove();
    await window.DisposeAsync();
    Check(owned.IsDisposed && !cached.IsDisposed && cached.ParentWindow is null);
    await window.DisposeAsync();
    Throws<ObjectDisposedException>(() => window.Elements.Add(cached));
    cached.Dispose();
}

static Task ShaderStartup()
{
    var frames = new ShaderFrameExchange(120, 1);
    var target = new ConsoleBuffer(80, 24);
    Check(!frames.HasFrame && !frames.CopyLatestTo(target));
    frames.PublishFrame(120, 1, pixels => Array.Fill(pixels, ShaderPixel('S')));
    Check(frames.HasFrame && frames.CopyLatestTo(target));
    for (var x = 0; x < target.Width; x++) Equal('S', target[0, x].Character);
    Equal('\0', target[1, 0].Character);
    Equal((120, 1), frames.RequestedSize);
    Throws<ArgumentOutOfRangeException>(() => frames.RequestSize(0, 0));
    Equal((120, 1), frames.RequestedSize);
    return Task.CompletedTask;
}

static Task ShaderResize()
{
    var frames = new ShaderFrameExchange(120, 1);
    frames.RequestSize(3, 2);
    Equal((3, 2), frames.RequestedSize);
    frames.PublishFrame(3, 2, pixels =>
    {
        for (int i = 0; i < pixels.Length; i++) pixels[i] = ShaderPixel((char)('A' + i));
    });
    var larger = new ConsoleBuffer(5, 4);
    frames.CopyLatestTo(larger);
    Equal('D', larger[0, 0].Character);
    Equal('F', larger[0, 2].Character);
    Equal('A', larger[1, 0].Character);
    Equal('C', larger[1, 2].Character);
    Equal('\0', larger[0, 3].Character);
    Equal('\0', larger[2, 0].Character);
    var smaller = new ConsoleBuffer(2, 1);
    frames.RequestSize(2, 1);
    frames.CopyLatestTo(smaller); // The requested resize has not rendered yet.
    Equal('D', smaller[0, 0].Character);
    Equal('E', smaller[0, 1].Character);
    frames.PublishFrame(2, 1, pixels => { pixels[0] = ShaderPixel('X'); pixels[1] = ShaderPixel('Y'); });
    frames.CopyLatestTo(smaller);
    Equal('X', smaller[0, 0].Character);
    Equal('Y', smaller[0, 1].Character);
    Throws<IOException>(() => frames.PublishFrame(2, 1, pixels =>
    {
        pixels[0] = ShaderPixel('!');
        throw new IOException("Readback failed");
    }));
    frames.CopyLatestTo(smaller);
    Equal('X', smaller[0, 0].Character); // A failed read must preserve the last completed frame.
    return Task.CompletedTask;
}

static async Task ShaderPublication()
{
    var frames = new ShaderFrameExchange(1, 1);
    frames.PublishFrame(1, 1, pixels => pixels[0] = ShaderPixel('A'));
    using var release = new ManualResetEventSlim();
    var entered = Signal();
    var producer = Task.Run(() => frames.PublishFrame(3, 2, pixels =>
    {
        Array.Fill(pixels, ShaderPixel('B'));
        entered.TrySetResult();
        if (!release.Wait(3000)) throw new TimeoutException("Test producer was not released.");
    }));
    try
    {
        await entered.Task;
        var target = new ConsoleBuffer(4, 3);
        frames.CopyLatestTo(target);
        Equal('A', target[0, 0].Character);
    }
    finally { release.Set(); await producer; }
    var finished = new ConsoleBuffer(4, 3);
    frames.CopyLatestTo(finished);
    Equal('B', finished[0, 0].Character);
    Equal('B', finished[1, 2].Character);

    // Alternate shapes with equal pixel counts while copying on another thread.
    var stress = Task.Run(() =>
    {
        for (int i = 0; i < 1000; i++)
        {
            var size = i % 2 == 0 ? (Width: 31, Height: 2) : (Width: 2, Height: 31);
            frames.RequestSize(size.Width, size.Height);
            frames.PublishFrame(size.Width, size.Height, pixels => Array.Fill(pixels, ShaderPixel((char)('C' + i % 20))));
        }
    });
    for (int i = 0; i < 1000; i++)
    {
        var target = new ConsoleBuffer(i % 35 + 1, (i * 7) % 35 + 1);
        frames.CopyLatestTo(target);
        var visible = target.buffer.Where(pixel => pixel.Character != '\0').ToArray();
        Check(visible.Length > 0);
        Check(visible.All(pixel => pixel.Character == visible[0].Character));
    }
    await stress;
}

static ConsolePixel ShaderPixel(char character) => new(character, Color.White, Color.Black, false);

static ConsoleWindowManager Window() => new(new FakeTerminal());
static MUIPage Page() => new() { Width = "100%", Height = "100%", ShowBorder = false };
static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}."); }
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
static async Task Fails<T>(Func<Task> action) where T : Exception
{
    try { await action().WaitAsync(TimeSpan.FromSeconds(3)); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}

internal sealed class Probe : UIElement
{
    public int Renders;
    public int Disposals;
    protected override void RenderContent(ConsoleBuffer buffer) { Renders++; NeedsRecalculation = false; }
    protected override void DisposeCore() => Disposals++;
}

internal sealed class EventSource
{
    public event EventHandler? Changed;
    public int Subscribers => Changed?.GetInvocationList().Length ?? 0;
    public void Raise() => Changed?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeTerminal : IConsoleTerminal
{
    public int BeginCount;
    public int EndCount;
    public int SizeRequests;
    public int PresentThread;
    public int PresentCount;
    public bool FailBegin;
    public ConcurrentBag<string> Titles = new();
    public ConcurrentQueue<ConsoleKeyInfo> Keys = new();
    public void Begin() { BeginCount++; if (FailBegin) throw new IOException("Expected initialization failure"); }
    public (int Width, int Height) GetSize() { SizeRequests++; return (40, 10); }
    public bool TryReadKey(out ConsoleKeyInfo key) => Keys.TryDequeue(out key);
    public void SetTitle(string title) => Titles.Add(title);
    public void Present(ConsoleBuffer frame, bool usePlatformRenderer) { PresentThread = Environment.CurrentManagedThreadId; PresentCount++; }
    public void End() => EndCount++;
}
