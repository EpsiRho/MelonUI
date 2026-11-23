using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using MelonUI.Managers;
using Pastel;

namespace MelonUIDemo;

internal static class CSharpDemo
{
    internal static async Task RunAsync(bool smoke)
    {
        await using var window = new ConsoleWindowManager
        {
            Title = "MelonUI C# demo", TargetFrameRate = smoke ? 10 : 60
        };
        var page = new MUIPage { Name = "Home", Width = "100%", Height = "100%", ShowBorder = false };
        var status = new TextBlock
        {
            Name = "Status", Text = "Ready", Width = "100%", Height = "1",
            XYAlignment = Alignment.BottomLeft, ShowBorder = false, ConsiderForFocus = false
        };
        page.Children.Add(new TextBlock
        {
            Text = "MelonUI C# composition\nC: detach/restore cached page   P: popup   R: renderer   Esc: quit",
            Width = "100%", Height = "4", ShowBorder = false, ConsiderForFocus = false
        });
        page.Children.Add(status);
        window.Elements.Add(page);

        // A detached subtree keeps its state; its owner is responsible for eventually disposing it.
        using var details = new MUIPage { Name = "CachedDetails", Y = "5", Width = "90%", Height = "6" };
        details.Children.Add(new TextBlock
        {
            Text = "This page can be removed and added again.\nChildren get their window automatically.".Pastel("#a90fbf"),
            Width = "100%", Height = "100%", ShowBorder = false, ConsiderForFocus = false
        });
        page.Children.Add(details);
        window.RegisterKeyboardControl(ConsoleKey.C, () =>
        {
            if (details.Parent is null) page.Children.Add(details);
            else details.Remove();
            status.Text = details.Parent is null ? "Page cached" : "Page restored";
        }, "Cache/restore page");

        PopupHandle? popup = null;
        window.RegisterKeyboardControl(ConsoleKey.P, () =>
        {
            if (popup is not null) { popup.Dispose(); popup = null; return; }
            var content = new TextBlock
            {
                Name = "Popup", Text = "A normal control shown as a modal popup. Press Esc to close.",
                Width = "60%", Height = "5", XYAlignment = Alignment.Centered
            };
            popup = window.ShowPopup(content);
        }, "Open/close popup");
        window.RegisterKeyboardControl(ConsoleKey.R, () =>
        {
            window.UsePlatformSpecificRenderer = !window.UsePlatformSpecificRenderer;
            status.Text = $"Platform renderer: {window.UsePlatformSpecificRenderer}";
        }, "Toggle renderer");
        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");

        if (smoke)
        {
            var step = 0;
            window.Updating += (_, _) =>
            {
                switch (step++)
                {
                    case 1: details.Remove(); break;
                    case 2: page.Children.Add(details); break;
                    case 3:
                        popup = window.ShowPopup(new TextBlock
                        {
                            Text = "Popup smoke check", Width = "30", Height = "4", XYAlignment = Alignment.Centered
                        });
                        break;
                    case 4: popup!.Dispose(); popup = null; break;
                    case 5: window.UsePlatformSpecificRenderer = false; break;
                    case 6:
                        if (!window.CaptureText().Contains("Background update applied"))
                            throw new InvalidOperationException("The status control was missing from the composed frame.");
                        break;
                }
                if (step > 8) window.Stop();
            };
        }
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; window.Stop(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var run = window.RunAsync();
            // Real background work would finish before queuing its short UI update here.
            var update = Task.Run(() => window.InvokeAsync(() =>
                window.Find<TextBlock>("Status")!.Text = "Background update applied on the UI thread"));
            try { await Task.WhenAll(run, update); }
            catch (TaskCanceledException) { /* Stopped before the queued action ran. */ }
        }
        finally
        {
            window.Stop();
            Console.CancelKeyPress -= cancel;
            popup?.Dispose();
        }
    }
}
