using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class TimedProgressBar : UIElement
    {
        private TimeSpan _maxTime;
        private Func<TimeSpan> _getCurrentTime;

        public TimeSpan MaxTime
        {
            get => _maxTime;
            set => _maxTime = value;
        }

        public DateTime LastUpdate { get; private set; }

        // Customization options
        public string Text { get; set; } = "";
        public bool ShowPercentage { get; set; } = true;
        public bool AnimateProgress { get; set; } = true;
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(200);
        public ProgressBarStyle Style { get; set; } = ProgressBarStyle.Solid;
        public Color ProgressColor { get; set; } = Color.Green;
        public Color EmptyColor { get; set; } = Color.DarkGray;

        public enum ProgressBarStyle
        {
            Solid,      // █████░░░░░
            Segmented,  // ▰▰▰▰▰▱▱▱▱▱
            Dotted,     // ●●●●●○○○○○
            Blocks,     // ■■■■■□□□□□
            Ascii,      // [=====-    ]
            Loading     // ▐░░█▌░░░░░░  (animated)
        }

        private static readonly Dictionary<ProgressBarStyle, (char full, char empty)> StyleChars = new()
        {
            { ProgressBarStyle.Solid, ('█', '░') },
            { ProgressBarStyle.Segmented, ('▰', '▱') },
            { ProgressBarStyle.Dotted, ('●', '○') },
            { ProgressBarStyle.Blocks, ('■', '□') },
            { ProgressBarStyle.Ascii, ('=', ' ') },
            { ProgressBarStyle.Loading, ('█', '░') }
        };

        public TimedProgressBar(TimeSpan maxTime, Func<TimeSpan> getCurrentTime)
        {
            _maxTime = maxTime;
            _getCurrentTime = getCurrentTime;
            ShowBorder = true;
            LastUpdate = DateTime.Now;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var background = IsFocused ? FocusedBackground : Background;
            int contentWidth = ActualWidth - 1;
            int contentHeight = ActualHeight - 2;

            // Calculate progress based on current time and max time
            TimeSpan currentTime = _getCurrentTime();
            float progress = (float)currentTime.Ticks / _maxTime.Ticks;
            progress = Math.Clamp(progress, 0f, 1f);

            // Calculate the animated progress
            float currentProgress = progress;
            if (AnimateProgress && PreviousProgress.HasValue)
            {
                var timeSinceUpdate = (DateTime.Now - LastUpdate).TotalMilliseconds;
                var animationProgress = Math.Min(1.0, timeSinceUpdate / AnimationDuration.TotalMilliseconds);

                if (animationProgress < 1.0)
                {
                    currentProgress = (float)(PreviousProgress.Value + (progress - PreviousProgress.Value) * animationProgress);
                }
            }

            // Draw the text showing the current time and max time above the progress bar if we have enough height
            if (contentHeight >= 2)
            {
                string displayText = Text;
                string timeDisplay = $"{currentTime:hh\\:mm\\:ss} / {_maxTime:hh\\:mm\\:ss}";
                if (!string.IsNullOrEmpty(displayText))
                    displayText += " - ";
                displayText += timeDisplay;

                if (!string.IsNullOrEmpty(displayText))
                {
                    buffer.WriteStringCentered(1, displayText, Foreground, background);
                }
            }

            // Calculate progress bar dimensions
            int barY = contentHeight >= 2 ? 2 : 1;
            (char full, char empty) = StyleChars[Style];
            int progressWidth = contentWidth - 2; // Leave space for brackets if needed
            int filledWidth = (int)(progressWidth * currentProgress);

            // Special handling for ASCII style
            if (Style == ProgressBarStyle.Ascii)
            {
                buffer.SetPixel(1, barY, '[', Foreground, background);
                buffer.SetPixel(contentWidth, barY, ']', Foreground, background);
            }

            // Draw the progress bar
            for (int x = 0; x < progressWidth; x++)
            {
                int drawX = Style == ProgressBarStyle.Ascii ? x + 2 : x + 1;

                if (Style == ProgressBarStyle.Loading)
                {
                    // Create a sliding animation effect
                    int offset = (int)((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) / 100) % progressWidth;
                    bool isInLoadingSegment = (x + offset) % progressWidth < filledWidth;
                    buffer.SetPixel(drawX, barY,
                        isInLoadingSegment ? full : empty,
                        isInLoadingSegment ? ProgressColor : EmptyColor,
                        background);
                }
                else
                {
                    bool isFilled = x < filledWidth;
                    buffer.SetPixel(drawX, barY,
                        isFilled ? full : empty,
                        isFilled ? ProgressColor : EmptyColor,
                        background);
                }
            }

            // Store the current progress for animation
            if (progress != PreviousProgress)
            {
                PreviousProgress = progress;
                LastUpdate = DateTime.Now;
            }
        }

        public float? PreviousProgress { get; private set; }
    }
}
