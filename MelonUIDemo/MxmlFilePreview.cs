using System.Threading.Channels;
using MelonUI.Base;
using MelonUI.Managers;

namespace MelonUIDemo;

/// <summary>Demo-only file watching; all compilation and tree changes run on the UI owner.</summary>
internal sealed class MxmlFilePreview : IAsyncDisposable
{
    private readonly ConsoleWindowManager _window;
    private readonly MUIPage _page;
    private readonly Action<string, bool> _status;
    private readonly FileSystemWatcher _watcher;
    private readonly CancellationTokenSource _stop = new();
    private readonly Channel<bool> _changes = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest
    });
    private string? _lastMarkup;
    private Task? _worker;
    private int _forceReload;
    public string FilePath { get; }
    public int CompletedReloads { get; private set; } // Read/updated only on the UI owner.

    public MxmlFilePreview(string path, string initialMarkup, ConsoleWindowManager window, MUIPage page,
        Action<string, bool> status)
    {
        FilePath = Path.GetFullPath(path);
        _lastMarkup = initialMarkup;
        _window = window;
        _page = page;
        _status = status;
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath)!, Path.GetFileName(FilePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
        };
        _watcher.Changed += (_, _) => RequestReload();
        _watcher.Created += (_, _) => RequestReload();
        _watcher.Deleted += (_, _) => RequestReload();
        _watcher.Renamed += (_, _) => RequestReload();
        _watcher.Error += (_, _) => ForceReload(); // Resync after a dropped notification/overflow.
        _watcher.EnableRaisingEvents = true;
    }

    public void RequestReload() => _changes.Writer.TryWrite(true);

    public Task Start()
    {
        if (_worker is not null) throw new InvalidOperationException("The file preview is already running.");
        return _worker = RunAsync();
    }

    private async Task RunAsync()
    {
        var token = _stop.Token;
        try
        {
            while (await _changes.Reader.WaitToReadAsync(token))
            {
                // Notepad and other editors can emit several events or replace the file on save.
                do
                {
                    while (_changes.Reader.TryRead(out _)) { }
                    await Task.Delay(150, token);
                } while (_changes.Reader.TryPeek(out _));

                string markup;
                try { markup = await ReadSavedFileAsync(token); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _lastMarkup = null;
                    if (!await OnUiAsync(() => _status($"Could not read file: {ex.Message} Save again or press F5 to retry.", true))) return;
                    continue;
                }
                // A duplicate notification should not replace a focused control or reset local UI state.
                if (Interlocked.Exchange(ref _forceReload, 0) == 0 && markup == _lastMarkup) continue;
                _lastMarkup = markup;
                if (!await OnUiAsync(() =>
                {
                    bool compiled = _page.Compile(markup, FilePath);
                    CompletedReloads++;
                    var diagnostic = _page.CompilerFocusedMessage;
                    _status(compiled
                        ? $"Compiled at {DateTime.Now:T} ({_page.CompilationFinished.TotalMilliseconds:F1} ms). Save in Notepad to update."
                        : $"{diagnostic.Code} at {diagnostic.MxmlLineNumber.Line}:{diagnostic.MxmlLineNumber.Position}: {diagnostic.Message} Last working preview retained.", !compiled);
                })) return;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || !_window.IsRunning) { }
    }

    public void ForceReload()
    {
        // Enqueue a forced reload without racing the background reader's deduplication state.
        Interlocked.Exchange(ref _forceReload, 1);
        RequestReload();
    }
    private async Task<string> ReadSavedFileAsync(CancellationToken token)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await File.ReadAllTextAsync(FilePath, token);
            }
            catch (IOException) when (attempt < 5) { await Task.Delay(100, token); }
        }
    }

    private async Task<bool> OnUiAsync(Action action)
    {
        Task work;
        // Enqueue can reject work between Stop() and IsRunning becoming false. Only catch this
        // synchronous rejection; exceptions raised inside the UI action must still be observed.
        try { work = _window.InvokeAsync(action); }
        catch (InvalidOperationException) { return false; }
        await work;
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        _watcher.Dispose();
        await _stop.CancelAsync();
        try { if (_worker is not null) await _worker; }
        finally { _stop.Dispose(); }
    }
}
