using MelonUI.Attributes;
using MelonUI.Base;

namespace MelonUI.Default;

public partial class Button : UIElement
{
    [Binding] private string text = "";
    [Binding] private Action? onPressed;
    public readonly ConsoleKey _key;
    public Button() : this(ConsoleKey.Enter) { }
    public Button(ConsoleKey key)
    {
        _key = key;
        Height = "3";
        ShowBorder = false;
    }
    public void PerformClick()
    {
        VerifyAccess();
        if (IsEffectivelyVisible && IsEffectivelyEnabled && !LockControls) OnPressed?.Invoke();
    }
    public override bool TryHandleKey(ConsoleKeyInfo key)
    {
        if (LockControls || !IsEffectivelyVisible || !IsEffectivelyEnabled) return false;
        if (base.TryHandleKey(key)) return true;
        if ((key.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift)) != 0) return false;
        if (key.Key != _key && key.Key is not (ConsoleKey.Enter or ConsoleKey.Spacebar)) return false;
        PerformClick(); return true;
    }
    internal string Caption => _key == ConsoleKey.Enter ? $"[ {Text} ]" : $"[({GetKeyDisplay(_key)}) {Text}]";
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var area = ContentRect;
        string caption = ControlDrawing.Clip(Caption, area.Width);
        int x = area.X + Math.Max(0, (area.Width - ConsoleBuffer.GetStringWidth(caption)) / 2);
        ControlDrawing.Text(buffer, x, area.Y + area.Height / 2, caption, area.Width,
            !IsEffectivelyEnabled ? System.Drawing.Color.Gray : IsFocused ? FocusedForeground : Foreground,
            IsFocused ? FocusedBackground : Background);
    }
    public static string GetKeyDisplay(ConsoleKey key) => key switch
    {
        ConsoleKey.Spacebar => "Space", ConsoleKey.Enter => "Enter", ConsoleKey.Escape => "Esc",
        ConsoleKey.Backspace => "Back", ConsoleKey.UpArrow => "↑", ConsoleKey.DownArrow => "↓",
        ConsoleKey.LeftArrow => "←", ConsoleKey.RightArrow => "→", ConsoleKey.Oem3 => "`",
        _ => key.ToString()
    };
}
