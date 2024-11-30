using MelonUI.Base;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class ConsoleImage : UIElement
    {
        public string _path;
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                if (value != _path)
                {
                    _path = value;
                    ReadImageToPixelBuffer();
                }
            }
        }
        public Image<Rgba32> image { get; set; }
        public ConsolePixel[,] CurrentBuffer { get; set; }
        public static string AsciiTable = " .'^\":;li!I-~+?][}{1)(\\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";
        private int lastWidth { get; set; }
        private int lastHeight { get; set; }
        public bool UseBg { get; set; } = true;
        public ConsoleImage(string path, string width, string height)
        {
            Path = path;
            Width = width;
            Height = height;
            ReadImageToPixelBuffer();
        }

        public void ReadImageToPixelBuffer()
        {
            if(ActualHeight == 0 || ActualWidth == 0)
            {
                return;
            }
            CurrentBuffer = new ConsolePixel[ActualWidth, ActualHeight];
            image = Image.Load<Rgba32>(Path);

            image.Mutate(x => x
                 .Resize(ActualWidth, ActualHeight));

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        Rgba32 pixel = pixelRow[x];
                        double brightness = CalculateBrightnessPercentage(pixel.R, pixel.G, pixel.B, pixel.A);
                        char c = GetPixelChar(brightness);
                        CurrentBuffer[x, y] = new ConsolePixel(c, System.Drawing.Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B), System.Drawing.Color.FromArgb(pixel.A - 125 > 0 ? pixel.A - 125 : 1, pixel.R, pixel.G, pixel.B));
                    }
                }
            });
        }
        public static double CalculateBrightnessPercentage(byte red, byte green, byte blue, byte alpha)
        {
            // Calculate perceived brightness
            double brightness = 0.299 * red + 0.587 * green + 0.114 * blue;

            // Adjust brightness by alpha (optional, normalize alpha to [0, 1])
            double normalizedAlpha = alpha / 255.0;
            brightness *= normalizedAlpha;

            // Normalize brightness to the range [0, 1]
            return brightness / 255.0;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (CurrentBuffer == null || CurrentBuffer.Length == 0)
            {
                ReadImageToPixelBuffer();
                NeedsRecalculation = true;
                return;
            }

            if (lastHeight != ActualHeight || lastWidth != ActualWidth)
            {
                ReadImageToPixelBuffer();
                NeedsRecalculation = true;
                lastWidth = ActualWidth;
                lastHeight = ActualHeight;
            }

            int xMax = CurrentBuffer.GetLength(0);
            int yMax = CurrentBuffer.GetLength(1);
            int xStart = 0;
            int yStart = 0;

            if (ShowBorder)
            {
                xStart = 1;
                yStart = 1;
            }

            for (int y = 0 + xStart; y < yMax - 1; y++)
            {
                for (int x = 0 + yStart; x < xMax - 1; x++)
                {
                    if (UseBg)
                    {
                        buffer.SetPixel(x, y, CurrentBuffer[x, y].Character, CurrentBuffer[x, y].Foreground, CurrentBuffer[x, y].Background);
                    }
                    else
                    {
                        buffer.SetPixel(x, y, CurrentBuffer[x, y].Character, CurrentBuffer[x, y].Foreground, Background);
                    }
                }
            }
        }
        public char GetPixelChar(double percentage)
        {
            int index = (int)Math.Floor(AsciiTable.Length * percentage);

            if (index >= AsciiTable.Length)
            {
                index = AsciiTable.Length - 1;
            }

            char character = AsciiTable[index];
            return character;
        }
    }
}
