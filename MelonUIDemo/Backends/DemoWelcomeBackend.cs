using System.Threading.Channels;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;

namespace MelonUIDemo.Backends;

public static class DemoWelcomeBackend
{
    // Public properties are used by the existing experimental MXML bindings.
    public static ConsoleWindowManager CWM { get; set; } = null!;
    public static string RenderTypeDisplay => CWM.UsePlatformSpecificRenderer ? Win32Renderer.IsSupported ? "Win32" : "Unix" : "SysV2";
    public static string ConsoleSizeDisplay => $"W/H: {Console.WindowWidth}/{Console.WindowHeight}";
    public static string FrameTimeDisplay { get; set; } = "Frame time: unknown";
    public static DateTime LastFrameTime = DateTime.Now;
    public static List<double> FrameTimes = new();
    public static string WelcomeText { get; set; } = "MelonUI experimental MXML viewer\nEnter a file to watch for changes.";
    public static string ErrorInfo { get; set; } = "";
    public static string MXML { get; set; } = "";
    public static string MXMLPath { get; set; } = "";
    public static MUIPage? WatchedPage;
    public static FileSystemWatcher? PageWatcher;
    public static string ShaderSource { get; set; } = "";
    private static Channel<bool>? _changes;
    private static CancellationTokenSource? _watchStop;
    private static Task? _watchTask;

    public static Func<ConsoleKeyInfo, bool> CloseErrorDiagFunc { get; set; } = _ => true;
    public static Action CloseErrorDiag { get; set; } = () =>
    {
        if (CWM.Find<UIElement>("ErrorInfo") is { } error) error.IsVisible = false;
        if (CWM.Find<TextBox>("FileInput") is { } input) CWM.Focus(input);
    };

    public static Action<string, TextBox> FilePathInput { get; set; } = (path, input) =>
    {
        path = path.Replace("\\", "/").Replace("\"", "");
        try
        {
            MXML = File.ReadAllText(path);
            MXMLPath = Path.GetFullPath(path);
            input.Text = path;
            CloseWelcomeScreen();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (CWM.Find<TextBlock>("ErrorInfo") is { } error)
            {
                error.Text = $"Could not open {path}: {ex.Message}";
                error.IsVisible = true;
                CWM.Focus(error);
            }
        }
    };

    public static void CloseWelcomeScreen()
    {
        if (!ReplacePage(MXML)) return;
        CWM.Find<UIElement>("DemoWelcomePage")?.Dispose();
        CreateWatcher();
    }

    public static void CreateWatcher()
    {
        PageWatcher?.Dispose();
        var previousStop = _watchStop;
        previousStop?.Cancel();
        var stop = _watchStop = new CancellationTokenSource();
        var changes = _changes = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest
        });
        var path = MXMLPath;
        // One background reader coalesces filesystem notifications. Only its short UI action touches the tree.
        var previous = _watchTask;
        _watchTask = Task.Run(async () =>
        {
            try
            {
                if (previous is not null) await previous;
                previousStop?.Dispose();
                await foreach (var change in changes.Reader.ReadAllAsync(stop.Token))
                {
                    await Task.Delay(100, stop.Token);
                    while (changes.Reader.TryRead(out _)) { }
                    try
                    {
                        var markup = await File.ReadAllTextAsync(path, stop.Token);
                        await CWM.InvokeAsync(() => { MXML = markup; ReplacePage(markup); });
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        await CWM.InvokeAsync(() => SetStatus($"Could not reload page: {ex.Message}"));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) when (!CWM.IsRunning) { }
        });
        PageWatcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
        };
        PageWatcher.Changed += (_, _) => changes.Writer.TryWrite(true);
        PageWatcher.Created += (_, _) => changes.Writer.TryWrite(true);
        PageWatcher.Renamed += (_, _) => changes.Writer.TryWrite(true);
        PageWatcher.EnableRaisingEvents = true;
    }

    public static void CompilePageChanges() => _changes?.Writer.TryWrite(true);

    private static bool ReplacePage(string markup)
    {
        var replacement = WatchedPage ?? new MUIPage();
        if (!replacement.Compile(markup, MXMLPath.Length == 0 ? null : MXMLPath))
        {
            SetStatus($"Page compilation failed: {replacement.GetSimpleCompilerDisplay()}");
            if (WatchedPage is null) replacement.Dispose();
            return false;
        }
        WatchedPage = replacement;
        if (replacement.ParentWindow is null) CWM.Elements.Add(replacement);
        CWM.Focus(replacement);
        SetStatus("Page compiled");
        return true;
    }

    private static void SetStatus(string text)
    {
        if (CWM.Find<TextBlock>("Status") is { } status) status.Text = text;
    }

    public static async Task StopWatcherAsync()
    {
        PageWatcher?.Dispose();
        PageWatcher = null;
        if (_watchTask is not null)
        {
            if (!_watchTask.IsCompleted) _watchStop?.Cancel();
            await _watchTask;
        }
        _watchStop?.Dispose();
        _watchTask = null;
        _watchStop = null;
        _changes = null;
    }
}
