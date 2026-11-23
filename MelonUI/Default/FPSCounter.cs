using System.Diagnostics;
using MelonUI.Base;
using MelonUI.Managers;

namespace MelonUI.Default;

public class FPSCounter : UIElement
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private int _frames;
    private int _fps;
    private long _lastSequence = -1;
    public FPSCounter() { ConsiderForFocus = false; Width = "7"; Height = "4"; EnableCaching = false; }
    protected override void OnAttachedToWindow(ConsoleWindowManager window)
    {
        _lastSequence = -1; _frames = 0; _clock.Restart();
        window.FrameRendered += OnFrameRendered;
        base.OnAttachedToWindow(window);
    }
    protected override void OnDetachedFromWindow(ConsoleWindowManager window)
    { window.FrameRendered -= OnFrameRendered; base.OnDetachedFromWindow(window); }
    public void OnFrameRendered(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        if (sender is ConsoleWindowManager window)
        { if (ParentWindow != window || _lastSequence == window.FrameNumber) return; _lastSequence = window.FrameNumber; }
        _frames++;
        double elapsed = _clock.Elapsed.TotalSeconds;
        if (elapsed >= 1) { _fps = (int)Math.Round(_frames / elapsed); _frames = 0; _clock.Restart(); }
        Invalidate();
    }
    public int GetFPS() => _fps;
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        ControlDrawing.Text(buffer, a.X, a.Y, a.Height <= 1 ? $"FPS {_fps}" : "FPS", a.Width, Foreground, Background);
        if (a.Height > 1) ControlDrawing.Text(buffer, a.X, a.Y + 1, $"{_fps:000}", a.Width, Foreground, Background);
    }
}
