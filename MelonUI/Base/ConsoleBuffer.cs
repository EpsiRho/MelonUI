
using Pastel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Wcwidth;
using System.Globalization;

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
        public Win32Renderer? WRenderer { get; private set; }
        public UnixRenderer? URenderer { get; private set; }

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
            var emptyPixel = new ConsolePixel(' ', Color.Transparent, background, false);
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

        public void SetPixel(int x, int y, char c, Color foreground, Color background, bool forceIsWide = false)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                int charWidth = forceIsWide ? 2 : GetCharWidth(c);
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
            // Best-effort for single char inputs. Handle surrogate halves as width1 fallback.
            if (char.IsHighSurrogate(c) || char.IsLowSurrogate(c))
                return 1;
            var r = new Rune(c);
            int w = UnicodeCalculator.GetWidth(r);
            return w < 0 ? 1 : w;
        }

        public static int GetTextElementWidth(string element)
        {
            if (string.IsNullOrEmpty(element)) return 0;
            var span = element.AsSpan();
            var status = Rune.DecodeFromUtf16(span, out var first, out int consumed);
            if (status == System.Buffers.OperationStatus.Done && consumed > 0)
            {
                int w = UnicodeCalculator.GetWidth(first);
                return w < 0 ? 0 : w;
            }
            return 0;
        }

        public static int GetStringWidth(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            int width = 0;
            var indices = StringInfo.ParseCombiningCharacters(str);
            for (int i = 0; i < indices.Length; i++)
            {
                int start = indices[i];
                int length = (i + 1 < indices.Length) ? indices[i + 1] - start : str.Length - start;
                string element = str.Substring(start, length);
                width += GetTextElementWidth(element);
            }
            return width;
        }

        public void WriteString(int x, int y, string text, Color foreground, Color background)
        {
            if (string.IsNullOrEmpty(text) || y < 0 || y >= Height) return;

            int currentX = x;
            var indices = StringInfo.ParseCombiningCharacters(text);
            for (int i = 0; i < indices.Length; i++)
            {
                int start = indices[i];
                int length = (i + 1 < indices.Length) ? indices[i + 1] - start : text.Length - start;
                string element = text.Substring(start, length);

                int charWidth = GetTextElementWidth(element);
                if (currentX >= Width) break;
                if (currentX < 0) { currentX += charWidth; continue; }

                if (charWidth == 2 && currentX + 1 < Width)
                {
                    // If the grapheme is a surrogate pair (two UTF-16 code units), store both units so WriteConsoleW will output a valid pair
                    if (element.Length >= 2 && char.IsSurrogatePair(element[0], element[1]))
                    {
                        char high = element[0];
                        char low = element[1];
                        // write high surrogate as wide
                        this[y, currentX] = new ConsolePixel(high, foreground, background, true);
                        // write low surrogate in the next cell so UTF-16 pair is complete when writing
                        if (currentX + 1 < Width)
                        {
                            this[y, currentX + 1] = new ConsolePixel(low, foreground, background, false);
                        }
                        currentX += 2;
                    }
                    else
                    {
                        // store first code unit (ConsolePixel stores single char) but mark as wide
                        char c = element[0];
                        SetPixel(currentX, y, c, foreground, background, forceIsWide: true);
                        currentX += 2;
                    }
                }
                else if (charWidth == 1)
                {
                    char c = element[0];
                    SetPixel(currentX, y, c, foreground, background);
                    currentX += 1;
                }
                else
                {
                    // zero-width combining mark: attach visually to previous cell; do not advance
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
        public void WriteBuffer(int x, int y, ConsoleBuffer source, bool respectBackground = true)
        {
            ArgumentNullException.ThrowIfNull(source);
            int startX = (int)Math.Max(0L, x), startY = (int)Math.Max(0L, y);
            int endX = (int)Math.Min(Width, (long)x + source.Width), endY = (int)Math.Min(Height, (long)y + source.Height);
            if (startX >= endX || startY >= endY) return;
            for (int row = startY; row < endY; row++)
                for (int column = startX; column < endX; column++)
                {
                    var pixel = source[row - y, column - x];
                    // OpenGL readback uses the raw packed channels rather than the text/color layout.
                    if (pixel.Character == '\uffff') { this[row, column] = pixel; continue; }
                    if (!respectBackground && pixel.Background.A == 0 && pixel.Foreground.A == 0) continue;
                    var underneath = this[row, column];
                    if (pixel.Background.A == 0) pixel.BackgroundARGB = underneath.BackgroundARGB;
                    else if (pixel.Background.A < 255) pixel.Background = Blend(pixel.Background, underneath.Background);
                    if (pixel.Foreground.A is > 0 and < 255) pixel.Foreground = Blend(pixel.Foreground, pixel.Background);
                    this[row, column] = pixel;
                }
        }

        private static Color Blend(Color foreground, Color background)
        {
            int alpha = foreground.A;
            return Color.FromArgb(255,
                (foreground.R * alpha + background.R * (255 - alpha)) / 255,
                (foreground.G * alpha + background.G * (255 - alpha)) / 255,
                (foreground.B * alpha + background.B * (255 - alpha)) / 255);
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

                for (int y = 0; y < Height; y++)
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
                        string charStr = char.IsHighSurrogate(ch) && x + 1 < Width && char.IsLowSurrogate(this[y, x + 1].Character)
                            ? new string([ch, this[y, x + 1].Character]) : char.IsSurrogate(ch) ? "\uFFFD" : ch.ToString();

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

                    screenBuilder.Append(lineBuilder);
                    if (y < Height - 1) screenBuilder.AppendLine();
                }

                output.Write(screenBuilder.ToString());
                output.Flush();

                Console.SetCursorPosition(0, 0);
            }
            catch (Exception)
            {
                output.Flush();
                throw;
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

                for (int y = 0; y < Height; y++)
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
                        string charStr = char.IsHighSurrogate(ch) && x + 1 < Width && char.IsLowSurrogate(this[y, x + 1].Character)
                            ? new string([ch, this[y, x + 1].Character]) : char.IsSurrogate(ch) ? "\uFFFD" : ch.ToString();

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
                throw;
            }
        }
    }
}
