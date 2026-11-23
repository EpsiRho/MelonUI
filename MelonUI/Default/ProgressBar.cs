using System.Diagnostics;
using System.Drawing;
using MelonUI.Attributes;
using MelonUI.Base;

namespace MelonUI.Default;

public partial class ProgressBar : UIElement
{
    [Binding] private float progressValue;
    [Binding] private string text = "";
    [Binding] private bool showPercentage;
    [Binding] private bool animateProgress;
    [Binding] private ProgressBarStyle style;
    [Binding] private Color progressColor = Color.Green;
    [Binding] private Color emptyColor = Color.DarkGray;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private float _displayed;
    private double _lastRender;
    public float? PreviousProgress { get; private set; }
    public int LoadingPlace { get; private set; }
    public DateTime LastUpdate { get; private set; }
    public enum ProgressBarStyle { Solid, Segmented, Dotted, Blocks, Ascii, Loading }
    public ProgressBar() { ConsiderForFocus = false; Height = "3"; EnableCaching = false; }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        if (a.Width == 0 || a.Height == 0) return;
        float target = ControlDrawing.Unit(ProgressValue);
        double now = _clock.Elapsed.TotalSeconds;
        if (PreviousProgress != target) { PreviousProgress = target; LastUpdate = DateTime.UtcNow; }
        _displayed = AnimateProgress ? _displayed + (target - _displayed) * (float)Math.Min(1, Math.Max(0, now - _lastRender) * 12) : target;
        _lastRender = now;
        int y = a.Y;
        string label = (Text ?? "") + (ShowPercentage ? $" {target:P0}" : "");
        if (a.Height > 1 && label.Length > 0) { ControlDrawing.Text(buffer, a.X, y++, label.Trim(), a.Width, Foreground, Background); }
        var chars = Style switch
        {
            ProgressBarStyle.Segmented => ('▰', '▱'), ProgressBarStyle.Dotted => ('●', '○'),
            ProgressBarStyle.Blocks => ('■', '□'), ProgressBarStyle.Ascii => ('=', ' '), _ => ('█', '░')
        };
        bool ascii = Style == ProgressBarStyle.Ascii && a.Width >= 2;
        int width = a.Width - (ascii ? 2 : 0), startX = a.X + (ascii ? 1 : 0);
        if (ascii) { buffer.SetPixel(a.X, y, '[', Foreground, Background); buffer.SetPixel(a.X + a.Width - 1, y, ']', Foreground, Background); }
        int filled = (int)Math.Round(width * _displayed);
        LoadingPlace = width <= 1 ? 0 : (int)((now * 15) % (width * 2 - 2));
        if (LoadingPlace >= width) LoadingPlace = width * 2 - 2 - LoadingPlace;
        for (int x = 0; x < width; x++)
        {
            bool full = Style == ProgressBarStyle.Loading ? Math.Abs(x - LoadingPlace) <= 1 : x < filled;
            buffer.SetPixel(startX + x, y, full ? chars.Item1 : chars.Item2, full ? ProgressColor : EmptyColor, Background);
        }
    }
}
