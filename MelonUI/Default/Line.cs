using System.Drawing;
using MelonUI.Attributes;
using MelonUI.Base;

namespace MelonUI.Default;

public partial class Line : UIElement
{
    [Binding] private string x1 = "0";
    [Binding] private string y1 = "0";
    [Binding] private string x2 = "100%";
    [Binding] private string y2 = "0";
    [Binding] private bool antiAliased;
    [Binding] private float intensity = 1;
    [Binding] private float softness = 1;
    [Binding] private bool doubleWidth;
    public int ActualX1 { get; private set; }
    public int ActualY1 { get; private set; }
    public int ActualX2 { get; private set; }
    public int ActualY2 { get; private set; }
    public Line() { Height = "1"; ShowBorder = false; ConsiderForFocus = false; RespectBackgroundOnDraw = false; }
    public override void CalculateLayout(int x, int y, int width, int height)
    {
        base.CalculateLayout(x, y, width, height);
        var a = ContentRect;
        ActualX1 = a.X + Math.Clamp(ParseRelativeValue(X1, Math.Max(0, a.Width - 1)), 0, Math.Max(0, a.Width - 1));
        ActualY1 = a.Y + Math.Clamp(ParseRelativeValue(Y1, Math.Max(0, a.Height - 1)), 0, Math.Max(0, a.Height - 1));
        ActualX2 = a.X + Math.Clamp(ParseRelativeValue(X2, Math.Max(0, a.Width - 1)), 0, Math.Max(0, a.Width - 1));
        ActualY2 = a.Y + Math.Clamp(ParseRelativeValue(Y2, Math.Max(0, a.Height - 1)), 0, Math.Max(0, a.Height - 1));
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        if (AntiAliased) { DrawAntialiasedLine(buffer, ActualX1, ActualY1, ActualX2, ActualY2); return; }
        int x = ActualX1, y = ActualY1, dx = Math.Abs(ActualX2 - x), dy = -Math.Abs(ActualY2 - y);
        int sx = x < ActualX2 ? 1 : -1, sy = y < ActualY2 ? 1 : -1, error = dx + dy;
        while (true)
        {
            Plot(buffer, x, y, 1);
            if (x == ActualX2 && y == ActualY2) break;
            int doubled = error * 2;
            if (doubled >= dy) { error += dy; x += sx; }
            if (doubled <= dx) { error += dx; y += sy; }
        }
    }
    public void DrawAntialiasedLine(ConsoleBuffer buffer, int x0, int y0, int x1, int y1)
    {
        VerifyAccess();
        // Bound work to this buffer even for coordinates supplied directly by a consumer.
        x0 = Math.Clamp(x0, 0, buffer.Width - 1); x1 = Math.Clamp(x1, 0, buffer.Width - 1);
        y0 = Math.Clamp(y0, 0, buffer.Height - 1); y1 = Math.Clamp(y1, 0, buffer.Height - 1);
        int steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        if (steps == 0) { Plot(buffer, x0, y0, 1); return; }
        bool horizontal = Math.Abs(x1 - x0) >= Math.Abs(y1 - y0);
        for (int i = 0; i <= steps; i++)
        {
            double x = x0 + (x1 - x0) * (i / (double)steps), y = y0 + (y1 - y0) * (i / (double)steps);
            if (horizontal)
            { int floor = (int)Math.Floor(y); Plot(buffer, (int)Math.Round(x), floor, 1 - (y - floor)); if (y > floor) Plot(buffer, (int)Math.Round(x), floor + 1, y - floor); }
            else
            { int floor = (int)Math.Floor(x); Plot(buffer, floor, (int)Math.Round(y), 1 - (x - floor)); if (x > floor) Plot(buffer, floor + 1, (int)Math.Round(y), x - floor); }
        }
    }
    private void Plot(ConsoleBuffer buffer, int x, int y, double coverage)
    {
        var a = ContentRect;
        if (x < a.X || y < a.Y || x >= a.X + a.Width || y >= a.Y + a.Height) return;
        float strength = ControlDrawing.Unit(Intensity);
        float softness = float.IsFinite(Softness) && Softness > 0 ? Softness : 1;
        double amount = Math.Pow(Math.Clamp(coverage * strength, 0, 1), 1 / softness);
        var color = Color.FromArgb((int)(Foreground.A * amount), Foreground.R, Foreground.G, Foreground.B);
        buffer.SetPixel(x, y, '█', color, Background);
        if (DoubleWidth && x + 1 < a.X + a.Width) buffer.SetPixel(x + 1, y, '█', color, Background);
    }
}
