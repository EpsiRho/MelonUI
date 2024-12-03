using MelonUI.Base;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class ConsoleImage : UIElement
    {
        private string _path;
        public string Path
        {
            get => _path;
            set
            {
                if (value != _path)
                {
                    _path = value;
                    _ = InitializeImageAsync();
                }
            }
        }

        private Image<Rgba32> image;
        private Dictionary<int, ConsolePixel[,]> frameCache;
        private List<int> frameDelays;
        private readonly Stopwatch timeKeeper;
        private int currentFrameIndex;
        private int totalFrames;
        private bool isGif;
        private bool renderLock;
        private int lastWidth;
        private int lastHeight;
        private long lastFrameTime;
        public int MAX_CACHED_FRAMES = 30;
        public int FRAME_SKIP_THRESHOLD_MS = 100;
        private readonly object cacheLock = new object();
        private Task initializationTask;
        private byte[] imageBuffer;
        private string loadingMessage = "Loading File";

        public static readonly string AsciiTable = " .'^\":;li!I-~+?][}{1)(\\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";
        public bool UseBg { get; set; } = true;
        public bool UseColor { get; set; } = true;

        public ConsoleImage(string path, string width, string height)
        {
            Width = width;
            Height = height;
            frameCache = new Dictionary<int, ConsolePixel[,]>();
            frameDelays = new List<int>();
            timeKeeper = new Stopwatch();
            Path = path;
        }

        public async Task InitializeImageAsync()
        {
            renderLock = true;
            loadingMessage = "Reading File...";

            try
            {
                if (ActualHeight == 0 || ActualWidth == 0)
                    return;

                // Load file into memory buffer asynchronously
                imageBuffer = await File.ReadAllBytesAsync(Path);
                loadingMessage = "Processing Image...";

                // Process image in background
                initializationTask = Task.Run(() =>
                {
                    image?.Dispose();
                    image = Image.Load<Rgba32>(imageBuffer);

                    if (!UseColor)
                        image.Mutate(x => x.Grayscale());

                    image.Mutate(x => x.Resize(ActualWidth, ActualHeight));

                    isGif = image.Frames.Count > 1;
                    totalFrames = image.Frames.Count;
                    frameDelays.Clear();
                    frameCache.Clear();
                    currentFrameIndex = 0;
                    lastFrameTime = 0;

                    if (isGif)
                    {
                        foreach (var frame in image.Frames)
                        {
                            if (frame.Metadata.TryGetGifMetadata(out var gifMeta))
                                frameDelays.Add(gifMeta.FrameDelay * 10);
                        }
                    }

                    loadingMessage = "Preloading Frame 1...";
                    LoadFrame(0);
                    timeKeeper.Restart();

                    // Clear the buffer after processing
                    imageBuffer = null;
                });

                await initializationTask;
            }
            catch (Exception ex)
            {
                loadingMessage = $"Error: {ex.Message}";
            }
            finally
            {
                renderLock = false;
            }
        }

        private void LoadFrame(int frameIndex)
        {
            lock (cacheLock)
            {
                if (frameCache.ContainsKey(frameIndex))
                    return;

                var frameBuffer = new ConsolePixel[ActualWidth, ActualHeight];
                var frame = image.Frames[frameIndex];

                frame.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            Rgba32 pixel = pixelRow[x];
                            double brightness = CalculateBrightnessPercentage(pixel.R, pixel.G, pixel.B, pixel.A);
                            char c = GetPixelChar(brightness);
                            frameBuffer[x, y] = new ConsolePixel(c,
                                System.Drawing.Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B),
                                System.Drawing.Color.FromArgb(pixel.A - 125 > 0 ? pixel.A - 125 : 1, pixel.R, pixel.G, pixel.B),
                                false);
                        }
                    }
                });

                frameCache[frameIndex] = frameBuffer;
                CleanupOldFrames();
            }
        }

        private void CleanupOldFrames()
        {
            if (frameCache.Count <= MAX_CACHED_FRAMES)
                return;

            var keysToRemove = new List<int>();
            int currentFrame = currentFrameIndex;

            foreach (var key in frameCache.Keys)
            {
                int distance = Math.Min(
                    Math.Abs(key - currentFrame),
                    Math.Abs(key - currentFrame + totalFrames)
                );

                if (distance > MAX_CACHED_FRAMES / 2)
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                frameCache.Remove(key);
            }
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (renderLock)
            {
                buffer.WriteStringCentered(2, loadingMessage, Foreground, Background);
                return;
            }

            if (image == null || lastHeight != ActualHeight || lastWidth != ActualWidth)
            {
                _ = InitializeImageAsync();
                NeedsRecalculation = true;
                lastWidth = ActualWidth;
                lastHeight = ActualHeight;
                return;
            }

            if (isGif)
            {
                UpdateGifFrame();
            }

            RenderFrame(buffer);

            // Preload next frame
            if (isGif)
            {
                int nextFrame = (currentFrameIndex + 1) % totalFrames;
                Task.Run(() => LoadFrame(nextFrame));
            }
        }

        private void UpdateGifFrame()
        {
            long elapsedTime = timeKeeper.ElapsedMilliseconds;

            if (elapsedTime - lastFrameTime >= frameDelays[currentFrameIndex])
            {
                long totalElapsed = elapsedTime - lastFrameTime;
                int framesToSkip = 0;

                if (totalElapsed > FRAME_SKIP_THRESHOLD_MS)
                {
                    framesToSkip = (int)(totalElapsed / frameDelays[currentFrameIndex]);
                }

                currentFrameIndex = (currentFrameIndex + 1 + framesToSkip) % totalFrames;
                lastFrameTime = elapsedTime;
            }
        }

        private void RenderFrame(ConsoleBuffer buffer)
        {
            ConsolePixel[,] currentFrame;
            lock (cacheLock)
            {
                if (!frameCache.TryGetValue(currentFrameIndex, out currentFrame))
                {
                    LoadFrame(currentFrameIndex);
                    if (!frameCache.TryGetValue(currentFrameIndex, out currentFrame))
                        return;
                }
            }

            int xStart = ShowBorder ? 1 : 0;
            int yStart = ShowBorder ? 1 : 0;

            for (int y = yStart; y < ActualHeight - 1; y++)
            {
                for (int x = xStart; x < ActualWidth - 1; x++)
                {
                    var pixel = currentFrame[x, y];
                    buffer.SetPixel(x, y, pixel.Character,
                        pixel.Foreground,
                        UseBg ? pixel.Background : Background);
                }
            }
        }

        private static double CalculateBrightnessPercentage(byte red, byte green, byte blue, byte alpha)
        {
            double brightness = (0.299 * red + 0.587 * green + 0.114 * blue) * (alpha / 255.0);
            return brightness / 255.0;
        }

        private char GetPixelChar(double percentage)
        {
            int index = Math.Min((int)(AsciiTable.Length * percentage), AsciiTable.Length - 1);
            return AsciiTable[index];
        }

        public void Dispose()
        {
            image?.Dispose();
            imageBuffer = null;
        }
    }
}