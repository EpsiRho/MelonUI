using ATL;
using Pastel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Wcwidth;

namespace MelonUI.Base
{
    public class ConsoleBuffer
    {
        /// <summary>
        /// Internally store pixels in a 1D array.
        /// </summary>
        public ConsolePixel[] buffer;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public Win32Renderer WRenderer { get; private set; }
        public UnixRenderer URenderer { get; private set; }

        /// <summary>
        /// Indexer so we can write: this[y, x] = ...
        /// Instead of buffer[y*Width + x], we hide the 1D indexing here.
        /// </summary>
        public ConsolePixel this[int row, int col]
        {
            get => buffer[row * Width + col];
            set => buffer[row * Width + col] = value;
        }

        /// <summary>
        /// note from hypervis0r: fuck your indexing epsi
        /// </summary>
        public ConsolePixel this[int index]
        {
            get => buffer[index];
            set => buffer[index] = value;
        }

        public ConsoleBuffer(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            // Allocate 1D array
            buffer = new ConsolePixel[Width * Height];
            //Clear(Color.FromArgb(0, 0, 0, 0));
        }

        public void Resize(int newWidth, int newHeight)
        {
            newWidth = Math.Max(1, newWidth);
            newHeight = Math.Max(1, newHeight);

            // Allocate new 1D buffer
            var newBuffer = new ConsolePixel[newWidth * newHeight];
            int copyWidth = Math.Min(Width, newWidth);
            int copyHeight = Math.Min(Height, newHeight);

            // Copy existing pixels into new buffer
            //for (int y = 0; y < copyHeight; y++)
            //{
            //    for (int x = 0; x < copyWidth; x++)
            //    {
            //        newBuffer[y * newWidth + x] = this[y, x];
            //    }
            //}

            // Replace the old buffer
            buffer = newBuffer;
            Width = newWidth;
            Height = newHeight;
        }

        public void Clear(Color background)
        {
            var emptyPixel = new ConsolePixel(' ', Color.Transparent, Color.Transparent, false);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    this[y, x] = emptyPixel;
                }
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
                        this[targetY, targetX] = source[sy, sx];
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
                    this[y, x] = new ConsolePixel(c, foreground, background, true);
                    // Set an empty character next to it that will be skipped
                    this[y, x + 1] = new ConsolePixel(' ', foreground, background, false);
                }
                else
                {
                    this[y, x] = new ConsolePixel(c, foreground, background, false);
                }
            }
        }

        public static int GetCharWidth(char c)
        {
            var wideTable = WideTable.GetTable(Unicode.Version_15_1_0);
            return wideTable.Exist((uint)c) ? 2 : 1;
            //return UnicodeCalculator.GetWidth(c);
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

        public byte[] StructToBytes(ConsolePixel[] structure)
        {
            int size = Marshal.SizeOf(typeof(ConsolePixel)) * structure.Length;
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            for (int i = 0; i < structure.Length; i++)
            {
                try
                {
                    Marshal.StructureToPtr(structure[i], ptr, true);
                    Marshal.Copy(ptr, arr, 0, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            return arr;
        }
        public unsafe void WriteBuffer(int x, int y, ConsoleBuffer source, bool respectBackground = true)
        {
            int startX = Math.Max(0, x);
            int startY = Math.Max(0, y);
            int endX = Math.Min(Width, x + source.Width);
            int endY = Math.Min(Height, y + source.Height);
            if (startX >= endX || startY >= endY) return;
            // Get direct access to the underlying arrays
            fixed (ConsolePixel* destPtr = &buffer[0])
            fixed (ConsolePixel* sourcePtr = &source.buffer[0])
            {
                int width = endX - startX;
                int sourceWidth = source.Width;
                int destWidth = Width;
                if (respectBackground)
                {
                    // Direct memory copy for entire rows when possible
                    if (startX == 0 && width == sourceWidth)
                    {
                        int bytesToCopy = width * sizeof(ConsolePixel);
                        for (int ty = startY, sy = startY - y; ty < endY; ty++, sy++)
                        {
                            Buffer.MemoryCopy(
                                sourcePtr + (sy * sourceWidth),
                                destPtr + (ty * destWidth),
                                bytesToCopy,
                                bytesToCopy);
                        }
                    }
                    else
                    {
                        // Process multiple pixels at once using pointer arithmetic
                        for (int ty = startY, sy = startY - y; ty < endY; ty++, sy++)
                        {
                            ConsolePixel* srcRow = sourcePtr + (sy * sourceWidth) + (startX - x);
                            ConsolePixel* destRow = destPtr + (ty * destWidth) + startX;
                            // Unroll the loop for better performance
                            int i = 0;
                            int widthMinus4 = width - 4;
                            for (; i < widthMinus4; i += 4)
                            {
                                destRow[i] = srcRow[i];
                                destRow[i + 1] = srcRow[i + 1];
                                destRow[i + 2] = srcRow[i + 2];
                                destRow[i + 3] = srcRow[i + 3];
                            }
                            // Handle remaining pixels
                            for (; i < width; i++)
                            {
                                destRow[i] = srcRow[i];
                            }
                        }
                    }
                }
                else
                {
                    // Optimize the transparency check path
                    for (int ty = startY, sy = startY - y; ty < endY; ty++, sy++)
                    {
                        ConsolePixel* srcRow = sourcePtr + (sy * sourceWidth) + (startX - x);
                        ConsolePixel* destRow = destPtr + (ty * destWidth) + startX;
                        int i = 0;
                        int widthMinus4 = width - 4;
                        for (; i < width; i++)
                        {
                            var sourcePixel = srcRow[i];
                            if (sourcePixel.Background.A != 0 || sourcePixel.Foreground.A != 0)
                            {
                                destRow[i] = sourcePixel;
                            }
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
                        sub[sy, sx] = this[sourceY, sourceX];
                    }
                }
            }
            return sub;
        }

        /// <summary>
        /// Renders to console using either the Win32Renderer or a fallback method.
        /// </summary>
        public void RenderToConsole(StreamWriter output, bool UsePlatformSpecificRenderer)
        {
            // Windows Win32 API Support
            if (UsePlatformSpecificRenderer && Win32Renderer.IsSupported)
            {
                if (WRenderer == null)
                {
                    WRenderer = new Win32Renderer(Width, Height);
                }
                else if (WRenderer.Width != Width || WRenderer.Height != Height)
                {
                    WRenderer.SetSize(Width, Height);
                }

                WRenderer.RenderToConsole(buffer);
                return;
            }
            else if (UsePlatformSpecificRenderer && UnixRenderer.IsSupported)
            {
                if (URenderer == null)
                {
                    URenderer = new UnixRenderer(Width, Height);
                }
                else if (URenderer.Width != Width || URenderer.Height != Height)
                {
                    URenderer.SetSize(Width, Height);
                }

                URenderer.RenderToConsole(buffer);
                return;
            }

            // If not using Win32Renderer, do the standard System.Console rendering
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

                        var pixel = this[y, x];
                        char ch = pixel.Character == '\0' ? ' ' : pixel.Character;
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

        /// <summary>
        /// Renders the buffer to a string (screenshot) with optional color.
        /// </summary>
        public string Screenshot(bool keepColor)
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

                        var pixel = this[y, x];
                        char ch = pixel.Character;
                        string charStr = ch.ToString();

                        if (pixel.Foreground.A == 0 && pixel.Background.A == 0)
                        {
                            lineBuilder.Append(charStr);
                        }
                        else if (keepColor)
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
