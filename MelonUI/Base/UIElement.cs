using MelonUI.Enums;
using MelonUI.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MelonUI.Base
{
    public abstract class UIElement
    {
        public virtual bool DefaultKeyControl { get; set; } = true;
        public virtual bool EnableCaching { get; set; } = true;
        public virtual bool NeedsRecalculation { get; set; } = true;
        public virtual bool RenderThreadDeleteMe { get; set; } = false;
        protected Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();
        private object _XYAlignment = Alignment.TopLeft;
        public Alignment XYAlignment
        {
            get => (Alignment)GetBoundValue(nameof(XYAlignment), _XYAlignment);
            set => SetBoundValue(nameof(XYAlignment), value, ref _XYAlignment);
        }
        private object _UID = Guid.NewGuid().ToString();
        public string UID
        {
            get => (string)GetBoundValue(nameof(UID), _UID);
            set => SetBoundValue(nameof(UID), value, ref _UID);
        }
        private object _X = "0";
        public string X
        {
            get
            {
                var val = GetBoundValue(nameof(X), $"{_X}");
                return $"{val}";
            }
            set => SetBoundValue(nameof(X), value, ref _X);
        }
        private object _Y = "0";
        public string Y
        {
            get
            {
                var val = GetBoundValue(nameof(Y), $"{_Y}");
                return $"{val}";
            }
            set => SetBoundValue(nameof(Y), value, ref _Y);
        }
        private object _Width = "0";
        public string Width
        {
            get
            {
                var val = GetBoundValue(nameof(Width), $"{_Width}");
                return $"{val}";
            }
            set => SetBoundValue(nameof(Width), value, ref _Width);
        }
        private object _Height = "0";
        public string Height
        {
            get
            {
                var val = GetBoundValue(nameof(Height), $"{_Height}");
                return $"{val}";
            }
            set => SetBoundValue(nameof(Height), value, ref _Height);
        }
        private object _MinWidth = "5";
        public string MinWidth
        {
            get 
            { 
                var val = GetBoundValue(nameof(MinWidth), $"{_MinWidth}");
                return $"{val}";
            }

            set => SetBoundValue(nameof(MinWidth), value, ref _MinWidth);
        }
        private object _MinHeight = "3";
        public string MinHeight
        {
            get
            {
                var val = GetBoundValue(nameof(MinHeight), $"{_MinHeight}");
                return $"{val}";
            }
            set => SetBoundValue(nameof(MinHeight), value, ref _MinHeight);
        }
        private object _MaxWidth = "";
        public string MaxWidth
        {
            get
            {
                var val = GetBoundValue(nameof(MaxWidth), $"{_MaxWidth}");
                return $"{val}";
            }

            set => SetBoundValue(nameof(MaxWidth), value, ref _MaxWidth);
        }
        private object _MaxHeight = "";
        public string MaxHeight
        {
            get
            {
                var val = GetBoundValue(nameof(MaxHeight), $"{_MaxHeight}");
                return $"{val}";
            }
            set => SetBoundValue(nameof(MaxHeight), value, ref _MaxHeight);
        }
        private object _Z = 0;
        public int Z
        {
            get => (int)GetBoundValue(nameof(Z), _Z);
            set => SetBoundValue(nameof(Z), value, ref _Z);
        }
        public int ActualX { get; protected set; }
        public int ActualY { get; protected set; }
        public int ActualWidth { get; protected set; }
        public int ActualHeight { get; protected set; }
        public int ActualMinWidth { get; protected set; }
        public int ActualMinHeight { get; protected set; }
        public int ActualMaxWidth { get; protected set; }
        public int ActualMaxHeight { get; protected set; }
        private object _IsFocused = false;
        public bool IsFocused
        {
            get => (bool)GetBoundValue(nameof(IsFocused), _IsFocused);
            set => SetBoundValue(nameof(IsFocused), value, ref _IsFocused);
        }
        private object _ConsiderForFocus = true;
        public virtual bool ConsiderForFocus
        {
            get => (bool)GetBoundValue(nameof(ConsiderForFocus), _ConsiderForFocus);
            set => SetBoundValue(nameof(ConsiderForFocus), value, ref _ConsiderForFocus);
        }
        public object _LockControls = false;
        public virtual bool LockControls
        {
            get => (bool)GetBoundValue(nameof(LockControls), _LockControls);
            set => SetBoundValue(nameof(LockControls), value, ref _LockControls);
        }
        public object _IsVisible = true;
        public bool IsVisible
        {
            get => (bool)GetBoundValue(nameof(IsVisible), _IsVisible);
            set => SetBoundValue(nameof(IsVisible), value, ref _IsVisible);
        }
        public bool HasCalculatedLayout { get; set; }
        public UIElement Parent { get; set; }
        public virtual ConsoleWindowManager ParentWindow { get; set; }
        public List<UIElement> Children { get; } = new List<UIElement>();
        public object _Name = "";
        public string? Name
        {
            get => (string?)GetBoundValue(nameof(Name), _Name);
            set => SetBoundValue(nameof(Name), value, ref _Name);
        }
        protected List<KeyboardControl> KeyboardControls { get; } = new();

        // Box drawing configuration
        public object _ShowBorder = true;
        public bool ShowBorder
        {
            get => (bool)GetBoundValue(nameof(ShowBorder), _ShowBorder);
            set => SetBoundValue(nameof(ShowBorder), value, ref _ShowBorder);
        }
        public object _BorderColor = Color.Gray;
        public Color BorderColor
        {
            get => (Color)GetBoundValue(nameof(BorderColor), _BorderColor);
            set => SetBoundValue(nameof(BorderColor), value, ref _BorderColor);
        }
        public object _FocusedBorderColor = Color.Cyan;
        public Color FocusedBorderColor
        {
            get => (Color)GetBoundValue(nameof(FocusedBorderColor), _FocusedBorderColor);
            set => SetBoundValue(nameof(FocusedBorderColor), value, ref _FocusedBorderColor);
        }
        public object _Foreground = Color.White;
        public Color Foreground
        {
            get => (Color)GetBoundValue(nameof(Foreground), _Foreground);
            set => SetBoundValue(nameof(Foreground), value, ref _Foreground);
        }
        public object _Background = Color.FromArgb(0, 0, 0, 0);
        public Color Background
        {
            get => (Color)GetBoundValue(nameof(Background), _Background);
            set => SetBoundValue(nameof(Background), value, ref _Background);
        }
        public object _FocusedForeground = Color.Cyan;
        public Color FocusedForeground
        {
            get => (Color)GetBoundValue(nameof(FocusedForeground), _FocusedForeground);
            set => SetBoundValue(nameof(FocusedForeground), value, ref _FocusedForeground);
        }
        public object _FocusedBackground = Color.FromArgb(0, 0, 0, 0);
        public Color FocusedBackground
        {
            get => (Color)GetBoundValue(nameof(FocusedBackground), _FocusedBackground);
            set => SetBoundValue(nameof(FocusedBackground), value, ref _FocusedBackground);
        }

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
            if (_bindings.TryGetValue(propertyName, out var binding))
            {
                if (binding.IsProperty)
                    return binding.GetValue();
            }
            return localValue;
        }

        /// <summary>
        /// Sets the bound value or the local value.
        /// </summary>
        protected void SetBoundValue(string propertyName, object value, ref object localStorage)
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
            int parsedX = Math.Max(0, ParseRelativeValue(X, parentWidth) + parentX);
            int parsedY = Math.Max(0, ParseRelativeValue(Y, parentHeight) + parentY);

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
                    ActualX = (parentWidth / 2) - (parsedWidth / 2);
                    ActualY = (parentHeight / 2) - (parsedHeight / 2);
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
