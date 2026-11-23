using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using MelonUI.Managers;

namespace MelonUIDemo;

public sealed class FormBackend : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private string _name = "Melon listener", _status = "Ready";
    private Color _accent = Color.Cyan;
    public string Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); } }
    public string Status { get => _status; set { _status = value; PropertyChanged?.Invoke(this, new(nameof(Status))); } }
    public Color Accent { get => _accent; set { _accent = value; PropertyChanged?.Invoke(this, new(nameof(Accent))); } }
    public void Save() => Status = $"Saved {Name}";
    public void ChooseColor(Color color) => Status = $"Accent #{color.R:X2}{color.G:X2}{color.B:X2}";
}

internal static class MxmlControlsDemo
{
    // The editable document cannot overwrite the host's diagnostics or cover its footer.
    private sealed class PreviewHost : MUIPage
    {
        public override void CalculateLayout(int x, int y, int width, int height) =>
            base.CalculateLayout(x, y, width, Math.Max(0, height - 4));
    }

    internal static async Task RunAsync(bool smoke)
    {
        var source = Path.Combine(Path.GetTempPath(), "MelonUI-MXML", Guid.NewGuid().ToString("N"), "Demo.mxml");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        var starter = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Pages", "Controls.xml"));
        await File.WriteAllTextAsync(source, starter);

        await using var window = new ConsoleWindowManager { Title = "MelonUI live MXML", TargetFrameRate = smoke ? 12 : 60 };
        var backend = new FormBackend();
        var page = new MUIPage();
        page.BackendObjects.Add("App", backend);
        if (!page.Compile(starter, source)) throw new InvalidOperationException(page.GetSimpleCompilerDisplay());
        var host = new PreviewHost { ShowBorder = false, Children = { page } };
        var status = new TextBlock { Height = "4", ShowBorder = false, XYAlignment = Alignment.BottomLeft, Z = 1 };
        window.Elements.Add(host);
        window.Elements.Add(status);
        string? editorError = null;
        void SetStatus(string message, bool error)
        {
            status.Text = message + (editorError is null ? "" : $"\n{editorError}") + $"\nFile: {source}";
            status.Foreground = error ? Color.OrangeRed : Color.LightGray;
        }
        SetStatus("Ready. Save in Notepad to update | F5: reload | Escape: quit", false);
        await using var preview = new MxmlFilePreview(source, starter, window, page, SetStatus);
        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
        window.RegisterKeyboardControl(ConsoleKey.F5, preview.ForceReload, "Reload file");
        if (!smoke)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var start = new ProcessStartInfo("notepad.exe") { UseShellExecute = false };
                    start.ArgumentList.Add(source);
                    using var editor = Process.Start(start);
                    if (editor is null) throw new InvalidOperationException("Notepad did not start.");
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
                {
                    editorError = $"Could not open Notepad: {ex.Message} Open the file above in your editor.";
                    SetStatus("Ready. Watching for saved changes.", true);
                }
            }
            else SetStatus("Open this file in your editor; saving updates the preview. F5: reload | Escape: quit", false);
        }

        using var stop = new CancellationTokenSource(smoke ? TimeSpan.FromSeconds(20) : Timeout.InfiniteTimeSpan);
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; window.Stop(); };
        Console.CancelKeyPress += cancel;
        var run = window.RunAsync(stop.Token);
        var watcher = preview.Start();
        var smokeTest = smoke ? SmokeAsync(window, page, backend, preview, status, starter, stop.Token) : null;
        try
        {
            var completed = await Task.WhenAny(run, watcher, smokeTest ?? run);
            await completed;
            if (smokeTest is not null && completed != smokeTest)
                throw new InvalidOperationException("The live MXML smoke test stopped before completing.");
        }
        finally
        {
            window.Stop();
            await stop.CancelAsync();
            Console.CancelKeyPress -= cancel;
            try { await run; }
            finally
            {
                // Observe the smoke task even when the terminal or watcher fails first.
                if (smokeTest is not null)
                    try { await smokeTest; } catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            }
        }
        if (smoke) Console.WriteLine("PASS: watched-file edits, invalid/successful compilation, atomic saves, delete/recreate recovery, forced reload, retained backend values and shutdown.");
        Console.WriteLine($"MXML file kept at: {source}");
    }

    private static async Task SmokeAsync(ConsoleWindowManager window, MUIPage page, FormBackend backend,
        MxmlFilePreview preview, TextBlock status, string starter, CancellationToken token)
    {
        TextBox entry = null!;
        await window.InvokeAsync(() =>
        {
            entry = page.Find<TextBox>("Name")!;
            window.Focus(entry);
            entry.Text = "Ada";
            page.Find<Button>("Save")!.PerformClick();
            Check(backend.Status == "Saved Ada" && page.Find<TextBlock>("Details")!.IsVisible, "MXML callback/action failed.");
            var picker = page.Find<RgbColorPicker>("Accent")!;
            picker.TrySetHex("12ABEF"); picker.Commit();
            Check(backend.Status == "Accent #12ABEF", "MXML CLR event binding failed.");
        });
        await File.WriteAllTextAsync(preview.FilePath, "<MUIPage><Unknown/></MUIPage>", token);
        await UntilAsync(() => preview.CompletedReloads == 1 && page.Failed);
        await window.InvokeAsync(() => Check(!entry.IsDisposed && page.Find<TextBox>("Name") == entry &&
            status.Text.Contains("Last working preview retained"), "Invalid file edit replaced the preview or hid diagnostics."));

        var updated = starter.Replace("MXML live preview", "Live file update", StringComparison.Ordinal);
        await File.WriteAllTextAsync(preview.FilePath, updated, token);
        await UntilAsync(() => preview.CompletedReloads == 2 && !page.Failed);
        await window.InvokeAsync(() => Check(entry.IsDisposed && page.Find<TextBox>("Name")!.Text == "Ada" &&
            window.CaptureText(width: 100, height: 30).Contains("Live file update"), "Saved changes were not rendered or backend data was reset."));

        var atomic = updated.Replace("Live file update", "Atomic save update", StringComparison.Ordinal);
        await File.WriteAllTextAsync(preview.FilePath + ".saving", atomic, token);
        File.Move(preview.FilePath + ".saving", preview.FilePath, overwrite: true);
        await UntilAsync(() => preview.CompletedReloads == 3 && page.MXML == atomic);
        File.Delete(preview.FilePath);
        await UntilAsync(() => status.Text.Contains("Could not read file:"));
        await File.WriteAllTextAsync(preview.FilePath, atomic, token);
        await UntilAsync(() => preview.CompletedReloads == 4 && !page.Failed && !status.Text.Contains("Could not read file:"));
        preview.ForceReload();
        await UntilAsync(() => preview.CompletedReloads == 5 && !page.Failed);
        TextBox current = null!;
        await window.InvokeAsync(() => current = page.Find<TextBox>("Name")!);
        // Redundant filesystem notifications must not reset unsaved control/focus state.
        preview.RequestReload();
        await Task.Delay(400, token);
        await window.InvokeAsync(() => Check(preview.CompletedReloads == 5 && page.Find<TextBox>("Name") == current,
            "Duplicate save notifications replaced the live tree."));
        preview.RequestReload(); // Exit with a pending notification to exercise cancellation/cleanup.

        async Task UntilAsync(Func<bool> condition)
        {
            var deadline = Stopwatch.StartNew();
            while (deadline.Elapsed < TimeSpan.FromSeconds(5))
            {
                token.ThrowIfCancellationRequested();
                bool ready = false;
                await window.InvokeAsync(() => ready = condition());
                if (ready) return;
                await Task.Delay(25, token);
            }
            throw new InvalidOperationException("Timed out waiting for a watched MXML file update.");
        }
        static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
