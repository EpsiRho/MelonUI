using System.Diagnostics;
using System.Drawing;
using MelonUI.Base;

namespace MelonUI.Default;

/// <summary>Three RGB sliders with live preview, commit and cancellation.</summary>
public class ColorPicker : UIElement
{
    private Color _colorValue = Color.Cyan;
    private Color _originalColor = Color.Cyan;
    private readonly Stopwatch _repeat = Stopwatch.StartNew();
    private ConsoleKey _lastKey;
    private int _repeats;
    private string _digits = "";
    public int SelectedChannel { get; private set; }
    public Color ColorValue
    {
        get => (Color)(GetBoundValue(nameof(ColorValue), _colorValue) ?? _colorValue);
        set
        {
            var before = ColorValue;
            SetBoundValue(nameof(ColorValue), value, ref _colorValue);
            if (before.ToArgb() != ColorValue.ToArgb()) ColorChanged?.Invoke(ColorValue);
        }
    }
    public Color OriginalColor => _originalColor;
    public event Action<Color>? ColorChanged;
    public event Action<Color>? Confirmed;
    public event Action<Color>? Cancelled;

    public ColorPicker() { Height = "8"; }
    public void BeginEdit(Color color)
    {
        VerifyAccess(); _originalColor = color; SelectedChannel = 0; _repeats = 0; _digits = ""; ColorValue = color;
        if (!IsDisposed) Invalidate();
    }
    public void Commit() { VerifyAccess(); _originalColor = ColorValue; Confirmed?.Invoke(ColorValue); }
    public void Cancel() { VerifyAccess(); ColorValue = _originalColor; Cancelled?.Invoke(ColorValue); }

    protected override void OnAttachedToWindow(MelonUI.Managers.ConsoleWindowManager window)
    { _originalColor = ColorValue; base.OnAttachedToWindow(window); }
    protected override void OnPropertyChanged(string name)
    {
        if (name == nameof(IsFocused)) { _repeats = 0; _lastKey = ConsoleKey.NoName; _digits = ""; }
        base.OnPropertyChanged(name);
    }
    public override bool TryHandleKey(ConsoleKeyInfo key)
    {
        if (LockControls || !IsEffectivelyVisible || !IsEffectivelyEnabled) return false;
        if (base.TryHandleKey(key)) return true;
        if ((key.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) return false;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow: SelectedChannel = Math.Max(0, SelectedChannel - 1); _repeats = 0; _digits = ""; Invalidate(); return true;
            case ConsoleKey.DownArrow: SelectedChannel = Math.Min(2, SelectedChannel + 1); _repeats = 0; _digits = ""; Invalidate(); return true;
            case ConsoleKey.Enter: Commit(); return true;
            case ConsoleKey.Escape: Cancel(); return true;
            case ConsoleKey.Home: SetChannel(0); return true;
            case ConsoleKey.End: SetChannel(255); return true;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                _digits = "";
                _repeats = key.Key == _lastKey && _repeat.ElapsedMilliseconds < 120 ? _repeats + 1 : 0;
                _lastKey = key.Key; _repeat.Restart();
                int step = key.Modifiers.HasFlag(ConsoleModifiers.Shift) ? 10 : Math.Clamp(1 + _repeats / 5, 1, 25);
                int value = SelectedChannel switch { 0 => ColorValue.R, 1 => ColorValue.G, _ => ColorValue.B };
                SetChannel(value + (key.Key == ConsoleKey.LeftArrow ? -step : step)); return true;
            default:
                if (key.KeyChar is >= '0' and <= '9')
                {
                    if (_digits.Length >= 3) _digits = "";
                    _digits += key.KeyChar;
                    SetChannel(int.Parse(_digits, System.Globalization.CultureInfo.InvariantCulture)); return true;
                }
                return false;
        }
    }
    public bool TrySetHex(string? value)
    {
        VerifyAccess();
        value = value?.Trim().TrimStart('#');
        if (value?.Length != 6 || !int.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int rgb)) return false;
        ColorValue = Color.FromArgb(ColorValue.A, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255); return true;
    }
    private void SetChannel(int value)
    {
        value = Math.Clamp(value, 0, 255);
        var color = ColorValue;
        ColorValue = Color.FromArgb(color.A, SelectedChannel == 0 ? value : color.R,
            SelectedChannel == 1 ? value : color.G, SelectedChannel == 2 ? value : color.B);
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        if (a.Width == 0 || a.Height == 0) return;
        var color = ColorValue;
        for (int channel = 0; channel < 3 && channel < a.Height; channel++)
        {
            int value = channel switch { 0 => color.R, 1 => color.G, _ => color.B };
            var tint = channel switch { 0 => Color.FromArgb(255, 255 - value, 255 - value),
                1 => Color.FromArgb(255 - value, 255, 255 - value), _ => Color.FromArgb(255 - value, 255 - value, 255) };
            int barWidth = Math.Max(0, a.Width - 9);
            string bar = "";
            if (barWidth > 0)
            {
                int marker = (int)Math.Round(value / 255d * (barWidth - 1));
                bar = "[" + new string('─', marker) + "█" + new string('─', barWidth - marker - 1) + "] ";
            }
            string text = $"{(channel == SelectedChannel && IsFocused ? '>' : ' ')}{"RGB"[channel]} {bar}{value:000}";
            ControlDrawing.Text(buffer, a.X, a.Y + channel, text, a.Width, tint, Background);
        }
        if (a.Height > 3) ControlDrawing.Text(buffer, a.X, a.Y + 3, $"Old #{_originalColor.R:X2}{_originalColor.G:X2}{_originalColor.B:X2}", a.Width, _originalColor, Background);
        if (a.Height > 4) ControlDrawing.Text(buffer, a.X, a.Y + 4, $"New #{color.R:X2}{color.G:X2}{color.B:X2}  ███", a.Width, color, Background);
        if (a.Height > 5) ControlDrawing.Text(buffer, a.X, a.Y + 5, "↑↓ channel  ←→ value  Shift:10  Enter:save  Esc:cancel", a.Width, Foreground, Background);
    }
    protected override void DisposeCore() { ColorChanged = null; Confirmed = null; Cancelled = null; base.DisposeCore(); }
}

/// <summary>Explicit RGB name for consumers; ColorPicker remains the short control name.</summary>
public class RgbColorPicker : ColorPicker { }
