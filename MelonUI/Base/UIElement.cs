using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public abstract class UIElement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string RelativeX { get; set; }
        public string RelativeY { get; set; }
        public string RelativeWidth { get; set; }
        public string RelativeHeight { get; set; }
        public int ActualX { get; protected set; }
        public int ActualY { get; protected set; }
        public int ActualWidth { get; protected set; }
        public int ActualHeight { get; protected set; }
        public bool IsFocused { get; set; }
        public UIElement Parent { get; set; }
        public List<UIElement> Children { get; } = new List<UIElement>();

        // Box drawing configuration
        public bool ShowBorder { get; set; } = true;
        public virtual ConsoleColor BorderColor => ConsoleColor.Gray;
        public virtual ConsoleColor FocusedBorderColor => ConsoleColor.Cyan;
        public virtual ConsoleColor Foreground => ConsoleColor.White;
        public virtual ConsoleColor Background => ConsoleColor.Black;
        public virtual ConsoleColor FocusedForeground => ConsoleColor.Cyan;
        public virtual ConsoleColor FocusedBackground => ConsoleColor.Black;

        // Box drawing characters - can be overridden by derived classes
        protected virtual char BoxTopLeft => '┌';
        protected virtual char BoxTopRight => '┐';
        protected virtual char BoxBottomLeft => '└';
        protected virtual char BoxBottomRight => '┘';
        protected virtual char BoxHorizontal => '─';
        protected virtual char BoxVertical => '│';

        // Base rendering method
        public ConsoleBuffer Render()
        {
            var buffer = new ConsoleBuffer(ActualWidth, ActualHeight);
            var background = IsFocused ? FocusedBackground : Background;
            buffer.Clear(background);

            // Draw border if enabled
            if (ShowBorder)
            {
                DrawBorder(buffer);
            }

            // Call the derived class's rendering implementation
            RenderContent(buffer);

            return buffer;
        }

        protected void DrawBorder(ConsoleBuffer buffer)
        {
            var foreground = IsFocused ? FocusedBorderColor : BorderColor;
            var background = IsFocused ? FocusedBackground : Background;

            // Draw corners
            buffer.SetPixel(0, 0, BoxTopLeft, foreground, background);
            buffer.SetPixel(ActualWidth - 1, 0, BoxTopRight, foreground, background);
            buffer.SetPixel(0, ActualHeight - 1, BoxBottomLeft, foreground, background);
            buffer.SetPixel(ActualWidth - 1, ActualHeight - 1, BoxBottomRight, foreground, background);

            // Draw top and bottom edges
            for (int x = 1; x < ActualWidth - 1; x++)
            {
                buffer.SetPixel(x, 0, BoxHorizontal, foreground, background);
                buffer.SetPixel(x, ActualHeight - 1, BoxHorizontal, foreground, background);
            }

            // Draw left and right edges
            for (int y = 1; y < ActualHeight - 1; y++)
            {
                buffer.SetPixel(0, y, BoxVertical, foreground, background);
                buffer.SetPixel(ActualWidth - 1, y, BoxVertical, foreground, background);
            }
        }
        // Classes should implement this themselves
        protected abstract void RenderContent(ConsoleBuffer buffer);

        public virtual void CalculateLayout(int parentX, int parentY, int parentWidth, int parentHeight)
        {
            ActualX = ParseRelativeValue(RelativeX, parentWidth) + parentX;
            ActualY = ParseRelativeValue(RelativeY, parentHeight) + parentY;
            ActualWidth = ParseRelativeValue(RelativeWidth, parentWidth);
            ActualHeight = ParseRelativeValue(RelativeHeight, parentHeight);

            foreach (var child in Children)
            {
                child.CalculateLayout(ActualX, ActualY, ActualWidth, ActualHeight);
            }
        }

        private int ParseRelativeValue(string value, int parentSize)
        {
            if (string.IsNullOrEmpty(value)) return 0;

            if (value.EndsWith("%"))
            {
                if (int.TryParse(value.TrimEnd('%'), out int percentage))
                {
                    return (parentSize * percentage) / 100;
                }
            }
            else if (value == "center")
            {
                return parentSize / 2;
            }
            else if (int.TryParse(value, out int absolute))
            {
                return absolute;
            }

            return 0;
        }

        public virtual void HandleKey(ConsoleKeyInfo keyInfo) { }
    }
}
