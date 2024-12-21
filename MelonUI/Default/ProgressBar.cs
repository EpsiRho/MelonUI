using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class ProgressBar : UIElement
    {
        private object _progress = 0f;
        public float Progress
        {
            get => (float)GetBoundValue(nameof(Progress), _progress);
            set => SetBoundValue(nameof(Progress), Math.Clamp(value, 0f, 1f), ref _progress);
        }
        public int LoadingPlace = 0;
        public float? PreviousProgress { get; private set; }
        public DateTime LastUpdate { get; private set; }
        private bool flip = false;

        // Customization options
        private object _Text = "";
        public string Text
        {
            get => (string)GetBoundValue(nameof(Text), _Text);
            set => SetBoundValue(nameof(Text), value, ref _Text);
        }
        private object _ShowPercentage = false;
        public bool ShowPercentage
        {
            get => (bool)GetBoundValue(nameof(ShowPercentage), _ShowPercentage);
            set => SetBoundValue(nameof(ShowPercentage), value, ref _ShowPercentage);
        }
        private object _AnimateProgress = false;
        public bool AnimateProgress
        {
            get => (bool)GetBoundValue(nameof(AnimateProgress), _AnimateProgress);
            set => SetBoundValue(nameof(AnimateProgress), value, ref _AnimateProgress);
        }
        private object _Style = ProgressBarStyle.Solid;
        public ProgressBarStyle Style
        {
            get => (ProgressBarStyle)GetBoundValue(nameof(Style), _Style);
            set => SetBoundValue(nameof(Style), value, ref _Style);
        }
        private object _ProgressColor = Color.Green;
        public Color ProgressColor
        {
            get => (Color)GetBoundValue(nameof(ProgressColor), _ProgressColor);
            set => SetBoundValue(nameof(ProgressColor), value, ref _ProgressColor);
        }
        private object _EmptyColor = Color.DarkGray;
        public Color EmptyColor
        {
            get => (Color)GetBoundValue(nameof(EmptyColor), _EmptyColor);
            set => SetBoundValue(nameof(EmptyColor), value, ref _EmptyColor);
        }

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
            int contentWidth = ActualWidth - 1;
            int contentHeight = ShowPercentage ? ActualHeight - 2 : ActualHeight - 1;

            var currentProgress = Progress;

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
            int barY = contentHeight >= 2 && ShowPercentage ? 2 : 1;
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
            int loadingWidth = 2;
            int start = LoadingPlace - loadingWidth;
            int end = LoadingPlace + loadingWidth;
            for (int x = 0; x < progressWidth; x++)
            {
                int drawX = Style == ProgressBarStyle.Ascii ? x + 2 : x + 1;

                if (Style == ProgressBarStyle.Loading)
                {
                    // Create a sliding animation effect
                    bool isInLoadingSegment = x <= end && x >= start;
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

            if(Style == ProgressBarStyle.Loading)
            {
                ShowPercentage = false;
                Progress += 0.01f;
                if(Progress >= 0.04f)
                {
                    Progress = 0;
                    LoadingPlace = flip ? LoadingPlace - 1 : LoadingPlace + 1;
                }
                if (LoadingPlace >= progressWidth)
                {
                    flip = true;
                }
                if (LoadingPlace <= 0)
                {
                    flip = false;
                }
            }
        }


    }
}
