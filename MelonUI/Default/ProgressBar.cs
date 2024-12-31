using MelonUI.Attributes;
using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public partial class ProgressBar : UIElement
    {
        [Binding]
        private float progressValue = 0f;
        [Binding]
        private string text = "";

        public float? PreviousProgress { get; private set; }
        public int LoadingPlace = 0;
        public DateTime LastUpdate { get; private set; }
        private bool flip = false;

        // Customization options
        [Binding]
        private bool showPercentage = false;
        [Binding]
        private bool animateProgress = false;
        [Binding]
        private ProgressBarStyle style = ProgressBarStyle.Solid;
        [Binding]
        private Color progressColor = Color.Green;
        [Binding]
        private Color emptyColor = Color.DarkGray;

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

            var currentProgress = ProgressValue;

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
            if (ProgressValue != PreviousProgress)
            {
                PreviousProgress = ProgressValue;
                LastUpdate = DateTime.Now;
            }

            if(Style == ProgressBarStyle.Loading)
            {
                ShowPercentage = false;
                ProgressValue += 0.01f;
                if(ProgressValue >= 0.04f)
                {
                    ProgressValue = 0;
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
