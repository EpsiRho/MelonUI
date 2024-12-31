using MelonUI.Attributes;
using MelonUI.Enums;
using MelonUI.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MelonUI.Base
{
    public abstract partial class UIElement
    {
        // CWM Interactions
        public virtual bool DefaultKeyControl { get; set; } = true;
        public virtual bool EnableCaching { get; set; } = true;
        public virtual bool NeedsRecalculation { get; set; } = true;
        public virtual bool RespectBackgroundOnDraw { get; set; } = true;
        public virtual bool RenderThreadDeleteMe { get; set; } = false;
        [Binding]
        private bool _IsFocused = false;
        [Binding]
        private bool _ConsiderForFocus = true;
        [Binding]
        private bool _LockControls = false;
        [Binding]
        private bool _IsVisible = true;
        [Binding]
        private List<KeyboardControl> _KeyboardControls = new List<KeyboardControl>();
        [Binding]
        private string _Name = "";

        // Element Tree
        public UIElement Parent { get; set; }
        public virtual ConsoleWindowManager ParentWindow { get; set; }
        public List<UIElement> Children { get; } = new List<UIElement>();


        // Binding Manager
        protected Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();

        // Position Properties
        [Binding]
        private Alignment _XYAlignment = Alignment.TopLeft;
        [Binding]
        private string _UID = Guid.NewGuid().ToString();
        [Binding]
        private string _X = "0";
        [Binding]
        private string _Y = "0";
        [Binding]
        private string _Width = "0";
        [Binding]
        private string _Height = "0";
        [Binding]
        private string _MinWidth = "5";
        [Binding]
        private string _MinHeight = "3";
        [Binding]
        private string _MaxWidth = "";
        [Binding]
        private string _MaxHeight = "";
        [Binding]
        private int _Z = 0;

        // Calculated Properties
        public int ActualX { get; protected set; }
        public int ActualY { get; protected set; }
        public int ActualWidth { get; protected set; }
        public int ActualHeight { get; protected set; }
        public int ActualMinWidth { get; protected set; }
        public int ActualMinHeight { get; protected set; }
        public int ActualMaxWidth { get; protected set; }
        public int ActualMaxHeight { get; protected set; }

        // Box drawing configuration
        [Binding]
        private bool _ShowBorder = true;
        [Binding]
        private Color _BorderColor = Color.Gray;
        [Binding]
        private Color _FocusedBorderColor = Color.Cyan;
        [Binding]
        private Color _Foreground = Color.White;
        [Binding]
        private Color _Background = Color.FromArgb(0, 0, 0, 0);
        [Binding]
        private Color _FocusedForeground = Color.Cyan;
        [Binding]
        private Color _FocusedBackground = Color.FromArgb(0, 0, 0, 0);

        // Box drawing characters - can be overridden by derived classes
        protected virtual char BoxTopLeft => '┌';
        protected virtual char BoxTopRight => '┐';
        protected virtual char BoxBottomLeft => '└';
        protected virtual char BoxBottomRight => '┘';
        protected virtual char BoxHorizontal => '─';
        protected virtual char BoxVertical => '│';


        public UIElement()
        {
        }

        /// <summary>
        /// Sets a binding for a property or event.
        /// </summary>
        public void SetBinding(string propertyName, Binding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            _bindings[propertyName] = binding;
            NeedsRecalculation = true;
        }

        /// <summary>
        /// Gets the bound value or the local value.
        /// </summary>
        protected object GetBoundValue(string propertyName, object localValue)
        {
            try
            {
                if (_bindings.TryGetValue(propertyName, out var binding))
                {
                    if (binding.IsProperty)
                        return binding.GetValue();
                }
                return localValue;
            }
            catch (Exception)
            {
                return localValue;
            }
        }

        /// <summary>
        /// Sets the bound value or the local value.
        /// </summary>
        protected void SetBoundValue(string propertyName, T value, ref T localStorage)
        {
            if (_bindings.TryGetValue(propertyName, out var binding))
            {
                if (binding.IsProperty)
                {
                    binding.SetValue(value);
                    NeedsRecalculation = true;
                    return;
                }
                // Event bindings are handled separately
            }

            // Not bound, set locally
            localStorage = value;
            NeedsRecalculation = true;
        }

        // Base rendering method
        public ConsoleBuffer Render()
        {
            var buffer = new ConsoleBuffer(ActualWidth, ActualHeight);
            var background = IsFocused ? FocusedBackground : Background;
            buffer.Clear(background);

            if (!IsVisible || ActualWidth == 0 || ActualHeight == 0)
            {
                return buffer;
            }

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
            if (LockControls)
            {
                return;
            }
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
