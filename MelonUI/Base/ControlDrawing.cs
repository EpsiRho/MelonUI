using System.Drawing;
using System.Globalization;
using System.Text;

namespace MelonUI.Base;

internal static class ControlDrawing
{
    internal static string Clip(string? text, int width)
    {
        if (width <= 0 || string.IsNullOrEmpty(text)) return "";
        var output = new StringBuilder();
        var parts = StringInfo.GetTextElementEnumerator(text);
        int used = 0;
        while (parts.MoveNext())
        {
            string part = parts.GetTextElement();
            if (part.Any(char.IsControl)) part = " ";
            int size = Math.Max(1, ConsoleBuffer.GetStringWidth(part));
            if (used + size > width) break;
            output.Append(part);
            used += size;
        }
        return output.ToString();
    }

    internal static void Text(ConsoleBuffer buffer, int x, int y, string? text, int width, Color foreground, Color background)
    {
        if (y < 0 || y >= buffer.Height || width <= 0) return;
        buffer.WriteString(x, y, Clip(text, Math.Min(width, buffer.Width - x)), foreground, background);
    }

    internal static float Unit(float value) => float.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
}
