using MelonUI.Attributes;
using MelonUI.Base;
using MelonUI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public partial class Waveform : UIElement
    {
        [Binding]
        private float scaleAmmount = 5.0f;
        [Binding]
        private int samplesPerSecond = 60;
        [Binding]
        private float windowsize = 0.7f;
        [Binding]
        private PlaybackManager pbManager;
        [Binding]
        private float[] waveformData;
        public Waveform(PlaybackManager manager)
        {
            pbManager = manager;
            // Generate the entire waveform once
            WaveformData = WaveFormer.GenerateWholeWaveform(pbManager.FilePath, samplesPerSecond);
            EnableCaching = false;
            NeedsRecalculation = true;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var width = ActualWidth;
            var height = ActualHeight;
            if (ShowBorder)
            {
                width -= 2;
                height -= 1;
            }
            var waveHeight = (int)((height));
            var lyricsStart = waveHeight + 1;
            float scaleFactor = height / 2.0f;
            int centerRow = (int)(height / 2);

            // Set up NAudio playback
            TimeSpan duration = pbManager.Duration;
            try
            {
                TimeSpan windowDuration = TimeSpan.FromSeconds(windowsize);
                var frameStartTime = DateTime.Now;

                // Draw waveform
                // Get current playback position
                TimeSpan currentTime = pbManager.GetPosition();
                if (currentTime > pbManager.Duration)
                {
                    pbManager.Pause();
                }

                // Calculate start and end times for the waveform segment
                double windowDurationSeconds = windowDuration.TotalSeconds;
                double halfWindowSeconds = windowDurationSeconds / 2.0;

                double startTimeSeconds = currentTime.TotalSeconds - halfWindowSeconds;
                if (startTimeSeconds < 0)
                {
                    startTimeSeconds = 0;
                }

                double endTimeSeconds = startTimeSeconds + windowDurationSeconds;
                if (endTimeSeconds > duration.TotalSeconds)
                {
                    endTimeSeconds = duration.TotalSeconds;
                    startTimeSeconds = endTimeSeconds - windowDurationSeconds;
                    if (startTimeSeconds < 0)
                    {
                        startTimeSeconds = 0;
                    }
                }

                // Convert times to indices
                int startIndex = (int)(startTimeSeconds * samplesPerSecond);
                int endIndex = (int)(endTimeSeconds * samplesPerSecond);

                // Extract the waveform segment
                int segmentLength = endIndex - startIndex;
                float[] waveformSegment = new float[width];
                if (segmentLength > 0)
                {
                    float samplesPerPixel = (float)segmentLength / width;
                    for (int i = 0; i < width; i++)
                    {
                        int index = startIndex + (int)(i * samplesPerPixel);
                        if (index >= WaveformData.Length)
                            index = WaveformData.Length - 1;

                        waveformSegment[i] = WaveformData[index];
                    }
                }
                else
                {
                    // Handle case where segmentLength <= 0
                    Array.Clear(waveformSegment, 0, waveformSegment.Length);
                }

                // Scale the waveform
                float[] scaled = new float[waveformSegment.Length];
                for (int i = 0; i < waveformSegment.Length; i++)
                {
                    scaled[i] = waveformSegment[i] * scaleFactor;
                }

                // Build the frame in the buffer
                int bump = ShowBorder ? 1 : 0;
                for (int p = 0 + bump; p < width; p++)
                {
                    int h = (int)scaled[p];
                    float t1 = h * 0.75f;
                    float t2 = h * 0.55f;
                    float t3 = h * 0.25f;
                    bool quiet = true;

                    for (int i = 0; i < h; i++)
                    {
                        char block = '█';
                        if (i > t1)
                        {
                            block = '░';
                        }
                        else if (i > t2)
                        {
                            block = '▒';
                        }
                        else if (i > t3)
                        {
                            block = '▓';
                        }

                        int rowUp = centerRow - i;
                        int rowDown = centerRow + i;

                        if (rowUp >= 0)
                        {
                            buffer.SetPixel(p, rowUp, block, Foreground, Background);
                        }

                        if (rowDown < height)
                        {
                            buffer.SetPixel(p, rowDown, block, Foreground, Background);
                        }
                        quiet = false;
                    }

                    if (quiet)
                    {
                        buffer.SetPixel(p, centerRow, '░', Foreground, Background);
                    }
                }
            }
            catch (Exception)
            {
                scaleAmmount++;
            }
        }
    }
}