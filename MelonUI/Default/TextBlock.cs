using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using MelonUI.Attributes;
using MelonUI.Base;
using MelonUI.Enums;

namespace MelonUI.Default;

public partial class TextBlock : UIElement
{
    [Binding] private string text = "";
    [Binding] private Alignment textAlignment;
    [Binding] private bool sizeBasedOnText;
    private sealed record Glyph(string Text, int Width, Color Foreground, Color Background);
    private static readonly Regex Tokens = new(@"\x1b\[[0-9;]*m|\[Color\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*\)\]|\[ColorReset\]", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    public TextBlock() { ConsiderForFocus = false; }
    public override void CalculateLayout(int x, int y, int width, int height)
    {
        base.CalculateLayout(x, y, width, height);
        if (!SizeBasedOnText) return;
        int edge = ShowBorder ? 2 : 0;
        var lines = BuildLines(Math.Max(1, ActualMaxWidth - edge));
        int oldWidth = ActualWidth, oldHeight = ActualHeight;
        ActualWidth = Math.Clamp((lines.Count == 0 ? 0 : lines.Max(line => line.Sum(g => g.Width))) + edge, ActualMinWidth, ActualMaxWidth);
        ActualHeight = Math.Clamp(lines.Count + edge, ActualMinHeight, ActualMaxHeight);
        if (XYAlignment is Alignment.TopRight or Alignment.CenterRight or Alignment.BottomRight) ActualX += oldWidth - ActualWidth;
        else if (XYAlignment is Alignment.TopCenter or Alignment.Centered or Alignment.BottomCenter) ActualX += (oldWidth - ActualWidth) / 2;
        if (XYAlignment is Alignment.BottomLeft or Alignment.BottomCenter or Alignment.BottomRight) ActualY += oldHeight - ActualHeight;
        else if (XYAlignment is Alignment.CenterLeft or Alignment.Centered or Alignment.CenterRight) ActualY += (oldHeight - ActualHeight) / 2;
        LayoutChildren();
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        if (a.Width == 0 || a.Height == 0) return;
        var lines = BuildLines(a.Width);
        int vertical = TextAlignment is Alignment.BottomLeft or Alignment.BottomCenter or Alignment.BottomRight ? Math.Max(0, a.Height - lines.Count) :
            TextAlignment is Alignment.CenterLeft or Alignment.Centered or Alignment.CenterRight ? Math.Max(0, (a.Height - lines.Count) / 2) : 0;
        for (int i = 0; i < lines.Count && i + vertical < a.Height; i++)
        {
            int lineWidth = lines[i].Sum(g => g.Width);
            int horizontal = TextAlignment is Alignment.TopRight or Alignment.CenterRight or Alignment.BottomRight ? a.Width - lineWidth :
                TextAlignment is Alignment.TopCenter or Alignment.Centered or Alignment.BottomCenter ? (a.Width - lineWidth) / 2 : 0;
            int x = a.X + Math.Max(0, horizontal);
            foreach (var glyph in lines[i])
            {
                if (x + glyph.Width > a.X + a.Width) break;
                buffer.WriteString(x, a.Y + i + vertical, glyph.Text, glyph.Foreground, glyph.Background);
                x += glyph.Width;
            }
        }
    }
    private List<List<Glyph>> BuildLines(int width)
    {
        var lines = new List<List<Glyph>> { new() };
        if (width <= 0) return lines;
        var fg = IsFocused ? FocusedForeground : Foreground;
        var bg = IsFocused ? FocusedBackground : Background;
        var initialFg = fg; var initialBg = bg;
        bool bold = false;
        string value = (Text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        int cursor = 0, used = 0;
        void Append(string plain)
        {
            var parts = StringInfo.GetTextElementEnumerator(plain);
            while (parts.MoveNext())
            {
                string part = parts.GetTextElement();
                if (part == "\n") { lines.Add(new()); used = 0; continue; }
                if (part.Any(char.IsControl)) part = " ";
                int size = Math.Max(1, ConsoleBuffer.GetStringWidth(part));
                if (size > width) { part = "?"; size = 1; }
                if (used + size > width)
                {
                    int space = lines[^1].FindLastIndex(g => g.Text == " ");
                    if (space > 0)
                    {
                        var carry = lines[^1].Skip(space + 1).ToList();
                        lines[^1].RemoveRange(space, lines[^1].Count - space);
                        lines.Add(carry); used = carry.Sum(g => g.Width);
                    }
                    else { lines.Add(new()); used = 0; }
                }
                var drawFg = bold ? Color.FromArgb(fg.A, Math.Max(fg.R, (byte)128), Math.Max(fg.G, (byte)128), Math.Max(fg.B, (byte)128)) : fg;
                lines[^1].Add(new Glyph(part, size, drawFg, bg)); used += size;
            }
        }
        foreach (Match token in Tokens.Matches(value))
        {
            Append(value[cursor..token.Index]);
            if (token.Value == "[ColorReset]") { fg = initialFg; bg = initialBg; bold = false; }
            else if (token.Value.StartsWith("[Color("))
            {
                var colors = ParseColors(token.Value);
                if (colors.Count > 0) fg = colors[0].color; else Append(token.Value);
            }
            else
            {
                var codes = token.Value[2..^1].Split(';').Select(s => int.TryParse(s, out int code) ? code : -1).ToArray();
                for (int i = 0; i < codes.Length; i++)
                {
                    if (codes[i] is 0 or -1) { fg = initialFg; bg = initialBg; bold = false; }
                    else if (codes[i] == 1) bold = true;
                    else if (codes[i] == 22) bold = false;
                    else if (codes[i] == 39) fg = initialFg;
                    else if (codes[i] == 49) bg = initialBg;
                    else if (codes[i] is 38 or 48 && i + 4 < codes.Length && codes[i + 1] == 2 && codes.Skip(i + 2).Take(3).All(c => c is >= 0 and <= 255))
                    {
                        var color = Color.FromArgb(codes[i + 2], codes[i + 3], codes[i + 4]);
                        if (codes[i] == 38) fg = color; else bg = color;
                        i += 4;
                    }
                    else if (codes[i] is 38 or 48 && i + 2 < codes.Length && codes[i + 1] == 5 && codes[i + 2] is >= 0 and <= 255)
                    {
                        var color = IndexedColor(codes[i + 2]);
                        if (codes[i] == 38) fg = color; else bg = color;
                        i += 2;
                    }
                    else if (codes[i] is >= 30 and <= 37) fg = BasicColors[codes[i] - 30];
                    else if (codes[i] is >= 40 and <= 47) bg = BasicColors[codes[i] - 40];
                    else if (codes[i] is >= 90 and <= 97) fg = BrightColors[codes[i] - 90];
                    else if (codes[i] is >= 100 and <= 107) bg = BrightColors[codes[i] - 100];
                }
            }
            cursor = token.Index + token.Length;
        }
        Append(value[cursor..]);
        return lines;
    }
    private static readonly Color[] BasicColors = [Color.Black, Color.Red, Color.Green, Color.Yellow, Color.Blue, Color.Magenta, Color.Cyan, Color.White];
    private static readonly Color[] BrightColors = [Color.Gray, Color.FromArgb(255,85,85), Color.FromArgb(85,255,85), Color.FromArgb(255,255,85), Color.FromArgb(85,85,255), Color.FromArgb(255,85,255), Color.FromArgb(85,255,255), Color.White];
    internal static Color IndexedColor(int index)
    {
        if (index < 8) return BasicColors[index];
        if (index < 16) return BrightColors[index - 8];
        if (index >= 232) { int value = 8 + (index - 232) * 10; return Color.FromArgb(value, value, value); }
        index -= 16;
        static int Channel(int value) => value == 0 ? 0 : 55 + value * 40;
        return Color.FromArgb(Channel(index / 36), Channel(index / 6 % 6), Channel(index % 6));
    }
    public static bool ContainsAnsiCodes(string input) => !string.IsNullOrEmpty(input) && Tokens.Matches(input).Any(m => m.Value.StartsWith('\x1b'));
    public static List<(Color color, int startIndex, int endIndex)> ParseColors(string input)
    {
        var result = new List<(Color, int, int)>();
        if (string.IsNullOrEmpty(input)) return result;
        foreach (Match match in Tokens.Matches(input))
        {
            if (!match.Value.StartsWith("[Color(")) continue;
            var values = match.Value[7..^2].Split(',').Select(s => int.TryParse(s, out int v) ? v : -1).ToArray();
            if (values.Length == 3 && values.All(v => v is >= 0 and <= 255))
                result.Add((Color.FromArgb(values[0], values[1], values[2]), match.Index, match.Index + match.Length - 1));
        }
        return result;
    }
}
