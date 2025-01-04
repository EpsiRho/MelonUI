using MelonUI.Base;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace MelonUI.Helpers
{
    public static class ColoredStringParser
    {
        private static (int matchLength, string codes) ParseAnsiSequence(string input, int startIndex)
        {
            if (startIndex >= input.Length || input[startIndex] != '\x1B' ||
                startIndex + 1 >= input.Length || input[startIndex + 1] != '[')
                return (0, null);

            int currentIndex = startIndex + 2;
            var codeBuilder = new StringBuilder();

            while (currentIndex < input.Length)
            {
                char c = input[currentIndex];

                if (c == 'm')
                {
                    // End of sequence found
                    return (currentIndex - startIndex + 1, codeBuilder.ToString());
                }
                else if ((c >= '0' && c <= '9') || c == ';')
                {
                    codeBuilder.Append(c);
                    currentIndex++;
                }
                else
                {
                    // Invalid character in sequence
                    return (0, null);
                }
            }

            // Reached end of string without finding 'm'
            return (0, null);
        }

        private static readonly Dictionary<int, Color> StandardColors = new()
        {
            { 0, Color.Black },
            { 1, Color.Red },
            { 2, Color.Green },
            { 3, Color.Yellow },
            { 4, Color.Blue },
            { 5, Color.Magenta },
            { 6, Color.Cyan },
            { 7, Color.White },
        };

        private static readonly Dictionary<int, Color> BrightColors = new()
        {
            { 90,  Color.FromArgb(128, 128, 128) },  // Bright Black
            { 91,  Color.FromArgb(255,  85,  85) },  // Bright Red
            { 92,  Color.FromArgb( 85, 255,  85) },  // Bright Green
            { 93,  Color.FromArgb(255, 255,  85) },  // Bright Yellow
            { 94,  Color.FromArgb( 85,  85, 255) },  // Bright Blue
            { 95,  Color.FromArgb(255,  85, 255) },  // Bright Magenta
            { 96,  Color.FromArgb( 85, 255, 255) },  // Bright Cyan
            { 97,  Color.FromArgb(255, 255, 255) },  // Bright White
        };
        private class ColorState
        {
            public Color Foreground { get; set; } = Color.White;
            public Color Background { get; set; } = Color.Black;
            public bool IsBold { get; set; } = false;
            public bool IsItalic { get; set; } = false;
            public bool IsUnderline { get; set; } = false;

            // Used when we see 38 or 48. We then check what the next codes are.
            public bool IsExtendedForeground { get; set; } = false;
            public bool IsExtendedBackground { get; set; } = false;

            public Color GetEffectiveForeground()
            {
                if (!IsBold) return Foreground;
                return GetBrightColor(Foreground);
            }
        }
        public static ConsoleBuffer ParseColoredString(string input)
        {
            // Edge case: empty or null string -> zero-width buffer
            if (string.IsNullOrEmpty(input))
                return new ConsoleBuffer(0, 1);

            try
            {
                // We'll parse line by line, storing each line as a list of (char, fg, bg).
                var lines = new List<List<(char c, Color fg, Color bg)>>()
                {
                    new List<(char, Color, Color)>()
                };

                var state = new ColorState();
                int currentLine = 0;

                for (int i = 0; i < input.Length; i++)
                {
                    char ch = input[i];

                    // Handle CR, ignore it (common in Windows CRLF)
                    if (ch == '\r')
                    {
                        continue;
                    }

                    // Handle LF -> new line
                    if (ch == '\n')
                    {
                        // Start a new line
                        lines.Add(new List<(char, Color, Color)>());
                        currentLine++;
                        continue;
                    }

                    // Check for ANSI escape
                    if (ch == '\x1B')
                    {
                        // Attempt to match an ANSI sequence
                        var match = ParseAnsiSequence(input, i);
                        if (match.matchLength > 0)
                        {
                            // The captured group 1 has the code(s) like "1", "31", "38;5;129", etc.
                            ProcessAnsiSequence(match.codes.Split(";"), state);

                            // Advance i so we skip over the entire ANSI sequence.
                            i += match.matchLength - 1;
                            continue;
                        }
                    }

                    // If not an ANSI code (or if the match fails), treat as a normal character
                    var fg = state.GetEffectiveForeground();
                    var bg = state.Background;
                    lines[currentLine].Add((ch, fg, bg));
                }

                // Figure out how wide the buffer needs to be
                int maxWidth = 0;
                foreach (var line in lines)
                {
                    if (line.Count > maxWidth) maxWidth = line.Count;
                }
                int height = lines.Count;

                // Create the final buffer
                ConsoleBuffer buffer = new ConsoleBuffer(maxWidth, height);

                // Fill in each line
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < lines[y].Count; x++)
                    {
                        var cell = lines[y][x];
                        buffer.SetPixel(x, y, cell.c, cell.fg, cell.bg);
                    }
                }

                return buffer;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static void ProcessAnsiSequence(string[] codes, ColorState state)
        {
            if (codes == null || codes.Length == 0 || string.IsNullOrEmpty(codes[0]))
            {
                // If no codes, it's effectively a full reset
                ResetState(state);
                return;
            }

            for (int i = 0; i < codes.Length; i++)
            {
                if (!int.TryParse(codes[i], out int code))
                    continue; // skip invalid codes

                // If we previously saw "38" or "48", we might be in "extended" color mode
                // e.g. 38;5;123 => true 256-color
                // e.g. 38;2;255;100;0 => 24-bit color
                if (state.IsExtendedForeground)
                {
                    // 256-color or 24-bit color
                    if (code == 5 && (i + 1) < codes.Length && int.TryParse(codes[i + 1], out int colorCode256))
                    {
                        // 38;5;N
                        state.Foreground = Convert256ColorCode(colorCode256);
                        i++;
                    }
                    else if (code == 2 && (i + 3) < codes.Length
                             && int.TryParse(codes[i + 1], out int r)
                             && int.TryParse(codes[i + 2], out int g)
                             && int.TryParse(codes[i + 3], out int b))
                    {
                        // 38;2;R;G;B
                        state.Foreground = Color.FromArgb(r, g, b);
                        i += 3;
                    }
                    state.IsExtendedForeground = false;
                    continue;
                }

                if (state.IsExtendedBackground)
                {
                    // 256-color or 24-bit color
                    if (code == 5 && (i + 1) < codes.Length && int.TryParse(codes[i + 1], out int colorCode256))
                    {
                        // 48;5;N
                        state.Background = Convert256ColorCode(colorCode256);
                        i++;
                    }
                    else if (code == 2 && (i + 3) < codes.Length
                             && int.TryParse(codes[i + 1], out int r)
                             && int.TryParse(codes[i + 2], out int g)
                             && int.TryParse(codes[i + 3], out int b))
                    {
                        // 48;2;R;G;B
                        state.Background = Color.FromArgb(r, g, b);
                        i += 3;
                    }
                    state.IsExtendedBackground = false;
                    continue;
                }

                // Process the main code
                switch (code)
                {
                    case 0:
                        // Reset all to default
                        ResetState(state);
                        break;
                    case 1:
                        // Bold on
                        state.IsBold = true;
                        break;
                    case 3:
                        // Italic on
                        state.IsItalic = true;
                        break;
                    case 4:
                        // Underline on
                        state.IsUnderline = true;
                        break;
                    case 22:
                        // Normal intensity (bold off, faint off)
                        state.IsBold = false;
                        break;
                    case 23:
                        // Italic off
                        state.IsItalic = false;
                        break;
                    case 24:
                        // Underline off
                        state.IsUnderline = false;
                        break;
                    case 38:
                        // Next codes define extended foreground color
                        state.IsExtendedForeground = true;
                        break;
                    case 48:
                        // Next codes define extended background color
                        state.IsExtendedBackground = true;
                        break;
                    case 39:
                        // Default foreground
                        state.Foreground = Color.White;
                        break;
                    case 49:
                        // Default background
                        state.Background = Color.Black;
                        break;
                    default:
                        // Standard or bright foreground/background
                        if (code >= 30 && code <= 37)
                        {
                            // Standard FG
                            state.Foreground = StandardColors[code - 30];
                        }
                        else if (code >= 40 && code <= 47)
                        {
                            // Standard BG
                            state.Background = StandardColors[code - 40];
                        }
                        else if (code >= 90 && code <= 97)
                        {
                            // Bright FG
                            // e.g. 90 => bright black, 91 => bright red, etc.
                            if (BrightColors.ContainsKey(code))
                                state.Foreground = BrightColors[code];
                        }
                        else if (code >= 100 && code <= 107)
                        {
                            // Bright BG
                            // e.g. 100 => bright black bg, 101 => bright red bg, etc.
                            int baseCode = code - 10; // so 100 -> 90, 101 -> 91, etc.
                            if (BrightColors.ContainsKey(baseCode))
                                state.Background = BrightColors[baseCode];
                        }
                        // Other codes like blink(5) or reverse(7) are unhandled here
                        // but won't break anything—just ignored.
                        break;
                }
            }
        }
        private static void ResetState(ColorState state)
        {
            state.Foreground = Color.White;
            state.Background = Color.Black;
            state.IsBold = false;
            state.IsItalic = false;
            state.IsUnderline = false;
            state.IsExtendedForeground = false;
            state.IsExtendedBackground = false;
        }
        private static Color GetBrightColor(Color color)
        {
            // Simple approach: 20% boost on each channel, capped at 255
            int r = Math.Min(255, (int)(color.R * 1.2));
            int g = Math.Min(255, (int)(color.G * 1.2));
            int b = Math.Min(255, (int)(color.B * 1.2));
            return Color.FromArgb(r, g, b);
        }
        public static Color Convert256ColorCode(int code)
        {
            if (code < 0 || code > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(code), "Color code must be between 0 and 255.");
            }

            // 0-7 => standardColors
            // 8-15 => brightColors
            if (code < 8)
            {
                return StandardColors[code];
            }
            else if (code < 16)
            {
                // 8-15 => bright
                return BrightColors[code + 90 - 8]; // 8 -> 90, 9 -> 91, etc.
            }
            else if (code < 16 + 6 * 6 * 6)
            {
                // 16-231 => 6x6x6 color cube
                int index = code - 16;
                int r = (index / 36) % 6;
                int g = (index / 6) % 6;
                int b = index % 6;
                // Each component is 0..5, map that to 0..255 in steps of 51
                return Color.FromArgb(r * 51, g * 51, b * 51);
            }
            else
            {
                // 232-255 => grayscale from black to white in 24 steps
                int level = (code - 232) * 10 + 8;
                if (level < 0) level = 0;
                if (level > 255) level = 255;
                return Color.FromArgb(level, level, level);
            }
        }
    }

}
