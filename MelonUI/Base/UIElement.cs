using MelonUI.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public abstract class UIElement
    {
        public virtual bool DefaultKeyControl { get; set; } = true;
        public virtual bool EnableCaching { get; set; } = true;
        public virtual bool NeedsRecalculation { get; set; } = true;
        public virtual bool RenderThreadDeleteMe { get; set; } = false;
        public string X { get; set; }
        public string Y { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public int Z { get; set; } = 0;
        public int ActualX { get; protected set; }
        public int ActualY { get; protected set; }
        public int ActualWidth { get; protected set; }
        public int ActualHeight { get; protected set; }
        public bool IsFocused { get; set; }
        public bool _IsVisible = true;
        public bool ControlLock { get; set; }
        public bool IsVisible
        {
            get
            {
                return _IsVisible;
            }
            set
            {
                if (_IsVisible != value)
                {
                    _IsVisible = value;
                }
            }
        }
        public bool HasCalculatedLayout { get; set; }
        public UIElement Parent { get; set; }
        public virtual ConsoleWindowManager ParentWindow { get; set; }
        public List<UIElement> Children { get; } = new List<UIElement>();
        public string? Name { get; set; }
        protected List<KeyboardControl> KeyboardControls { get; } = new();

        // Box drawing configuration
        public virtual bool ShowBorder { get; set; } = true;
        public virtual Color BorderColor { get; set; } = Color.Gray;
        public virtual Color FocusedBorderColor { get; set; } = Color.Cyan;
        public virtual Color Foreground { get; set; } = Color.White;
        public virtual Color Background { get; set; } = Color.Black;
        public virtual Color FocusedForeground { get; set; } = Color.Cyan;
        public virtual Color FocusedBackground { get; set; } = Color.Black;

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
            ActualX = Math.Max(0, ParseRelativeValue(X, parentWidth) + parentX);
            ActualY = Math.Max(0, ParseRelativeValue(Y, parentHeight) + parentY);
            ActualWidth = Math.Min(parentWidth - (ActualX - parentX), ParseRelativeValue(Width, parentWidth));
            ActualHeight = Math.Min(parentHeight - (ActualY - parentY), ParseRelativeValue(Height, parentHeight));

            foreach (var child in Children)
            {
                child.CalculateLayout(ActualX, ActualY, ActualWidth, ActualHeight);
            }
        }

        public int GetAbsoluteX()
        {
            int absoluteX = ActualX;
            UIElement current = Parent;

            while (current != null)
            {
                // Add parent's position
                absoluteX += current.ActualX;

                // If parent has a border, account for it
                if (current.ShowBorder)
                {
                    absoluteX += 1;
                }

                current = current.Parent;
            }

            return absoluteX;
        }

        public int GetAbsoluteY()
        {
            int absoluteY = ActualY;
            UIElement current = Parent;

            while (current != null)
            {
                // Add parent's position
                absoluteY += current.ActualY;

                // If parent has a border, account for it
                if (current.ShowBorder)
                {
                    absoluteY += 1;
                }

                current = current.Parent;
            }

            // Account for the CWM's title/status bar
            if (ParentWindow != null)
            {
                absoluteY += 2;
            }

            return absoluteY;
        }

        public (int X, int Y) GetAbsolutePosition()
        {
            return (GetAbsoluteX(), GetAbsoluteY());
        }

        public string GetRootParentRelativeWidth()
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current.Width;
        }

        public string GetRootParentRelativeHeight()
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current.Height;
        }

        public string GetRootParentRelativeX()
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current.X;
        }

        public string GetRootParentRelativeY()
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current.Y;
        }

        public (string Width, string Height, string X, string Y) GetRootParentRelativeDimensions()
        {
            return (GetRootParentRelativeWidth(), GetRootParentRelativeHeight(), GetRootParentRelativeX(), GetRootParentRelativeY());
        }
        public int ParseRelativeValue(string value, int parentSize)
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


        public void RegisterKeyboardControl(ConsoleKey key, Action action, string description,
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
        public void RegisterKeyboardControl(KeyboardControl keyControl)
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
