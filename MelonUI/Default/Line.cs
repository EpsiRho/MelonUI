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
        public int ActualX1 { get; set; }
        public int ActualY1 { get; set; }
        public int ActualX2 { get; set; }
        public int ActualY2 { get; set; }
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
                RenderDiagonalLine(buffer);
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
