using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Managers
{
    public class AudioPlaybackManager : IDisposable
    {
        [DllImport("winmm.dll")]
        private static extern uint timeGetTime();

        [DllImport("winmm.dll")]
        private static extern int timeBeginPeriod(int period);

        [DllImport("winmm.dll")]
        private static extern int timeEndPeriod(int period);

        public WaveStream _reader;
        public WaveOutEvent _outputDevice;
        private readonly string _filePath;
        private uint _startTime;
        private long _startPosition;
        private bool _isPlaying;
        private readonly object _seekLock = new object();
        private DateTime _lastSeekTime = DateTime.MinValue;
        private const int MIN_SEEK_INTERVAL_MS = 50; // Increased minimum time between seeks
        private bool _isDisposed;
        private ISampleProvider _sampleProvider;

        public AudioPlaybackManager(string filePath)
        {
            _filePath = filePath;
            timeBeginPeriod(1);
            InitializeAudio();
        }

        private WaveStream CreateReader()
        {
            string extension = Path.GetExtension(_filePath).ToLowerInvariant();

            try
            {
                if (extension == ".wav")
                {
                    return new WaveFileReader(_filePath);
                }
                else
                {
                    // For FLAC and other formats, wrap MediaFoundationReader in a buffered wave provider
                    var mediaReader = new MediaFoundationReader(_filePath);
                    var buffered = new BufferedWaveProvider(mediaReader.WaveFormat);
                    var waveProvider = new WaveChannel32(mediaReader);
                    return waveProvider;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CreateReader error: {ex.Message}");
                throw;
            }
        }

        private void InitializeAudio()
        {
            lock (_seekLock)
            {
                try
                {
                    var oldReader = _reader;
                    var oldOutput = _outputDevice;

                    _reader = CreateReader();
                    _sampleProvider = _reader.ToSampleProvider();

                    _outputDevice = new WaveOutEvent
                    {
                        DesiredLatency = 100,
                        NumberOfBuffers = 3
                    };
                    _outputDevice.Init(_sampleProvider);

                    _startPosition = 0;
                    _startTime = timeGetTime();

                    if (oldOutput != null)
                    {
                        oldOutput.Stop();
                        oldOutput.Dispose();
                    }
                    if (oldReader != null)
                    {
                        oldReader.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InitializeAudio error: {ex.Message}");
                    throw;
                }
            }
        }

        public void Play()
        {
            lock (_seekLock)
            {
                if (_isDisposed) return;

                if (!_isPlaying)
                {
                    try
                    {
                        _startTime = timeGetTime();
                        _startPosition = _reader.Position;
                        _outputDevice.Play();
                        _isPlaying = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Play error: {ex.Message}");
                        RecoverFromError();
                    }
                }
            }
        }

        public void Pause()
        {
            lock (_seekLock)
            {
                if (_isDisposed) return;

                if (_isPlaying)
                {
                    try
                    {
                        _outputDevice.Pause();
                        _startPosition = _reader.Position;
                        _isPlaying = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Pause error: {ex.Message}");
                        RecoverFromError();
                    }
                }
            }
        }

        private void RecoverFromError()
        {
            try
            {
                InitializeAudio();
                if (_startPosition > 0)
                {
                    _reader.Position = _startPosition;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RecoverFromError failed: {ex.Message}");
            }
        }

        public void Seek(float seconds)
        {
            if (_isDisposed) return;

            // Throttle rapid seeks
            var now = DateTime.Now;
            var timeSinceLastSeek = (now - _lastSeekTime).TotalMilliseconds;
            if (timeSinceLastSeek < MIN_SEEK_INTERVAL_MS)
            {
                Thread.Sleep((int)(MIN_SEEK_INTERVAL_MS - timeSinceLastSeek));
            }

            lock (_seekLock)
            {
                try
                {
                    bool wasPlaying = _isPlaying;
                    if (wasPlaying)
                    {
                        _outputDevice.Stop();
                        _isPlaying = false;
                    }

                    // Completely reinitialize audio to avoid COM issues
                    InitializeAudio();

                    seconds = Math.Max(0, Math.Min(seconds, (float)_reader.TotalTime.TotalSeconds));
                    long targetPosition = (long)(seconds * _reader.WaveFormat.AverageBytesPerSecond);
                    targetPosition = targetPosition - (targetPosition % _reader.WaveFormat.BlockAlign);

                    _reader.Position = targetPosition;
                    _startPosition = targetPosition;
                    _startTime = timeGetTime();

                    if (wasPlaying)
                    {
                        _outputDevice.Play();
                        _isPlaying = true;
                    }

                    _lastSeekTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Seek error: {ex.Message}");
                    RecoverFromError();
                }
            }
        }

        public TimeSpan GetPosition()
        {
            lock (_seekLock)
            {
                if (_isDisposed) return TimeSpan.Zero;

                try
                {
                    long position;
                    if (_isPlaying)
                    {
                        uint currentTime = timeGetTime();
                        uint elapsedMs = currentTime - _startTime;
                        long elapsedBytes = (long)((elapsedMs / 1000.0) * _reader.WaveFormat.AverageBytesPerSecond);
                        position = _startPosition + elapsedBytes;
                    }
                    else
                    {
                        position = _startPosition;
                    }

                    double seconds = (double)position / _reader.WaveFormat.AverageBytesPerSecond;
                    return TimeSpan.FromSeconds(seconds);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GetPosition error: {ex.Message}");
                    return TimeSpan.FromSeconds((double)_startPosition / _reader.WaveFormat.AverageBytesPerSecond);
                }
            }
        }

        public void SeekRelative(float offsetSeconds)
        {
            if (_isDisposed) return;
            var currentSeconds = GetPosition().TotalSeconds;
            Seek((float)(currentSeconds + offsetSeconds));
        }

        public void SetVolume(float volume)
        {
            if (_isDisposed) return;
            try
            {
                _outputDevice.Volume = Math.Clamp(volume, 0f, 1f);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetVolume error: {ex.Message}");
            }
        }

        public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_seekLock)
            {
                _isDisposed = true;
                timeEndPeriod(1);

                try
                {
                    if (_outputDevice != null)
                    {
                        _outputDevice.Stop();
                        _outputDevice.Dispose();
                        _outputDevice = null;
                    }
                    if (_reader != null)
                    {
                        _reader.Dispose();
                        _reader = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Dispose error: {ex.Message}");
                }
            }
        }
    }
}
