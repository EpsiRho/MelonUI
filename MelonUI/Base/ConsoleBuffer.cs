using Pastel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wcwidth;

namespace MelonUI.Base
{
    public class ConsoleBuffer
    {
        public ConsolePixel[,] Buffer;
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
            var emptyPixel = new ConsolePixel(' ', Color.Transparent, Color.Transparent, false);
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
            var width = UnicodeCalculator.GetWidth(c);
            return width;
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
            int startX = Math.Max(0, x);
            int startY = Math.Max(0, y);
            int endX = Math.Min(Width, x + source.Width);
            int endY = Math.Min(Height, y + source.Height);

            if (startX >= endX || startY >= endY) return;

            if (respectBackground) // Direct copy without background checks
            {
                for (int ty = startY, sy = startY - y; ty < endY; ty++, sy++)
                {
                    for (int tx = startX, sx = startX - x; tx < endX; tx++, sx++)
                    {
                        Buffer[ty, tx] = source.Buffer[sy, sx];
                    }
                }
            }
            else // Copy only if source pixel background is not fully transparent
            {
                for (int ty = startY, sy = startY - y; ty < endY; ty++, sy++)
                {
                    for (int tx = startX, sx = startX - x; tx < endX; tx++, sx++)
                    {
                        var sourcePixel = source.Buffer[sy, sx];
                        if (sourcePixel.Background.A != 0 || sourcePixel.Foreground.A != 0)
                        {
                            Buffer[ty, tx] = sourcePixel;
                        }
                    }
                }
            }
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
            if (Width <= 0 || Height <= 0) return;

            try
            {
                StringBuilder screenBuilder = new StringBuilder();

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
                        char ch = pixel.Character;

                        string charStr = ch.ToString();

                        if (pixel.Foreground.A == 0 && pixel.Background.A == 0)
                        {
                            lineBuilder.Append(charStr);
                        }
                        else
                        {
                            string coloredChar = charStr;
                            if (pixel.Foreground.A != 0)
                                coloredChar = coloredChar.Pastel(pixel.Foreground);

                            if (pixel.Background.A != 0)
                                coloredChar = coloredChar.PastelBg(pixel.Background);

                            lineBuilder.Append(coloredChar);
                        }

                        if (pixel.IsWide)
                        {
                            skipNext = true;
                        }
                    }

                    screenBuilder.AppendLine(lineBuilder.ToString());
                }

                output.Write(screenBuilder.ToString());
                output.Flush();

                Console.SetCursorPosition(0, 0);
            }
            catch (Exception)
            {
                output.Flush();
            }
        }

        public string Screenshot(bool KeepColor)
        {
            if (Width <= 0 || Height <= 0) return "";

            try
            {
                StringBuilder screenBuilder = new StringBuilder();

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
                        char ch = pixel.Character;

                        string charStr = ch.ToString();

                        if (pixel.Foreground.A == 0 && pixel.Background.A == 0)
                        {
                            lineBuilder.Append(charStr);
                        }
                        else if (KeepColor)
                        {
                            string coloredChar = charStr;
                            if (pixel.Foreground.A != 0)
                                coloredChar = coloredChar.Pastel(pixel.Foreground);

                            if (pixel.Background.A != 0)
                                coloredChar = coloredChar.PastelBg(pixel.Background);

                            lineBuilder.Append(coloredChar);
                        }
                        else
                        {
                            lineBuilder.Append(charStr);
                        }

                        if (pixel.IsWide)
                        {
                            skipNext = true;
                        }
                    }

                    screenBuilder.AppendLine(lineBuilder.ToString());
                }

                return screenBuilder.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
