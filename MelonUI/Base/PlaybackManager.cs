using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public abstract class PlaybackManager : IDisposable
    {
        private bool _isDisposed { get; set; }
        public virtual string FilePath { get; set; }
        public TimeSpan Duration 
        {
            get
            {
                return GetDuration();
            }
        }
        public abstract void InitializeAudio();

        public abstract bool GetPlayState();
        public abstract void Play();

        public abstract void Pause();
        public abstract void Seek(float seconds);
        public abstract TimeSpan GetPosition();
        public abstract TimeSpan GetDuration();

        public void SeekRelative(float offsetSeconds)
        {
            if (_isDisposed) return;
            var currentSeconds = GetPosition().TotalSeconds;
            Seek((float)(currentSeconds + offsetSeconds));
        }

        public abstract void SetVolume(float volume);
        public abstract float GetVolume();

        public abstract void Dispose();

    }
}
