using System.Drawing;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using MelonUI.Managers;

internal static class ControlTests
{
    internal static IEnumerable<(string Name, Func<Task> Run)> All =>
    [
        ("default controls survive empty, tiny, nested and repeated resize states", SizeMatrix),
        ("layout applies min/max, alignment, percentages and local-to-absolute positions", Layout),
        ("RGB bounds, channel editing, hex input, commit and cancel are safe", Rgb),
        ("menus handle empty and changing lists, disabled items and repeated prefix search", Menus),
        ("file picker keeps process CWD and handles empty/missing folders and directory selection", Files),
        ("buttons activate once and honor enabled/locked state", Buttons),
        ("progress rendering does not mutate bound progress or display flags", Progress),
        ("work log handles empty/tiny snapshots and invalid progress counts", Jobs),
        ("text block preserves blank lines, color across wrapping and long-word clipping", Text),
        ("text box Unicode editing, multiline, limits, readonly and modifier handling", Editing),
        ("password reveal resets on focus loss and ordinary captures stay masked", Passwords),
        ("tab order wraps both ways and skips hidden, disabled and clipped elements", TabFocus),
        ("spatial focus uses nested rendered positions and preserves drawing order", SpatialFocus),
        ("input is delivered once to focused content and bubbles to parent/global handlers", InputScope),
        ("modal popups contain focus and shortcuts, restore prior focus and support nesting", Modals),
        ("queue exposes only active content to focus and advances after pop", QueueFocus),
        ("overflow menu stays visible near bottom edge and activates its action", Overflow),
        ("control updates under cached nested pages appear on the next frame", CachedControls),
        ("buffer drawing safely clips wide text at negative and out-of-range coordinates", BufferClipping),
        ("backgrounds, alpha and zero-size children compose without erasing siblings", Composition),
        ("UTF-8 frames and screenshots preserve surrogate pairs and row alignment", UnicodeOutput),
        ("overflow actions cannot invoke removed or disabled source buttons", StaleOverflow),
        ("control callbacks can dispose their own control safely", DisposingCallbacks),
        ("cached descendants reflow when only their ancestor viewport resizes", CachedResize)
    ];
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0', bool shift = false, bool ctrl = false, bool alt = false) => new(ch, key, shift, alt, ctrl);
    private static void Check(bool value, string message = "Assertion failed") { if (!value) throw new Exception(message); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}."); }
    private static ConsoleWindowManager Window() => new(new FakeTerminal());
    private static TextBox Input(string name, int x = 0, int y = 0) => new() { Name = name, X = $"{x}", Y = $"{y}", Width = "12", Height = "3" };
    private static MUIPage Page() => new() { ShowBorder = false };

    private static async Task SizeMatrix()
    {
        Func<UIElement>[] factories =
        [
            () => new Button { Text = "a long 界 label" }, () => new ButtonPanel(), () => new OptionsMenu(),
            () => new ColorPicker(), () => new TextBlock { Text = "[Color(20,30,40)]trailing space \n\nwide 界 e\u0301" },
            () => new TextBox { Text = "long 界 e\u0301 value", Label = "field" }, () => new ProgressBar { ShowPercentage = true, ProgressValue = float.NaN },
            () => new WorkLog(), () => new Waveform([float.NaN, -2, 0, 1, float.PositiveInfinity]),
            () => new Line { AntiAliased = true, X2 = "100%", Y2 = "100%", Intensity = 100 },
            () => new FPSCounter(), () => new GridContainer(4, false, 1, 2, 3, 4), () => new QueueContainer()
        ];
        foreach (var create in factories)
        {
            await using var window = Window();
            var element = create();
            var page = Page(); page.Children.Add(element); window.Elements.Add(page);
            if (element is GridContainer grid) grid.AddElement(new TextBox { Label = "nested" }, 3, 3);
            if (element is QueueContainer queue) for (int i = 0; i < 9; i++) queue.QueueElement(Input($"item{i}"));
            if (element is ButtonPanel panel) for (int i = 0; i < 9; i++) panel.AddButton(new Button(ConsoleKey.A) { Text = "action" });
            foreach (bool border in new[] { true, false })
                foreach (var size in new[] { (0, 0), (1, 1), (2, 2), (3, 3), (5, 4), (80, 24), (3, 1), (40, 12) })
                {
                    element.ShowBorder = border; element.Width = $"{size.Item1}"; element.Height = $"{size.Item2}";
                    try { window.CaptureText(width: Math.Max(1, size.Item1), height: Math.Max(1, size.Item2)); }
                    catch (Exception ex) { throw new Exception($"{element.GetType().Name} failed at {size}, border={border}", ex); }
                    Check(element.ActualWidth >= 0 && element.ActualHeight >= 0);
                }
        }
    }

    private static Task Layout()
    {
        using var page = new MUIPage { X = "5", Y = "4", Width = "20", Height = "10", ShowBorder = true };
        var child = Input("child", 2, 1); child.Width = "100%"; child.Height = "100%"; child.MaxWidth = "7"; child.MaxHeight = "4";
        page.Children.Add(child); page.CalculateLayout(0, 0, 80, 24);
        Equal(7, child.ActualWidth); Equal(4, child.ActualHeight);
        Equal((8, 6), child.GetAbsolutePosition());
        child.XYAlignment = Alignment.BottomRight; child.X = "1"; child.Y = "1";
        page.CalculateLayout(0, 0, 80, 24);
        Equal((16, 8), child.GetAbsolutePosition());
        child.MinWidth = "100"; child.MaxWidth = "3"; page.CalculateLayout(0, 0, 80, 24); Equal(3, child.ActualWidth);
        child.Width = "NaN"; child.MinWidth = "0"; page.CalculateLayout(0, 0, 80, 24); Equal(0, child.ActualWidth);
        Equal(5, page.ParseRelativeValue("12.5%", 40));
        return Task.CompletedTask;
    }

    private static Task Rgb()
    {
        using var picker = new ColorPicker();
        var original = Color.FromArgb(100, 254, 0, 10); picker.BeginEdit(original);
        for (int i = 0; i < 100; i++) picker.HandleKey(Key(ConsoleKey.RightArrow)); Equal(255, (int)picker.ColorValue.R);
        for (int i = 0; i < 100; i++) picker.HandleKey(Key(ConsoleKey.LeftArrow)); Equal(0, (int)picker.ColorValue.R);
        Equal(100, (int)picker.ColorValue.A);
        picker.HandleKey(Key(ConsoleKey.DownArrow)); picker.HandleKey(Key(ConsoleKey.End)); Equal(255, (int)picker.ColorValue.G);
        picker.HandleKey(Key(ConsoleKey.DownArrow)); picker.HandleKey(Key(ConsoleKey.D2, '2')); picker.HandleKey(Key(ConsoleKey.D5, '5')); picker.HandleKey(Key(ConsoleKey.D6, '6'));
        Equal(255, (int)picker.ColorValue.B);
        Check(picker.TrySetHex("#1234ef")); Check(!picker.TrySetHex("#nope")); Equal(18, (int)picker.ColorValue.R);
        int commits = 0, cancels = 0; picker.Confirmed += _ => commits++; picker.Cancelled += _ => cancels++;
        picker.Cancel(); Equal(original.ToArgb(), picker.ColorValue.ToArgb()); Equal(1, cancels);
        picker.TrySetHex("000000"); picker.Commit(); picker.HandleKey(Key(ConsoleKey.RightArrow)); picker.Cancel();
        Equal(Color.FromArgb(100, 0, 0, 0).ToArgb(), picker.ColorValue.ToArgb()); Equal(1, commits);
        return Task.CompletedTask;
    }

    private static Task Menus()
    {
        using var menu = new OptionsMenu();
        foreach (var key in new[] { ConsoleKey.Enter, ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.Home, ConsoleKey.End }) menu.HandleKey(Key(key));
        Equal(-1, menu.SelectedIndex);
        int selected = 0;
        menu.Options.Add(new MenuItem("Alpha", () => selected++));
        menu.Options.Add(new MenuItem("Disabled", () => throw new Exception("Disabled action")) { IsEnabled = false });
        menu.Options.Add(new MenuItem("Alpine", () => selected += 10));
        menu.HandleKey(Key(ConsoleKey.DownArrow)); Equal(2, menu.SelectedIndex);
        menu.HandleKey(Key(ConsoleKey.A, 'a')); Equal(0, menu.SelectedIndex);
        menu.HandleKey(Key(ConsoleKey.A, 'a')); Equal(2, menu.SelectedIndex);
        menu.HandleKey(Key(ConsoleKey.Enter)); Equal(10, selected);
        menu.Options.Clear(); menu.HandleKey(Key(ConsoleKey.Enter));
        return Task.CompletedTask;
    }

    private static Task Files()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "melonui-controls-" + Guid.NewGuid().ToString("N"));
        string cwd = Environment.CurrentDirectory;
        Directory.CreateDirectory(root);
        try
        {
            using var picker = new FilePicker(root);
            Equal(cwd, Environment.CurrentDirectory); picker.HandleKey(Key(ConsoleKey.Enter)); picker.HandleKey(Key(ConsoleKey.A, 'a'));
            picker.SelectionMode = FilePickerMode.Directory;
            int directories = 0; picker.OnDirectorySelected = () => directories++;
            Check(picker.SelectCurrentDirectory()); Equal(root, picker.Path); Equal(1, directories);
            Directory.CreateDirectory(System.IO.Path.Combine(root, "folder"));
            File.WriteAllText(System.IO.Path.Combine(root, "track.flac"), "data");
            File.WriteAllText(System.IO.Path.Combine(root, "ignore.txt"), "data");
            picker.Extensions.Add(".flac"); Check(picker.Refresh()); Equal(2, picker.Entries.Count);
            picker.HandleKey(Key(ConsoleKey.RightArrow)); Equal(System.IO.Path.Combine(root, "folder"), picker.CurrentDirectory);
            Check(picker.NavigateToParent());
            Check(!picker.NavigateTo(System.IO.Path.Combine(root, "missing"))); Equal(root, picker.CurrentDirectory); Check(picker.ErrorMessage is not null);
            picker.Refresh(); picker.SelectionMode = FilePickerMode.File;
            picker.HandleKey(Key(ConsoleKey.End)); picker.HandleKey(Key(ConsoleKey.Enter)); Check(picker.Path.EndsWith("track.flac"));
            Equal(cwd, Environment.CurrentDirectory);
        }
        finally
        {
            if (!System.IO.Path.GetFullPath(root).StartsWith(System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) throw new Exception("Unexpected test folder");
            Directory.Delete(root, true);
        }
        return Task.CompletedTask;
    }

    private static Task Buttons()
    {
        using var button = new Button(ConsoleKey.A); int calls = 0; button.OnPressed = () => calls++;
        button.HandleKey(Key(ConsoleKey.Enter)); button.HandleKey(Key(ConsoleKey.Spacebar, ' ')); button.HandleKey(Key(ConsoleKey.A, 'a')); Equal(3, calls);
        button.IsEnabled = false; button.HandleKey(Key(ConsoleKey.Enter)); button.PerformClick(); Equal(3, calls);
        button.IsEnabled = true; button.LockControls = true; button.HandleKey(Key(ConsoleKey.Enter)); Equal(3, calls);
        return Task.CompletedTask;
    }

    private static Task Progress()
    {
        using var progress = new ProgressBar { Style = ProgressBar.ProgressBarStyle.Loading, ProgressValue = .75f, ShowPercentage = true };
        progress.CalculateLayout(0, 0, 20, 5); progress.Render();
        Equal(.75f, progress.ProgressValue); Check(progress.ShowPercentage);
        foreach (var value in new[] { -100f, 0, 1, 100, float.NaN, float.PositiveInfinity })
        { progress.ProgressValue = value; progress.Style = ProgressBar.ProgressBarStyle.Ascii; progress.Render(); }
        return Task.CompletedTask;
    }

    private static Task Jobs()
    {
        using var log = new WorkLog { ShowProgressBar = true, ShowItemCount = true };
        var item = new WorkItem("scan", "A very long label", 5, true) { CompletedItems = -100 };
        Check(item.timeSinceRunning.IsRunning); log.Tasks.Add(item);
        foreach (int width in new[] { 1, 2, 3, 8, 80 })
        { log.CalculateLayout(0, 0, width, 5); log.Render(); item.CompletedItems = long.MaxValue; log.Render(); }
        item.Complete(); Check(!item.timeSinceRunning.IsRunning); log.Tasks.Clear(); log.Render();
        return Task.CompletedTask;
    }

    private static async Task Text()
    {
        await using var window = Window();
        var text = new TextBlock { Text = "[Color(1,2,3)]ab cd \n\nlongword1234", ShowBorder = false, Width = "4", Height = "8" };
        window.Elements.Add(text);
        var buffer = window.ComposeFrame(4, 8);
        Equal(Color.FromArgb(1, 2, 3).ToArgb(), buffer[0, 0].Foreground.ToArgb());
        var capture = window.CaptureText(width: 4, height: 8);
        Check(capture.Contains("long") && capture.Contains("word") && capture.Contains("1234"));
        text.Text = "trailing \x1b[0m "; window.CaptureText(width: 4, height: 8);
        text.Text = "\x1b[38;5;196mR\x1b[0mZ";
        buffer = window.ComposeFrame(4, 8);
        Equal(Color.Red.ToArgb(), buffer[0, 0].Foreground.ToArgb());
        Equal(text.Foreground.ToArgb(), buffer[0, 1].Foreground.ToArgb());
        text.SizeBasedOnText = true; text.Text = "hi"; window.CaptureText(width: 40, height: 8); Equal(2, text.ActualWidth);
    }

    private static Task Editing()
    {
        using var box = new TextBox { Text = "a😀e\u0301" };
        box.HandleKey(Key(ConsoleKey.End)); box.HandleKey(Key(ConsoleKey.Backspace)); Equal("a😀", box.Text);
        box.HandleKey(Key(ConsoleKey.Backspace)); Equal("a", box.Text);
        box.HandleKey(Key(ConsoleKey.B, 'b', ctrl: true)); Equal("a", box.Text);
        box.MaxLength = 2; box.HandleKey(Key(ConsoleKey.B, 'b')); box.HandleKey(Key(ConsoleKey.C, 'c')); Equal("ab", box.Text);
        box.ReadOnly = true; box.HandleKey(Key(ConsoleKey.Backspace)); Equal("ab", box.Text);
        box.ReadOnly = false; box.Clear(); box.Multiline = true; box.MaxLength = 0;
        box.HandleKey(Key(ConsoleKey.A, 'a')); box.HandleKey(Key(ConsoleKey.Enter)); box.HandleKey(Key(ConsoleKey.B, 'b'));
        Equal("a\nb", box.Text); int entered = 0; box.OnEnter = (_, _) => entered++;
        box.HandleKey(Key(ConsoleKey.Enter, shift: true)); Equal(1, entered);
        return Task.CompletedTask;
    }

    private static async Task Passwords()
    {
        await using var window = Window();
        var secret = Input("secret"); secret.HideCharacters = true; secret.Text = "sensitive";
        var other = Input("other", 15); window.Elements.Add(secret); window.Elements.Add(other);
        window.CaptureText(width: 40, height: 8); window.Focus(secret);
        window.HandleInput(Key(ConsoleKey.R, 'r', ctrl: true)); Check(!secret.IsCharactersHidden);
        Check(!window.CaptureText(width: 40, height: 8).Contains("sensitive"));
        Check(window.CaptureText(width: 40, height: 8, includeSensitive: true).Contains("sensitive"));
        window.HandleInput(Key(ConsoleKey.Tab)); Check(window.FocusedElement == other && secret.IsCharactersHidden);
    }

    private static async Task TabFocus()
    {
        await using var window = Window();
        var first = Input("first"); first.TabIndex = 2;
        var second = Input("second", 15); second.TabIndex = 1;
        var disabled = Input("disabled", 0, 4); disabled.IsEnabled = false;
        var hidden = Input("hidden", 15, 4); hidden.IsVisible = false;
        var clipped = Input("clipped", 1000, 0);
        foreach (var item in new[] { first, second, disabled, hidden, clipped }) window.Elements.Add(item);
        window.CaptureText(width: 40, height: 12); window.Focus(second);
        window.HandleInput(Key(ConsoleKey.Tab)); Check(window.FocusedElement == first);
        window.HandleInput(Key(ConsoleKey.Tab)); Check(window.FocusedElement == second);
        window.HandleInput(Key(ConsoleKey.Tab, shift: true)); Check(window.FocusedElement == first);
        first.IsEnabled = false; Check(window.FocusedElement == second);
        second.IsVisible = false; Check(window.FocusedElement is null);
    }

    private static async Task SpatialFocus()
    {
        await using var window = Window();
        var page = new MUIPage { X = "5", Y = "3", Width = "35", Height = "15", ShowBorder = true };
        var left = Input("left", 1, 1); var right = Input("right", 16, 1); var bottom = Input("bottom", 1, 7);
        page.Children.Add(left); page.Children.Add(right); page.Children.Add(bottom); window.Elements.Add(page);
        window.CaptureText(width: 60, height: 24); window.Focus(left);
        int z = left.Z;
        window.HandleInput(Key(ConsoleKey.RightArrow, alt: true)); Check(window.FocusedElement == right);
        window.Focus(left); window.HandleInput(Key(ConsoleKey.DownArrow, alt: true)); Check(window.FocusedElement == bottom); Equal(z, left.Z);
    }

    private static async Task InputScope()
    {
        await using var window = Window();
        var grid = new GridContainer(1, false, 2); int first = 0, second = 0, global = 0;
        var one = new Button(ConsoleKey.A) { OnPressed = () => first++ };
        var two = new Button(ConsoleKey.A) { OnPressed = () => second++ };
        grid.AddElement(one, 0, 0); grid.AddElement(two, 0, 1); window.Elements.Add(grid);
        window.RegisterKeyboardControl(ConsoleKey.A, () => global++, "global");
        window.CaptureText(width: 60, height: 8); window.Focus(one); window.HandleInput(Key(ConsoleKey.A, 'a'));
        Equal(1, first); Equal(0, second); Equal(0, global);
        window.EnableGlobalControls = false; window.HandleInput(Key(ConsoleKey.A, 'a')); Equal(2, first);
        one.RegisterKeyboardControl(ConsoleKey.D, () => one.Remove(), "remove self"); window.HandleInput(Key(ConsoleKey.D, 'd'));
        Check(window.FocusedElement == two);
    }

    private static async Task Modals()
    {
        await using var window = Window();
        var home = Input("home"); var previous = Input("previous", 15); window.Elements.Add(home); window.Elements.Add(previous);
        window.CaptureText(width: 60, height: 12); window.Focus(previous);
        int escaped = 0; window.RegisterKeyboardControl(ConsoleKey.Escape, () => escaped++, "quit");
        var modal = Page(); modal.Width = "30"; modal.Height = "8";
        var a = Input("a"); var b = Input("b", 15); modal.Children.Add(a); modal.Children.Add(b);
        var popup = window.ShowPopup(modal); window.CaptureText(width: 60, height: 12);
        window.HandleInput(Key(ConsoleKey.Tab)); Check(window.FocusedElement == b);
        window.HandleInput(Key(ConsoleKey.Tab)); Check(window.FocusedElement == a);
        var nested = window.ShowPopup(Input("nested")); window.HandleInput(Key(ConsoleKey.Escape));
        Check(window.FocusedElement == a && nested.Content.ParentWindow is null && escaped == 0); nested.Dispose();
        modal.IsVisible = false; Check(window.FocusedElement == previous);
        modal.IsVisible = true; window.CaptureText(width: 60, height: 12); Check(window.FocusedElement?.IsWithin(modal) == true);
        window.HandleInput(Key(ConsoleKey.Escape)); Check(window.FocusedElement == previous && escaped == 0);
        popup.Dispose(); window.HandleInput(Key(ConsoleKey.Escape)); Equal(1, escaped);
    }

    private static async Task QueueFocus()
    {
        await using var window = Window(); var queue = new QueueContainer(); var first = Input("first"); var second = Input("second");
        queue.Children.Add(first); queue.Children.Add(second); window.Elements.Add(queue);
        window.CaptureText(width: 40, height: 12); Check(window.FocusedElement == first);
        window.HandleInput(Key(ConsoleKey.Tab)); Check(window.FocusedElement == first);
        queue.PopElement(); window.CaptureText(width: 40, height: 12); Check(window.FocusedElement == second); first.Dispose();
    }

    private static async Task Overflow()
    {
        await using var window = Window(); int calls = 0;
        var panel = new ButtonPanel { Width = "20", Height = "1", Y = "11" };
        panel.AddButton(new Button(ConsoleKey.A) { Text = "A very long action", OnPressed = () => calls++ });
        window.Elements.Add(panel); window.CaptureText(width: 40, height: 12); window.Focus(panel);
        window.HandleInput(Key(ConsoleKey.Oem3, '`')); window.CaptureText(width: 40, height: 12);
        var menu = window.Elements.OfType<OverflowMenu>().Single(); Check(menu.ActualHeight > 2 && menu.ActualY < panel.ActualY);
        window.HandleInput(Key(ConsoleKey.Enter)); Equal(1, calls); Equal(0, window.Elements.OfType<OverflowMenu>().Count());
        Check(window.FocusedElement == panel);
    }

    private static async Task CachedControls()
    {
        await using var window = Window(); var page = Page(); var inner = Page(); var menu = new OptionsMenu { Width = "20", Height = "6" };
        inner.Children.Add(menu); page.Children.Add(inner); window.Elements.Add(page);
        window.CaptureText(width: 40, height: 12); menu.Options.Add(new MenuItem("New action", null));
        Check(window.CaptureText(width: 40, height: 12).Contains("New action"));
    }

    private static Task BufferClipping()
    {
        var buffer = new ConsoleBuffer(3, 2);
        foreach (int x in new[] { -5, -1, 0, 1, 3 }) foreach (int y in new[] { -1, 0, 2 }) buffer.WriteString(x, y, "😀界", Color.White, Color.Black);
        return Task.CompletedTask;
    }

    private static async Task Composition()
    {
        var lower = new ConsoleBuffer(3, 2); lower.Clear(Color.Blue);
        var upper = new ConsoleBuffer(3, 2); upper.Clear(Color.Transparent);
        upper.SetPixel(0, 0, 'X', Color.White, Color.Transparent);
        upper.SetPixel(1, 0, ' ', Color.Transparent, Color.FromArgb(128, 255, 0, 0));
        lower.WriteBuffer(0, 0, upper);
        Equal(Color.Blue.ToArgb(), lower[0, 0].Background.ToArgb());
        Equal('X', lower[0, 0].Character);
        Check(lower[0, 1].Background.R is >= 127 and <= 129 && lower[0, 1].Background.B is >= 126 and <= 128);
        await using var window = Window(); var page = Page(); page.Background = Color.Blue;
        page.Children.Add(new TextBlock { Text = "A", Width = "1", Height = "1", ShowBorder = false, Background = Color.Transparent });
        page.Children.Add(new TextBlock { Width = "0", Height = "0", ShowBorder = false, Background = Color.Red });
        window.Elements.Add(page); var frame = window.ComposeFrame(4, 3);
        Equal('A', frame[0, 0].Character); Equal(Color.Blue.ToArgb(), frame[0, 0].Background.ToArgb());
    }

    private static Task UnicodeOutput()
    {
        var buffer = new ConsoleBuffer(5, 2); buffer.Clear(Color.Black);
        buffer.WriteString(0, 0, "A😀BC", Color.White, Color.Black);
        buffer.WriteString(0, 1, "界DEF", Color.White, Color.Black);
        var utf8 = new UnixRenderer(5, 2).BuildFrame(buffer.buffer);
        var plain = System.Text.RegularExpressions.Regex.Replace(System.Text.Encoding.UTF8.GetString(utf8.Span), @"\x1b\[[0-9;]*[mH]", "");
        Equal("A😀BC\r\n界DEF", plain);
        Check(buffer.Screenshot(false).Contains("A😀BC") && !plain.Contains('\ufffd'));
        return Task.CompletedTask;
    }

    private static async Task StaleOverflow()
    {
        await using var window = Window(); int calls = 0;
        var panel = new ButtonPanel { Width = "20", Height = "1" };
        var button = new Button { Text = "A very long action", OnPressed = () => calls++ }; panel.AddButton(button);
        window.Elements.Add(panel); window.CaptureText(width: 40, height: 12); window.Focus(panel);
        window.HandleInput(Key(ConsoleKey.Oem3)); window.CaptureText(width: 40, height: 12);
        button.IsEnabled = false; window.HandleInput(Key(ConsoleKey.Enter)); Equal(0, calls);
        panel.RemoveButton(button); window.HandleInput(Key(ConsoleKey.Enter)); Equal(0, calls); button.Dispose();
    }

    private static Task DisposingCallbacks()
    {
        var menu = new OptionsMenu(); menu.Options.Add(new MenuItem("close", menu.Dispose)); menu.HandleKey(Key(ConsoleKey.Enter)); Check(menu.IsDisposed);
        var button = new Button(); button.OnPressed = button.Dispose; button.HandleKey(Key(ConsoleKey.Enter)); Check(button.IsDisposed);
        var rgb = new ColorPicker(); rgb.Confirmed += _ => rgb.Dispose(); rgb.Commit(); Check(rgb.IsDisposed);
        rgb = new ColorPicker(); rgb.Cancelled += _ => rgb.Dispose(); rgb.Cancel(); Check(rgb.IsDisposed);
        return Task.CompletedTask;
    }

    private static async Task CachedResize()
    {
        await using var window = Window(); var outer = Page(); var inner = Page();
        inner.Children.Add(new TextBlock { Text = "abcdefgh", ShowBorder = false });
        outer.Children.Add(inner); window.Elements.Add(outer);
        window.CaptureText(width: 8, height: 4);
        var narrow = window.CaptureText(width: 4, height: 4);
        Check(narrow.Contains("abcd") && narrow.Contains("efgh"), "Cached text did not reflow after shrinking.");
        var wide = window.CaptureText(width: 8, height: 4); Check(wide.Contains("abcdefgh"));
    }
}
