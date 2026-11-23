using MelonUI.Attributes;
using MelonUI.Base;

namespace MelonUI.Default;

/// <summary>Displays supplied peak amplitudes. Owns no audio devices, playback or file decoding.</summary>
public partial class Waveform : UIElement
{
    [Binding] private float scaleAmmount = 1;
    [Binding] private float[] waveformData = [];
    [Binding] private float progressValue;
    public Waveform() { ConsiderForFocus = false; EnableCaching = false; }
    public Waveform(float[] samples) : this() { WaveformData = samples ?? throw new ArgumentNullException(nameof(samples)); }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        var samples = WaveformData;
        if (a.Width == 0 || a.Height == 0 || samples is null || samples.Length == 0) return;
        float scale = float.IsFinite(ScaleAmmount) ? Math.Max(0, ScaleAmmount) : 1;
        int center = a.Y + (a.Height - 1) / 2;
        int progress = (int)(ControlDrawing.Unit(ProgressValue) * a.Width);
        for (int x = 0; x < a.Width; x++)
        {
            int start = (int)((long)x * samples.Length / a.Width);
            int end = Math.Max(start + 1, (int)((long)(x + 1) * samples.Length / a.Width));
            float peak = 0;
            for (int i = start; i < Math.Min(samples.Length, end); i++)
                if (float.IsFinite(samples[i])) peak = Math.Max(peak, Math.Abs(samples[i]));
            int height = (int)Math.Round(ControlDrawing.Unit(peak * scale) * (a.Height - 1) / 2);
            for (int y = Math.Max(a.Y, center - height); y <= Math.Min(a.Y + a.Height - 1, center + height); y++)
                buffer.SetPixel(a.X + x, y, height == 0 ? '─' : '█', x < progress ? FocusedForeground : Foreground, Background);
        }
    }
}
