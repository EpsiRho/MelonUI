using Pastel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public class ConsoleBuffer
    {
        private ConsolePixel[,] Buffer;
        public int Width { get; private set; }
        public int Height { get; private set; }

        private readonly char[] RenderBuffer;
        private readonly int MaxBufferSize = 65535;

        public ConsoleBuffer(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Buffer = new ConsolePixel[Height, Width];
            RenderBuffer = new char[MaxBufferSize];
            Clear(Color.FromArgb(0,0,0,0));
        }

        public void Resize(int newWidth, int newHeight)
        {
            newWidth = Math.Max(1, newWidth);
            newHeight = Math.Max(1, newHeight);

            var newBuffer = new ConsolePixel[newHeight, newWidth];
            int copyWidth = Math.Min(Width, newWidth);
            int copyHeight = Math.Min(Height, newHeight);

            for (int y = 0; y < copyHeight; y++)
                for (int x = 0; x < copyWidth; x++)
                {
                    newBuffer[y, x] = Buffer[y, x];
                }

            Buffer = newBuffer;
            Width = newWidth;
            Height = newHeight;
        }

        public void Clear(Color background)
        {
            var emptyPixel = new ConsolePixel(' ', Color.White, background, false);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    Buffer[y, x] = emptyPixel;
                }
        }

        public void Write(int x, int y, ConsoleBuffer source)
        {
            for (int sy = 0; sy < source.Height; sy++)
            {
                for (int sx = 0; sx < source.Width; sx++)
                {
                    int targetX = x + sx;
                    int targetY = y + sy;
                    if (targetX >= 0 && targetX < Width && targetY >= 0 && targetY < Height)
                    {
                        Buffer[targetY, targetX] = source.Buffer[sy, sx];
                    }
                }
            }
        }

        public void SetPixel(int x, int y, char c, Color foreground, Color background)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                int charWidth = GetCharWidth(c);
                if (charWidth == 2 && x + 1 < Width)
                {
                    // Set the wide character
                    Buffer[y, x] = new ConsolePixel(c, foreground, background, true);
                    // Set an empty character next to it that will be skipped
                    Buffer[y, x + 1] = new ConsolePixel(' ', foreground, background, false);
                }
                else
                {
                    Buffer[y, x] = new ConsolePixel(c, foreground, background, false);
                }
            }
        }

        public static int GetCharWidth(char c)
        {
            // East Asian Width property categories that are considered "wide"
            // Reference: Unicode Standard Annex #11: East Asian Width
            // https://www.unicode.org/reports/tr11/

            // Full and Wide ranges (F, W)
            if ((c >= '\u1100' && c <= '\u115F') ||   // Hangul Jamo
                (c >= '\u231A' && c <= '\u231B') ||   // Watch, Hourglass
                (c >= '\u2329' && c <= '\u232A') ||   // Angular Brackets
                (c >= '\u23E9' && c <= '\u23EC') ||   // Play/Pause Buttons
                (c >= '\u23F0' && c <= '\u23F3') ||   // Clock Face
                (c >= '\u25FD' && c <= '\u25FE') ||   // Geometric Shapes
                (c >= '\u2614' && c <= '\u2615') ||   // Umbrella, Hot Beverage
                (c >= '\u2648' && c <= '\u2653') ||   // Zodiac Symbols
                (c >= '\u267F' && c <= '\u2685') ||   // Wheelchair Symbol + Die Faces
                (c >= '\u2693' && c <= '\u2694') ||   // Anchor, Crossed Swords
                (c >= '\u26C4' && c <= '\u26C5') ||   // Snowman
                (c >= '\u26CE' && c <= '\u26CF') ||   // Ophiuchus
                (c >= '\u26D4' && c <= '\u26E1') ||   // Traffic Symbols
                (c >= '\u26E3' && c <= '\u26E3') ||   // Heavy Circle
                (c >= '\u26E8' && c <= '\u26E9') ||   // Black Cross
                (c >= '\u26EB' && c <= '\u26F1') ||   // Castle
                (c >= '\u26F4' && c <= '\u26F4') ||   // Ferry
                (c >= '\u26F6' && c <= '\u26F9') ||   // Park Symbols
                (c >= '\u26FB' && c <= '\u26FC') ||   // Japanese Bank Symbol
                (c >= '\u26FE' && c <= '\u26FF') ||   // Cup
                (c >= '\u2700' && c <= '\u2767') ||   // Dingbats
                (c >= '\u2794' && c <= '\u27BF') ||   // Arrows and Dingbats
                (c >= '\u2800' && c <= '\u28FF') ||   // Braille Patterns
                (c >= '\u2B00' && c <= '\u2B2F') ||   // Arrows
                (c >= '\u2B45' && c <= '\u2B46') ||   // Arrows
                (c >= '\u2B4D' && c <= '\u2B4F') ||   // Arrows
                (c >= '\u2E80' && c <= '\u2E99') ||   // CJK Radicals Supplement
                (c >= '\u2E9B' && c <= '\u2EF3') ||   // CJK Radicals
                (c >= '\u2F00' && c <= '\u2FD5') ||   // Kangxi Radicals
                (c >= '\u2FF0' && c <= '\u2FFB') ||   // Ideographic Description Characters
                (c >= '\u3000' && c <= '\u303E') ||   // CJK Symbols and Punctuation
                (c >= '\u3041' && c <= '\u3096') ||   // Hiragana
                (c >= '\u3099' && c <= '\u30FF') ||   // Kana
                (c >= '\u3105' && c <= '\u312F') ||   // Bopomofo
                (c >= '\u3131' && c <= '\u318E') ||   // Hangul Compatibility Jamo
                (c >= '\u3190' && c <= '\u31E3') ||   // Kanbun
                (c >= '\u31F0' && c <= '\u321E') ||   // Katakana Phonetic Extensions
                (c >= '\u3220' && c <= '\u3247') ||   // Enclosed CJK
                (c >= '\u3250' && c <= '\u4DBF') ||   // CJK Unified Ideographs Extension A
                (c >= '\u4E00' && c <= '\u9FFF') ||   // CJK Unified Ideographs
                (c >= '\uA000' && c <= '\uA48C') ||   // Yi Syllables
                (c >= '\uA490' && c <= '\uA4C6') ||   // Yi Radicals
                (c >= '\uA960' && c <= '\uA97C') ||   // Hangul Jamo Extended-A
                (c >= '\uAC00' && c <= '\uD7A3') ||   // Hangul Syllables
                (c >= '\uF900' && c <= '\uFAFF') ||   // CJK Compatibility Ideographs
                (c >= '\uFE10' && c <= '\uFE19') ||   // Vertical Forms
                (c >= '\uFE30' && c <= '\uFE52') ||   // CJK Compatibility Forms
                (c >= '\uFE54' && c <= '\uFE66') ||   // Small Forms
                (c >= '\uFE68' && c <= '\uFE6B') ||   // Small Forms
                (c >= '\uFF01' && c <= '\uFF60') ||   // Fullwidth Forms
                (c >= '\uFFE0' && c <= '\uFFE6') ||   // Fullwidth Signs

                // Special symbols that are typically rendered wide
                (c == '→' || c == '←' || c == '↑' || c == '↓' ||
                 c == '▲' || c == '▼' || c == '◄' || c == '►' ||
                 c == '◆' || c == '◇' || c == '○' || c == '●' || c == '◐' ||
                 c == '◑' || c == '◒' || c == '◓' || c == '◔' || c == '◕'))
            {
                return 2;
            }

            // Handle surrogate pairs (for emoji and other characters outside the BMP)
            if (char.IsSurrogate(c))
            {
                return 2;
            }

            // Default to single width
            return 1;
        }

        public static int GetStringWidth(string str)
        {
            int width = 0;
            foreach (char c in str)
            {
                width += GetCharWidth(c);
            }
            return width;
        }

        public void WriteString(int x, int y, string text, Color foreground, Color background)
        {
            if (string.IsNullOrEmpty(text)) return;

            int currentX = x;
            foreach (char c in text)
            {
                if (currentX >= Width) break;

                int charWidth = GetCharWidth(c);
                if (currentX + charWidth <= Width)
                {
                    SetPixel(currentX, y, c, foreground, background);
                    currentX += charWidth;
                }
                else
                {
                    break; // Stop if we can't fit the next character
                }
            }
        }

        public void WriteStringWrapped(int x, int y, string text, int maxWidth, Color foreground, Color background)
        {
            if (string.IsNullOrEmpty(text)) return;
            maxWidth = Math.Min(maxWidth, Width - x);
            if (maxWidth <= 0) return;

            string[] words = text.Split(' ');
            int currentX = x;
            int currentY = y;

            foreach (string word in words)
            {
                int wordWidth = GetStringWidth(word);

                // Check if we need to wrap
                if (currentX + wordWidth > x + maxWidth)
                {
                    currentX = x;
                    currentY++;
                    if (currentY >= Height) break;
                }

                // Write the word
                WriteString(currentX, currentY, word, foreground, background);
                currentX += wordWidth + 1; // +1 for space
            }
        }

        public void WriteStringCentered(int y, string text, Color foreground, Color background)
        {
            if (string.IsNullOrEmpty(text)) return;
            int textWidth = GetStringWidth(text);
            int x = Math.Max(0, (Width - textWidth) / 2);
            WriteString(x, y, text, foreground, background);
        }

        public void WriteLines(int x, int y, IEnumerable<string> lines, Color foreground, Color background)
        {
            int currentY = y;
            foreach (string line in lines)
            {
                if (currentY >= Height) break;
                WriteString(x, currentY, line, foreground, background);
                currentY++;
            }
        }

        public void WriteLinesCentered(int startY, IEnumerable<string> lines, Color foreground, Color background)
        {
            int currentY = startY;
            foreach (string line in lines)
            {
                if (currentY >= Height) break;
                WriteStringCentered(currentY, line, foreground, background);
                currentY++;
            }
        }

        public void WriteBuffer(int x, int y, ConsoleBuffer source, bool respectBackground = true)
        {
            for (int sy = 0; sy < source.Height; sy++)
            {
                for (int sx = 0; sx < source.Width; sx++)
                {
                    int targetX = x + sx;
                    int targetY = y + sy;

                    if (targetX >= 0 && targetX < Width && targetY >= 0 && targetY < Height)
                    {
                        var sourcePixel = source.Buffer[sy, sx];

                        // If respectBackground is false, only copy if the source pixel isn't the default background
                        if (respectBackground || sourcePixel.Background != Color.FromArgb(0,0,0,0))
                        {
                            Buffer[targetY, targetX] = sourcePixel;
                        }
                    }
                }
            }
        }

        public void WriteFrame(int x, int y, string text, Color foreground, Color background, bool border = true)
        {
            if (border)
            {
                // Draw border
                SetPixel(x, y, '┌', foreground, background);
                SetPixel(x + text.Length + 1, y, '┐', foreground, background);
                SetPixel(x, y + 2, '└', foreground, background);
                SetPixel(x + text.Length + 1, y + 2, '┘', foreground, background);

                for (int i = 1; i <= text.Length; i++)
                {
                    SetPixel(x + i, y, '─', foreground, background);
                    SetPixel(x + i, y + 2, '─', foreground, background);
                }

                SetPixel(x, y + 1, '│', foreground, background);
                SetPixel(x + text.Length + 1, y + 1, '│', foreground, background);
            }

            // Draw text
            WriteString(x + 1, y + 1, text, foreground, background);
        }

        public void FillRect(int x, int y, int width, int height, char c, Color foreground, Color background)
        {
            for (int cy = y; cy < y + height && cy < Height; cy++)
            {
                for (int cx = x; cx < x + width && cx < Width; cx++)
                {
                    if (cx >= 0 && cy >= 0)
                    {
                        SetPixel(cx, cy, c, foreground, background);
                    }
                }
            }
        }

        public void DrawRect(int x, int y, int width, int height, Color foreground, Color background)
        {
            // Draw corners
            SetPixel(x, y, '┌', foreground, background);
            SetPixel(x + width - 1, y, '┐', foreground, background);
            SetPixel(x, y + height - 1, '└', foreground, background);
            SetPixel(x + width - 1, y + height - 1, '┘', foreground, background);

            // Draw top and bottom edges
            for (int i = 1; i < width - 1; i++)
            {
                SetPixel(x + i, y, '─', foreground, background);
                SetPixel(x + i, y + height - 1, '─', foreground, background);
            }

            // Draw left and right edges
            for (int i = 1; i < height - 1; i++)
            {
                SetPixel(x, y + i, '│', foreground, background);
                SetPixel(x + width - 1, y + i, '│', foreground, background);
            }
        }

        public ConsoleBuffer CreateSubBuffer(int x, int y, int width, int height)
        {
            var sub = new ConsoleBuffer(width, height);
            for (int sy = 0; sy < height; sy++)
            {
                for (int sx = 0; sx < width; sx++)
                {
                    int sourceX = x + sx;
                    int sourceY = y + sy;
                    if (sourceX >= 0 && sourceX < Width && sourceY >= 0 && sourceY < Height)
                    {
                        sub.Buffer[sy, sx] = Buffer[sourceY, sourceX];
                    }
                }
            }
            return sub;
        }

        public void RenderToConsole(StreamWriter output)
        {
            try
            {
                if (Width <= 0 || Height <= 0) return;

                for (int y = 0; y < Height - 1; y++)
                {
                    StringBuilder lineBuilder = new StringBuilder();
                    bool skipNext = false;

                    for (int x = 0; x < Width; x++)
                    {
                        if (skipNext)
                        {
                            skipNext = false;
                            continue;
                        }

                        var pixel = Buffer[y, x];
                        if(pixel.Foreground.A == 0 && pixel.Background.A == 0)
                        {
                            lineBuilder.Append($"{pixel.Character}");
                        }
                        else if(pixel.Foreground.A == 0 && pixel.Background.A != 0)
                        {
                            lineBuilder.Append($"{pixel.Character}".PastelBg(pixel.Background));
                        }
                        else if(pixel.Foreground.A != 0 && pixel.Background.A == 0)
                        {
                            lineBuilder.Append($"{pixel.Character}".Pastel(pixel.Foreground));
                        }
                        else if(pixel.Foreground.A != 0 && pixel.Background.A != 0)
                        {
                            lineBuilder.Append($"{pixel.Character}".Pastel(pixel.Foreground).PastelBg(pixel.Background));   
                        }

                        if (pixel.IsWide)
                        {
                            skipNext = true;
                        }
                    }

                    output.WriteLine(lineBuilder.ToString());
                    output.Flush();
                }
                Console.SetCursorPosition(0, 0);
            }
            catch (Exception ex)
            {
                output.Flush();
            }
        }
    }
}
