using MelonUI.Base;
using System.Globalization;
using System.Text;

namespace MelonUI.Docs;

// A complete custom control: generated properties, bounded drawing and keyboard input.
public partial class Badge : UIElement
{
    [MelonUI.Attributes.Binding] private string _caption = "Ready";
    [MelonUI.Attributes.Binding] private bool _active;
    [MelonUI.Attributes.Binding] private Action? _onActivated;

    public Badge() { Height = "3"; Width = "28"; }

    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var area = ContentRect;
        if (area.Width == 0 || area.Height == 0) return;
        var color = Active ? FocusedForeground : Foreground;
        var text = $"{(Active ? "●" : "○")} {Caption}";
        var visible = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(text);
        int remaining = area.Width;
        while (elements.MoveNext())
        {
            var next = elements.GetTextElement();
            int width = ConsoleBuffer.GetTextElementWidth(next);
            if (width > remaining) break;
            visible.Append(next);
            remaining -= width;
        }
        buffer.WriteString(area.X, area.Y, visible.ToString(), color, Background);
    }

    public override bool TryHandleKey(ConsoleKeyInfo key)
    {
        if (LockControls || !IsEffectivelyVisible || !IsEffectivelyEnabled) return false;
        if (base.TryHandleKey(key)) return true;
        if (key.Key != ConsoleKey.Enter || key.Modifiers != 0) return false;
        Active = !Active;
        OnActivated?.Invoke();
        return true;
    }
}
