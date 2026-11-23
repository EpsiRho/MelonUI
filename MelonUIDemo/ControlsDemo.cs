using System.Diagnostics;
using System.Drawing;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using MelonUI.Managers;

namespace MelonUIDemo;

internal static class ControlsDemo
{
    internal static async Task RunAsync(bool smoke)
    {
        await using var window = new ConsoleWindowManager { Title = "MelonUI controls", TargetFrameRate = smoke ? 12 : 30 };
        var page = new MUIPage { ShowBorder = false };
        var status = new TextBlock { Name = "Status", Text = "Ready", Height = "1", ShowBorder = false, XYAlignment = Alignment.BottomLeft };
        page.Children.Add(new TextBlock
        {
            Text = "MelonUI control gallery | Tab / Shift+Tab: focus | Alt+arrows: spatial focus\nArrows: edit | Enter: activate | F2: RGB popup | F3: renderer | Esc: close popup / quit",
            Height = "2", ShowBorder = false
        });
        var grid = new GridContainer(2, false, 3, 3) { Y = "2", Height = "85%", ShowBorder = false };
        page.Children.Add(grid); page.Children.Add(status); window.Elements.Add(page);

        var fields = new MUIPage();
        fields.Children.Add(new TextBlock { Text = "Name / password / notes", Height = "1", ShowBorder = false });
        var text = new TextBox { Name = "Name", Text = "Melon listener", Y = "1", Height = "3" };
        var password = new TextBox { HideCharacters = true, Text = "demo secret", Y = "4", Height = "3" };
        var notes = new TextBox { Multiline = true, Text = "Unicode: 界 😀\nSecond line", Y = "7", Height = "4" };
        text.OnTextChanged = (value, _) => status.Text = $"Name: {value}";
        fields.Children.Add(text); fields.Children.Add(password); fields.Children.Add(notes); grid.AddElement(fields, 0, 0);

        var actions = new MUIPage();
        var menu = new OptionsMenu { MenuName = "Actions", Height = "75%", ShowBorder = false };
        PopupHandle? popup = null;
        var color = new RgbColorPicker { Name = "RGB", ShowBorder = true };
        color.BeginEdit(Color.FromArgb(80, 210, 130));
        color.ColorChanged += value => status.Foreground = value;
        color.Confirmed += value => status.Text = $"RGB saved: #{value.R:X2}{value.G:X2}{value.B:X2}";
        color.Cancelled += _ => status.Text = "RGB edit cancelled";
        void OpenColor()
        {
            if (popup is { IsOpen: true }) return;
            popup?.Dispose();
            var picker = new RgbColorPicker { Width = "65%", Height = "8", XYAlignment = Alignment.Centered };
            picker.BeginEdit(color.ColorValue);
            picker.Confirmed += value => { color.BeginEdit(value); status.Text = "Popup RGB saved"; popup?.Dispose(); popup = null; };
            picker.Cancelled += _ => { status.Text = "Popup RGB cancelled"; popup?.Dispose(); popup = null; };
            popup = window.ShowPopup(picker);
        }
        menu.Options.Add(new MenuItem("Choose RGB in popup", OpenColor));
        menu.Options.Add(new MenuItem("Toggle name field", () => text.IsEnabled = !text.IsEnabled));
        menu.Options.Add(new MenuItem("Disabled action", null) { IsEnabled = false });
        var buttons = new ButtonPanel { XYAlignment = Alignment.BottomLeft, Height = "1", MaxLines = 1 };
        foreach (var label in new[] { "Save", "Reset", "More options", "Overflow action" })
        {
            var caption = label;
            buttons.AddButton(new Button { Text = caption, OnPressed = () => status.Text = $"Clicked {caption}" });
        }
        actions.Children.Add(menu); actions.Children.Add(buttons); grid.AddElement(actions, 0, 1);
        grid.AddElement(new FilePicker { SelectionMode = FilePickerMode.FileOrDirectory }, 0, 2);
        grid.AddElement(color, 1, 0);

        var visuals = new MUIPage();
        var progress = new ProgressBar { Text = "Animated progress", AnimateProgress = true, ShowPercentage = true, Height = "2", ShowBorder = false };
        var wave = new Waveform(Enumerable.Range(0, 256).Select(i => (float)(Math.Sin(i * .13) * Math.Cos(i * .021))).ToArray())
        { Y = "2", Height = "65%", ShowBorder = false };
        visuals.Children.Add(progress); visuals.Children.Add(wave);
        visuals.Children.Add(new Line { XYAlignment = Alignment.BottomLeft, Height = "1", X2 = "100%", AntiAliased = true, Foreground = Color.Cyan });
        visuals.Children.Add(new FPSCounter { Width = "9", Height = "1", ShowBorder = false, XYAlignment = Alignment.BottomRight });
        grid.AddElement(visuals, 1, 1);
        var jobs = new MUIPage(); var log = new WorkLog { Height = "70%", ShowBorder = false, ShowItemCount = true, ShowProgressBar = true };
        var scan = new WorkItem("scan", "Scanning music", 100, true);
        log.Tasks.Add(new WorkItem("ready", "Library loaded", false) { Status = WorkStatus.Completed }); log.Tasks.Add(scan);
        log.Tasks.Add(new WorkItem("next", "Waiting for artwork", false)); jobs.Children.Add(log);
        var queue = new QueueContainer { Height = "3", XYAlignment = Alignment.BottomLeft, ShowBorder = false };
        for (int i = 1; i <= 3; i++)
        {
            var index = i;
            queue.QueueElement(new Button { Text = $"Queue card {index}: Enter advances", Height = "3", OnPressed = () =>
            {
                var popped = queue.PopElement();
                if (popped is not null) queue.QueueElement(popped);
            } });
        }
        jobs.Children.Add(queue); grid.AddElement(jobs, 1, 2);
        window.RegisterKeyboardControl(ConsoleKey.F2, OpenColor, "RGB popup");
        window.RegisterKeyboardControl(ConsoleKey.F3, () => window.UsePlatformSpecificRenderer = !window.UsePlatformSpecificRenderer, "Renderer");
        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
        var clock = Stopwatch.StartNew(); int step = 0;
        window.Updating += (_, _) =>
        {
            progress.ProgressValue = wave.ProgressValue = (float)(clock.Elapsed.TotalSeconds % 10 / 10);
            scan.CompletedItems = (int)(progress.ProgressValue * 100);
            if (!smoke) return;
            switch (step++)
            {
                case 1: window.Focus(text); break;
                case 2: OpenColor(); break;
                case 3: ((ColorPicker)popup!.Content).TrySetHex("#123456"); break;
                case 4: ((ColorPicker)popup!.Content).Commit(); break;
                case 5: OpenColor(); break;
                case 6: ((ColorPicker)popup!.Content).Cancel(); break;
                case 7: grid.Width = "33%"; grid.Height = "30%"; break;
                case 8: grid.Width = "100%"; grid.Height = "85%"; break;
                case 9: window.UsePlatformSpecificRenderer = false; break;
                case 10:
                    if (window.CaptureText().Contains("demo secret")) throw new InvalidOperationException("Password leaked in capture.");
                    break;
                case 12: window.Stop(); break;
            }
        };
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; window.Stop(); };
        Console.CancelKeyPress += cancel;
        try { await window.RunAsync(); }
        finally { Console.CancelKeyPress -= cancel; popup?.Dispose(); }
        if (smoke) Console.WriteLine("PASS: controls gallery, RGB commit/cancel, resize, masked capture and renderer switch.");
    }
}
