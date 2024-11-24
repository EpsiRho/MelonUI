using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class ProgressBar : UIElement
    {
        private float _progress = 0f;
        public float Progress
        {
            get => _progress;
            set => _progress = Math.Clamp(value, 0f, 1f);
        }

        public float? PreviousProgress { get; private set; }
        public DateTime LastUpdate { get; private set; }

        // Customization options
        public string Text { get; set; } = "";
        public bool ShowPercentage { get; set; } = true;
        public bool AnimateProgress { get; set; } = true;
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(200);
        public ProgressBarStyle Style { get; set; } = ProgressBarStyle.Solid;
        public ConsoleColor ProgressColor { get; set; } = ConsoleColor.Green;
        public ConsoleColor EmptyColor { get; set; } = ConsoleColor.DarkGray;

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

        public ProgressBar()
        {
            ShowBorder = true;
            LastUpdate = DateTime.Now;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var background = IsFocused ? FocusedBackground : Background;
            int contentWidth = ActualWidth - 2;  // Account for border
            int contentHeight = ActualHeight - 2;

            // Calculate the animated progress
            float currentProgress = Progress;
            if (AnimateProgress && PreviousProgress.HasValue)
            {
                var timeSinceUpdate = (DateTime.Now - LastUpdate).TotalMilliseconds;
                var animationProgress = Math.Min(1.0, timeSinceUpdate / AnimationDuration.TotalMilliseconds);

                if (animationProgress < 1.0)
                {
                    currentProgress = (float)(PreviousProgress.Value + (Progress - PreviousProgress.Value) * animationProgress);
                }
            }

            // Draw the text/percentage above the progress bar if we have enough height
            if (contentHeight >= 2)
            {
                string displayText = Text;
                if (ShowPercentage)
                {
                    string percentage = $"{currentProgress * 100:F0}%";
                    if (!string.IsNullOrEmpty(displayText))
                        displayText += " - ";
                    displayText += percentage;
                }

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
            if (Progress != PreviousProgress)
            {
                PreviousProgress = Progress;
                LastUpdate = DateTime.Now;
            }
        }

        public override void HandleKey(ConsoleKeyInfo keyInfo)
        {
            if (!IsFocused) return;

            // Allow arrow keys to adjust progress when focused
            switch (keyInfo.Key)
            {
                case ConsoleKey.LeftArrow:
                    Progress = Math.Max(0f, Progress - 0.01f);
                    break;
                case ConsoleKey.RightArrow:
                    Progress = Math.Min(1f, Progress + 0.01f);
                    break;
            }
        }
    }
}
