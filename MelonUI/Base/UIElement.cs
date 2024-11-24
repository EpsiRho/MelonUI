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
        public bool HasCalculatedLayout { get; set; }
        public UIElement Parent { get; set; }
        public List<UIElement> Children { get; } = new List<UIElement>();

        // Box drawing configuration
        public bool ShowBorder { get; set; } = true;
        public virtual ConsoleColor BorderColor { get; set; } = ConsoleColor.Gray;
        public virtual ConsoleColor FocusedBorderColor { get; set; } = ConsoleColor.Cyan;
        public virtual ConsoleColor Foreground { get; set; } = ConsoleColor.White;
        public virtual ConsoleColor Background { get; set; } = ConsoleColor.Black;
        public virtual ConsoleColor FocusedForeground { get; set; } = ConsoleColor.Cyan;
        public virtual ConsoleColor FocusedBackground { get; set; } = ConsoleColor.Black;

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
            ActualX = Math.Max(0, ParseRelativeValue(RelativeX, parentWidth) + parentX);
            ActualY = Math.Max(0, ParseRelativeValue(RelativeY, parentHeight) + parentY);
            ActualWidth = Math.Min(parentWidth - (ActualX - parentX), ParseRelativeValue(RelativeWidth, parentWidth));
            ActualHeight = Math.Min(parentHeight - (ActualY - parentY), ParseRelativeValue(RelativeHeight, parentHeight));

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

        protected List<KeyboardControl> KeyboardControls { get; } = new();

        protected void RegisterKeyboardControl(ConsoleKey key, Action action, string description,
            bool requireShift = false, bool requireControl = false, bool requireAlt = false)
        {
            KeyboardControls.Add(new KeyboardControl
            {
                Key = key,
                Action = action,
                Description = description,
                RequireShift = requireShift,
                RequireControl = requireControl,
                RequireAlt = requireAlt
            });
        }
        protected void RegisterKeyboardControl(KeyboardControl keyControl)
        {
            KeyboardControls.Add(keyControl);
        }

        public virtual IEnumerable<KeyboardControl> GetKeyboardControls()
        {
            return KeyboardControls;
        }
        public virtual void HandleKey(ConsoleKeyInfo keyInfo)
        {
            foreach (var control in KeyboardControls)
            {
                if (control.Matches(keyInfo))
                {
                    control.Action();
                }
            }
        }
    }
}
