using System.Text;
using MelonUI.Base;

namespace MelonUI.Managers;

// Kept internal so terminal plumbing does not become part of the normal CWM setup API.
internal interface IConsoleTerminal
{
    void Begin();
    (int Width, int Height) GetSize();
    bool TryReadKey(out ConsoleKeyInfo key);
    void SetTitle(string title);
    void Present(ConsoleBuffer frame, bool usePlatformRenderer);
    void End();
}

internal sealed class ConsoleTerminal : IConsoleTerminal
{
    private StreamWriter? _output;
    private Encoding? _encoding;
    private bool? _cursorVisible;
    private string? _title;
    private ConsoleBuffer? _presentation;

    public void Begin()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            throw new InvalidOperationException("MelonUI needs an interactive terminal to run. Constructing elements and capturing frames do not require one.");
        _encoding = Console.OutputEncoding;
        if (OperatingSystem.IsWindows()) _cursorVisible = Console.CursorVisible;
        if (OperatingSystem.IsWindows()) _title = Console.Title;
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        _output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false), 65535, leaveOpen: true);
    }

    public (int Width, int Height) GetSize()
    {
        var size = Win32Renderer.GetConsoleWindowSize();
        return (Math.Max(1, size.width), Math.Max(1, size.height));
    }

    public bool TryReadKey(out ConsoleKeyInfo key)
    {
        key = default;
        if (!Console.KeyAvailable) return false;
        key = Console.ReadKey(intercept: true);
        return true;
    }

    public void SetTitle(string title)
    {
        try { Console.Title = title; }
        catch (Exception ex) when (ex is IOException or PlatformNotSupportedException or System.Security.SecurityException) { }
    }

    public void Present(ConsoleBuffer frame, bool usePlatformRenderer)
    {
        // Keep backend state for this run even though composition produces a clean frame each time.
        _presentation ??= new ConsoleBuffer(frame.Width, frame.Height);
        if (_presentation.Width != frame.Width || _presentation.Height != frame.Height)
            _presentation.Resize(frame.Width, frame.Height);
        _presentation.buffer = frame.buffer;
        _presentation.RenderToConsole(_output!, usePlatformRenderer);
    }

    public void End()
    {
        try { _output?.Dispose(); }
        finally
        {
            _presentation?.WRenderer?.Dispose();
            _presentation = null;
            _output = null;
            if (_encoding is not null) Console.OutputEncoding = _encoding;
            if (_cursorVisible.HasValue) Console.CursorVisible = _cursorVisible.Value;
            else if (_encoding is not null) Console.CursorVisible = true;
            if (_title is not null) SetTitle(_title);
            _encoding = null;
            _cursorVisible = null;
            _title = null;
        }
    }
}
