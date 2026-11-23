using System.Drawing;
using MelonUI.Base;
using MelonUI.Default;

namespace MelonUI.Helpers;

/// <summary>Uses the same ANSI/color and Unicode layout as TextBlock.</summary>
public static class ColoredStringParser
{
    public static ConsoleBuffer ParseColoredString(string? input)
    {
        using var text = new TextBlock { Text = input ?? "", ShowBorder = false, SizeBasedOnText = true };
        text.CalculateLayout(0, 0, int.MaxValue, int.MaxValue);
        return text.Render();
    }
    public static Color Convert256ColorCode(int code)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(code);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(code, 255);
        return TextBlock.IndexedColor(code);
    }
}
