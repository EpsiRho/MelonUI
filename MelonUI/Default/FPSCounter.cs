using MelonUI.Base;
using MelonUI.Managers;
using System;
using System.Diagnostics;
using System.Drawing;

namespace MelonUI.Default
{
    public class FPSCounter : UIElement
    {
        private int _frameCount;
        private int _fps;
        private Stopwatch _stopwatch;

        public FPSCounter()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            MinWidth = "5";
            MinHeight = "4";
        }

        public void OnFrameRendered(object sender, EventArgs e)
        {
            _frameCount++;

            if (_stopwatch.ElapsedMilliseconds >= 1000)
            {
                _fps = _frameCount;
                _frameCount = 0;
                _stopwatch.Restart();
            }

            // Mark for re-render
            NeedsRecalculation = true;
        }

        /// <summary>
        /// Returns the last calculated FPS.
        /// </summary>
        public int GetFPS() => _fps;

        /// <summary>
        /// Renders the FPS display text inside the UIElement.
        /// </summary>
        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var displayText = $"FPS: {_fps}";
            var fg = IsFocused ? FocusedForeground : Foreground;
            var bg = IsFocused ? FocusedBackground : Background;

            // Allow for border spacing if enabled
            int offsetX = ShowBorder ? 1 : 0;
            int offsetY = ShowBorder ? 1 : 0;

            // Truncate if necessary to fit width
            //if (displayText.Length > ActualWidth - (ShowBorder ? 2 : 0))
            //{
            //    displayText = displayText.Substring(0, Math.Max(0, ActualWidth - (ShowBorder ? 2 : 0)));
            //}

            buffer.WriteStringCentered(offsetY, "FPS", fg, bg);
            buffer.WriteStringCentered(offsetY + 1, _fps.ToString("000"), fg, bg);
        }
    }
}
