using MelonUI.Attributes;
using MelonUI.Base;
using MelonUI.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public partial class Line : UIElement
    {
        [Binding]
        private string x1;
        [Binding]
        private string y1;
        [Binding]
        private string x2;
        [Binding]
        private string y2;
        [Binding]
        private bool antiAliased;
        [Binding]
        private float lineWidth = 1.0f;
        [Binding]
        private float intensity = 1.0f;     // Overall brightness/opacity multiplier (0-1)
        [Binding]
        private float softness = 1.0f;      // Edge softness multiplier (higher = softer edges)
        [Binding]
        private bool roundCaps = true;      // Whether to use rounded end caps
        public int ActualX1;
        public int ActualY1;
        public int ActualX2;
        public int ActualY2;
        public Line()
        {
            Height = "1";
        }
        protected override void RenderContent(ConsoleBuffer buffer)
        {
            bool isHorizontal = ActualY1 == ActualY2;
            bool isVertical = ActualX1 == ActualX2;
            bool isStraight = isHorizontal || isVertical;
            bool isRising = ActualY1 > ActualY2;
            bool isNegative = ActualX1 > ActualX2 || ActualY1 > ActualY2;

            if (isStraight)
            {
                RenderStraightLine(buffer, isHorizontal, isNegative);
            }
            else
            {
                if (AntiAliased)
                {
                    DrawAntialiasedLine(buffer, ActualX1, ActualY1, ActualX2, ActualY2);
                }
                else
                {
                    RenderDiagonalLine(buffer);
                }
            }
        }
        private void RenderStraightLine(ConsoleBuffer buffer, bool isHorizontal, bool isNegative)
        {
            int x = ActualX1;
            int y = ActualY1;
            int x2 = ActualX2;
            int y2 = ActualY2;
            int dx = x2 - x;
            int dy = y2 - y;
            int length = isHorizontal ? Math.Abs(dx) : Math.Abs(dy);
            int step = isNegative ? -1 : 1;

            for (int i = 0; i <= length; i++)
            {
                buffer.SetPixel(x, y, '█', Foreground, Background);
                x += isHorizontal ? step : 0;
                y += isHorizontal ? 0 : step;
            }
        }


        private void RenderDiagonalLine(ConsoleBuffer buffer)
        {
            int x1 = ActualX1;
            int y1 = ActualY1;
            int x2 = ActualX2;
            int y2 = ActualY2;

            // Step A: Ensure left-to-right
            if (x2 < x1)
            {
                // swap x1,x2
                int t = x1; x1 = x2; x2 = t;
                // swap y1,y2
                t = y1; y1 = y2; y2 = t;
            }

            // Step B: Calculate dx, dy
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);

            // Step C: Check if steep
            bool isSteep = (dy > dx);
            if (isSteep)
            {
                // swap x1,y1
                int t = x1; x1 = y1; y1 = t;
                // swap x2,y2
                t = x2; x2 = y2; y2 = t;
                // recalc dx, dy
                dx = Math.Abs(x2 - x1);
                dy = Math.Abs(y2 - y1);

                if (x2 < x1)
                {
                    t = x1; x1 = x2; x2 = t;
                    t = y1; y1 = y2; y2 = t;
                }
            }

            // Step D: figure out the sign for y
            int yStep = (y1 < y2) ? 1 : -1;

            // Step E: init decision param
            int decision = 2 * dy - dx;
            int y = y1;

            // Step F: main loop
            for (int x = x1; x <= x2; x++)
            {
                if (isSteep)
                {
                    buffer.SetPixel(y, x, '█', Foreground, Background);
                }
                else
                {
                    buffer.SetPixel(x, y, '█', Foreground, Background);

                }

                if (decision > 0)
                {
                    y += yStep;
                    decision -= 2 * dx;
                }
                decision += 2 * dy;
            }

            buffer.SetPixel(ActualX1, ActualY1, '█', Color.Green, Background);
            buffer.SetPixel(ActualX2, ActualY2, '█', Color.Red, Background);

            buffer.WriteString(1, 1, $"dx/y: ({dx},{dy})", Foreground, Background);
            buffer.WriteString(1, 2, $"Step: ({yStep})", Foreground, Background);
            buffer.WriteString(1, 3, $"Steep: ({isSteep})", Foreground, Background);
            
        }


        public void DrawAntialiasedLine(ConsoleBuffer buffer, int x0, int y0, int x1, int y1)
        {
            // Clamp parameters to valid ranges
            Intensity = Math.Max(0.0f, Math.Min(1.0f, Intensity));
            Softness = Math.Max(0.1f, Softness);
            LineWidth = Math.Max(1.0f, LineWidth);

            // Helper function to plot a pixel with intensity
            void Plot(int x, int y, float intensity)
            {
                if (x < 0 || x >= buffer.Width || y < 0 || y >= buffer.Height)
                    return;

                // Clamp intensity to valid range
                intensity = Math.Max(0.0f, Math.Min(1.0f, intensity));

                // Apply intensity and softness adjustments
                float adjustedIntensity = intensity * Intensity;
                if (Softness != 1.0f)
                {
                    adjustedIntensity = (float)Math.Pow(adjustedIntensity, 1.0f / Softness);
                }

                // Create a color with the appropriate alpha for blending
                int alpha = (int)(adjustedIntensity * 255);
                Color pixelColor = Color.FromArgb(
                    alpha,
                    Math.Max(0,  alpha),
                    Math.Max(0,  alpha),
                    Math.Max(0,  alpha)
                );

                buffer.SetPixel(x, y, '█', pixelColor, pixelColor);
            }

            // Handle wide lines
            if (LineWidth > 1.0f)
            {
                DrawWideAntialiasedLine(buffer, x0, y0, x1, y1, Plot);
            }
            else
            {
                DrawSingleLine(buffer, x0, y0, x1, y1, Plot);
            }
        }

        private void DrawWideAntialiasedLine(ConsoleBuffer buffer, int x0, int y0, int x1, int y1, Action<int, int, float> plot)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);

            // Handle degenerate case
            if (length < 0.001f)
            {
                if (RoundCaps)
                {
                    DrawEndCap(buffer, x0, y0, LineWidth, plot);
                }
                return;
            }

            // Calculate normal vector
            float nx = -dy / length;
            float ny = dx / length;

            // Calculate offset points for the wide line
            float halfWidth = LineWidth / 2.0f;

            // Draw the main line body using a series of thin lines
            int steps = Math.Max(2, (int)(LineWidth * 2));
            float stepSize = LineWidth / steps;

            // Function to determine if we're at an edge
            bool IsEdgeLine(float offset)
            {
                return Math.Abs(Math.Abs(offset) - halfWidth) < stepSize;
            }

            for (float offset = -halfWidth; offset <= halfWidth; offset += stepSize)
            {
                int newX0 = x0 + (int)(nx * offset);
                int newY0 = y0 + (int)(ny * offset);
                int newX1 = x1 + (int)(nx * offset);
                int newY1 = y1 + (int)(ny * offset);

                if (IsEdgeLine(offset))
                {
                    // Only edge lines get anti-aliasing from Wu's algorithm
                    DrawSingleLine(buffer, newX0, newY0, newX1, newY1, plot);
                }
                else
                {
                    // Interior lines are drawn with full intensity
                    void SolidPlot(int x, int y, float intensity)
                    {
                        plot(x, y, 1.0f);
                    }
                    DrawSingleLine(buffer, newX0, newY0, newX1, newY1, SolidPlot);
                }
            }

            // Draw end caps if enabled
            if (RoundCaps)
            {
                DrawEndCap(buffer, x0, y0, LineWidth, plot);
                DrawEndCap(buffer, x1, y1, LineWidth, plot);
            }
        }
        private void DrawEndCap(ConsoleBuffer buffer, int centerX, int centerY, float width, Action<int, int, float> plot)
        {
            float radius = width / 2.0f;
            int radiusCeil = (int)Math.Ceiling(radius);

            for (int y = -radiusCeil; y <= radiusCeil; y++)
            {
                for (int x = -radiusCeil; x <= radiusCeil; x++)
                {
                    float distance = (float)Math.Sqrt(x * x + y * y);
                    if (distance <= radius)
                    {
                        float intensity = 1.0f;
                        if (distance > radius - 1)
                        {
                            intensity = radius - distance;
                        }
                        plot(centerX + x, centerY + y, intensity);
                    }
                }
            }
        }

        private void DrawSingleLine(ConsoleBuffer buffer, int x0, int y0, int x1, int y1, Action<int, int, float> plot)
        {
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep)
            {
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }

            float dx = x1 - x0;
            float dy = y1 - y0;

            // Handle vertical lines
            float gradient;
            if (Math.Abs(dx) < 0.001f)
            {
                gradient = dy >= 0 ? 1.0f : -1.0f;
            }
            else
            {
                gradient = dy / dx;
            }

            // Handle first endpoint
            float xend = (float)Math.Round((double)x0);
            float yend = y0 + gradient * (xend - x0);
            float xgap = 1.0f - ((x0 + 0.5f) % 1.0f);
            int xpxl1 = (int)xend;
            int ypxl1 = (int)Math.Floor(yend);

            if (steep)
            {
                plot(ypxl1, xpxl1, (float)((1 - (yend - Math.Floor(yend))) * xgap));
                plot(ypxl1 + 1, xpxl1, (float)((yend - Math.Floor(yend)) * xgap));
            }
            else
            {
                plot(xpxl1, ypxl1, (float)((1 - (yend - Math.Floor(yend))) * xgap));
                plot(xpxl1, ypxl1 + 1, (float)((yend - Math.Floor(yend)) * xgap));
            }

            float intery = yend + gradient;

            // Handle second endpoint
            xend = (float)Math.Round((double)x1);
            yend = y1 + gradient * (xend - x1);
            xgap = 1.0f - (x1 + 0.5f) % 1.0f;
            int xpxl2 = (int)xend;
            int ypxl2 = (int)Math.Floor(yend);

            if (steep)
            {
                plot(ypxl2, xpxl2, (float)((1 - (yend - Math.Floor(yend))) * xgap));
                plot(ypxl2 + 1, xpxl2, (float)((yend - Math.Floor(yend)) * xgap));
            }
            else
            {
                plot(xpxl2, ypxl2, (float)((1 - (yend - Math.Floor(yend))) * xgap));
                plot(xpxl2, ypxl2 + 1, (float)((yend - Math.Floor(yend)) * xgap));
            }

            // Main line drawing loop
            if (steep)
            {
                for (int x = xpxl1 + 1; x < xpxl2; x++)
                {
                    int y = (int)Math.Floor(intery);
                    float frac = intery - y;
                    plot(y, x, 1 - frac);
                    plot(y + 1, x, frac);
                    intery += gradient;
                }
            }
            else
            {
                for (int x = xpxl1 + 1; x < xpxl2; x++)
                {
                    int y = (int)Math.Floor(intery);
                    float frac = intery - y;
                    plot(x, y, 1 - frac);
                    plot(x, y + 1, frac);
                    intery += gradient;
                }
            }
        }

        public override void CalculateLayout(int parentX, int parentY, int parentWidth, int parentHeight)
        {
            // Get inital X/Y
            int parsedX = ParseRelativeValue(X, parentWidth) + parentX;
            int parsedY = ParseRelativeValue(Y, parentHeight) + parentY;

            // Get Min W/H
            ActualMinWidth = Math.Max(0, ParseRelativeValue(MinWidth, parentWidth));
            ActualMinHeight = Math.Max(0, ParseRelativeValue(MinHeight, parentHeight));

            // Get Max W/H
            ActualMaxWidth = String.IsNullOrEmpty(MaxWidth) ? parentWidth : Math.Min(parentWidth, ParseRelativeValue(MaxWidth, parentWidth));
            ActualMaxHeight = String.IsNullOrEmpty(MaxHeight) ? parentWidth : Math.Min(parentWidth, ParseRelativeValue(MaxHeight, parentWidth));

            // Get inital W/H, based on if the Min W/H is bigger than the Actual W/H
            int parsedWidth = Math.Max(ActualMinWidth, ParseRelativeValue(Width, parentWidth));
            int parsedHeight = Math.Max(ActualMinHeight, ParseRelativeValue(Height, parentHeight));

            // Get W/H based on Max W/H
            parsedWidth = String.IsNullOrEmpty(MaxWidth) ? Math.Min(parsedWidth, ActualMaxWidth) : parsedWidth;
            parsedHeight = String.IsNullOrEmpty(MaxHeight) ? Math.Min(parsedHeight, ActualMaxHeight) : parsedHeight;
            switch (XYAlignment)
            {
                case Alignment.TopLeft:
                    ActualX = Math.Max(0, parsedX + parentX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    break;
                case Alignment.TopRight:
                    ActualX = Math.Max(0, parsedX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    ActualX = parentWidth - (ActualX + parsedWidth);

                    break;
                case Alignment.TopCenter:
                    ActualX = Math.Max(0, parsedX + parentX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    ActualX = (parentWidth / 2) - (parsedWidth / 2);
                    break;
                case Alignment.BottomLeft:
                    ActualX = Math.Max(0, parsedX + parentX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    ActualY = parentHeight - (ActualY + parsedHeight);
                    break;
                case Alignment.BottomRight:
                    ActualX = Math.Max(0, parsedX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    ActualY = parentHeight - (ActualY + parsedHeight);
                    ActualX = parentWidth - (ActualX + parsedWidth);
                    break;
                case Alignment.BottomCenter:
                    ActualX = Math.Max(0, parsedX + parentX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    ActualX = (parentWidth / 2) - (parsedWidth / 2);
                    ActualY = parentHeight - (ActualY + parsedHeight);
                    break;
                case Alignment.Centered:
                    ActualX = Math.Max(0, parsedX + parentX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    ActualX = (parentWidth / 2) - (parsedWidth / 2) + parsedX;
                    ActualY = (parentHeight / 2) - (parsedHeight / 2) + parsedY;
                    break;
                case Alignment.CenterLeft:
                    ActualX = Math.Max(0, parsedX + parentX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    ActualY = (parentHeight / 2) - (parsedHeight / 2);
                    break;
                case Alignment.CenterRight:
                    ActualX = Math.Max(0, parsedX);
                    ActualY = Math.Max(0, parsedY + parentY);
                    ActualY = (parentHeight / 2) - (parsedHeight / 2);
                    ActualX = parentWidth - (ActualX + parsedWidth);
                    break;
            }

            ActualWidth = Math.Min(parentWidth - (ActualX - parentX), parsedWidth);
            ActualHeight = Math.Min(parentHeight - (ActualY - parentY), parsedHeight);

            ActualX1 = ParseRelativeValue(X1, ActualWidth) + parentX;
            ActualY1 = ParseRelativeValue(Y1, ActualHeight) + parentY;

            ActualX2 = ParseRelativeValue(X2, ActualWidth) + parentX;
            ActualY2 = ParseRelativeValue(Y2, ActualHeight) + parentY;

            foreach (var child in Children)
            {
                child.CalculateLayout(ActualX, ActualY, ActualWidth, ActualHeight);
            }
        }
    }
}
