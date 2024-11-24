using System;
using System.Collections.Generic;
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
            Clear();
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
                    newBuffer[y, x] = Buffer[y, x];

            Buffer = newBuffer;
            Width = newWidth;
            Height = newHeight;
        }

        public void Clear(ConsoleColor background = ConsoleColor.Black)
        {
            var emptyPixel = new ConsolePixel(' ', ConsoleColor.White, background);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Buffer[y, x] = emptyPixel;
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

        public void SetPixel(int x, int y, char c, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                Buffer[y, x] = new ConsolePixel(c, foreground, background);
            }
        }

        // Write a string horizontally
        public void WriteString(int x, int y, string text, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            if (string.IsNullOrEmpty(text)) return;

            for (int i = 0; i < text.Length; i++)
            {
                if (x + i >= Width) break;
                SetPixel(x + i, y, text[i], foreground, background);
            }
        }

        // Write a string horizontally with word wrap
        public void WriteStringWrapped(int x, int y, string text, int maxWidth, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            if (string.IsNullOrEmpty(text)) return;
            maxWidth = Math.Min(maxWidth, Width - x);
            if (maxWidth <= 0) return;

            string[] words = text.Split(' ');
            int currentX = x;
            int currentY = y;

            foreach (string word in words)
            {
                // Check if we need to wrap
                if (currentX + word.Length > x + maxWidth)
                {
                    currentX = x;
                    currentY++;
                    if (currentY >= Height) break;
                }

                // Write the word
                WriteString(currentX, currentY, word, foreground, background);
                currentX += word.Length + 1; // +1 for space
            }
        }

        // Write a centered string
        public void WriteStringCentered(int y, string text, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            if (string.IsNullOrEmpty(text)) return;
            int x = Math.Max(0, (Width - text.Length) / 2);
            WriteString(x, y, text, foreground, background);
        }

        // Write multiple lines of text
        public void WriteLines(int x, int y, IEnumerable<string> lines, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            int currentY = y;
            foreach (string line in lines)
            {
                if (currentY >= Height) break;
                WriteString(x, currentY, line, foreground, background);
                currentY++;
            }
        }

        // Write multiple lines centered
        public void WriteLinesCentered(int startY, IEnumerable<string> lines, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            int currentY = startY;
            foreach (string line in lines)
            {
                if (currentY >= Height) break;
                WriteStringCentered(currentY, line, foreground, background);
                currentY++;
            }
        }

        // Composite another buffer with transparency
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
                        if (respectBackground || sourcePixel.Background != ConsoleColor.Black)
                        {
                            Buffer[targetY, targetX] = sourcePixel;
                        }
                    }
                }
            }
        }

        // Write a framed string (with optional border)
        public void WriteFrame(int x, int y, string text, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black, bool border = true)
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

        // Fill a rectangle with a character
        public void FillRect(int x, int y, int width, int height, char c, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
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

        // Draw a rectangle border
        public void DrawRect(int x, int y, int width, int height, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
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

        // Create a sub-buffer from a region of this buffer
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

        public void RenderToConsole()
        {
            try
            {
                int consoleWidth = Console.WindowWidth;
                int consoleHeight = Console.WindowHeight;

                if (Width != consoleWidth || Height != consoleHeight)
                {
                    Resize(consoleWidth, consoleHeight);
                }

                Console.SetCursorPosition(0, 0);
                ConsoleColor currentFg = ConsoleColor.White;
                ConsoleColor currentBg = ConsoleColor.Black;
                int bufferIndex = 0;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var pixel = Buffer[y, x];

                        if (pixel.Foreground != currentFg || pixel.Background != currentBg)
                        {
                            if (bufferIndex > 0)
                            {
                                Console.Write(RenderBuffer, 0, bufferIndex);
                                bufferIndex = 0;
                            }

                            currentFg = pixel.Foreground;
                            currentBg = pixel.Background;
                            Console.ForegroundColor = currentFg;
                            Console.BackgroundColor = currentBg;
                        }

                        RenderBuffer[bufferIndex++] = pixel.Character;

                        if (bufferIndex >= MaxBufferSize || x == Width - 1)
                        {
                            Console.Write(RenderBuffer, 0, bufferIndex);
                            bufferIndex = 0;
                        }
                    }
                }

                if (bufferIndex > 0)
                {
                    Console.Write(RenderBuffer, 0, bufferIndex);
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
            }
            catch (Exception)
            {
                // Handle console resize or other console-related exceptions
            }
        }
    }
}
