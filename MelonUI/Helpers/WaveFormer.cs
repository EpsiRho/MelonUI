using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Helpers
{
    public static class WaveFormer
    {
        public static float[] GenerateWaveform(string path, int width = 0)
        {
            if (width == 0 || !File.Exists(path))
            {
                return null;
            }

            using (var reader = new AudioFileReader(path))
            {
                int channels = reader.WaveFormat.Channels;

                // Total number of samples to process (all channels)
                long totalSamples = reader.Length / (reader.WaveFormat.BlockAlign / channels);

                var waveform = new float[width];
                var sumSquares = new double[width];
                var sampleCounts = new long[width];

                long currentSampleIndex = 0;
                double maxAmplitude = 0;

                // Precompute the scaling factor
                double scaleFactor = (double)width / totalSamples;

                // Read larger chunks of data
                int bufferSize = 1024 * 1024; // Adjust as needed
                var buffer = new float[bufferSize];
                int samplesRead;

                while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < samplesRead; i++)
                    {
                        int pixelIndex = (int)(currentSampleIndex * scaleFactor);
                        if (pixelIndex >= width)
                        {
                            pixelIndex = width - 1;
                        }

                        // Sum squares across all channels
                        sumSquares[pixelIndex] += buffer[i] * buffer[i];
                        sampleCounts[pixelIndex]++;

                        currentSampleIndex++;
                    }
                }

                // Compute RMS and find max amplitude
                for (int i = 0; i < width; i++)
                {
                    if (sampleCounts[i] > 0)
                    {
                        double rms = Math.Sqrt(sumSquares[i] / sampleCounts[i]);
                        waveform[i] = (float)rms;
                        if (rms > maxAmplitude)
                        {
                            maxAmplitude = rms;
                        }
                    }
                    else
                    {
                        waveform[i] = 0;
                    }
                }

                // Normalize the waveform to 0-1
                if (maxAmplitude > 0)
                {
                    for (int i = 0; i < waveform.Length; i++)
                    {
                        waveform[i] /= (float)maxAmplitude;
                    }
                }

                return waveform;
            }
        }

        public static float[] GenerateWaveformSegment(string path, int width, TimeSpan startTime, TimeSpan duration)
        {
            if (width == 0 || !File.Exists(path))
            {
                return null;
            }

            using (var reader = new AudioFileReader(path))
            {
                int channels = reader.WaveFormat.Channels;
                var sampleRate = reader.WaveFormat.SampleRate;
                var bytesPerSample = reader.WaveFormat.BitsPerSample / 8;

                // Seek to the start time
                reader.CurrentTime = startTime;

                // Calculate the total number of samples to read
                long totalSamples = (long)(duration.TotalSeconds * sampleRate) * channels;

                var waveform = new float[width];
                long samplesPerPixel = totalSamples / width;

                var buffer = new float[samplesPerPixel * channels];
                int waveformIndex = 0;
                float maxAmplitude = 0;

                while (waveformIndex < width)
                {
                    int samplesRequired = buffer.Length;
                    int samplesRead = reader.Read(buffer, 0, samplesRequired);

                    if (samplesRead == 0)
                        break;

                    float max = 0;
                    for (int i = 0; i < samplesRead; i += channels)
                    {
                        float amplitude = Math.Abs(buffer[i]);
                        if (amplitude > max)
                            max = amplitude;
                    }

                    waveform[waveformIndex++] = max;
                    if (max > maxAmplitude)
                        maxAmplitude = max;
                }

                // Normalize the waveform to 0-1
                if (maxAmplitude > 0)
                {
                    for (int i = 0; i < waveform.Length; i++)
                    {
                        waveform[i] /= maxAmplitude;
                    }
                }

                return waveform;
            }
        }
        public static float[] GenerateWholeWaveform(string path, int samplesPerSecond = 100)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using (var reader = new AudioFileReader(path))
            {
                int channels = reader.WaveFormat.Channels;
                int sampleRate = reader.WaveFormat.SampleRate;
                int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                int blockAlign = reader.WaveFormat.BlockAlign;

                double totalDurationSeconds = reader.TotalTime.TotalSeconds;
                int totalDataPoints = (int)(totalDurationSeconds * samplesPerSecond);

                var waveform = new float[totalDataPoints];
                int waveformIndex = 0;
                float maxAmplitude = 0;

                int samplesPerDataPoint = sampleRate / samplesPerSecond * channels;
                var buffer = new float[samplesPerDataPoint];

                while (waveformIndex < totalDataPoints)
                {
                    int samplesRead = reader.Read(buffer, 0, buffer.Length);
                    if (samplesRead == 0)
                        break;

                    float max = 0;
                    for (int i = 0; i < samplesRead; i += channels)
                    {
                        float amplitude = Math.Abs(buffer[i]);
                        if (amplitude > max)
                            max = amplitude;
                    }

                    waveform[waveformIndex++] = max;
                    if (max > maxAmplitude)
                        maxAmplitude = max;
                }

                // Normalize the waveform to 0-1
                if (maxAmplitude > 0)
                {
                    for (int i = 0; i < waveform.Length; i++)
                    {
                        waveform[i] /= maxAmplitude;
                    }
                }

                return waveform;
            }
        }
    }
}
