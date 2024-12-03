using MelonUI.Base;
using MelonUI.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
namespace MelonUI.Default
{
    public class GridContainer : UIElement
    {
        private class GridPosition
        {
            public UIElement Element { get; set; }
            public int Row { get; set; }
            public int Column { get; set; }
        }

        private readonly int _rows;
        private readonly int[] _columnsPerRow;
        private readonly List<GridPosition> _positions = new();
        private Dictionary<ConsoleKey, List<KeyboardControl>> _aggregatedControls = new();
        private bool _animateExpansion;
        private bool _animateCollapse;
        private int _currentAnimationStep;
        private const int AnimationSteps = 10;
        private int _renderCounter;
        public ConsoleWindowManager _parentWindow;
        private Action tempCloseAction;
        public override ConsoleWindowManager ParentWindow
        {
            get
            {
                return _parentWindow;
            }
            set
            {
                if (value != _parentWindow)
                {
                    SetParentWindows(value);
                }
            }
        }

        public GridContainer(int rows, bool animateExpansion = false, params int[] columnsPerRow)
        {
            _rows = rows;
            _columnsPerRow = columnsPerRow;
            ShowBorder = false; // Disable default border, as we will draw our own
            _animateExpansion = animateExpansion;
            _currentAnimationStep = animateExpansion ? 1 : AnimationSteps;
            _renderCounter = 0;
        }
        public void SetParentWindows(ConsoleWindowManager parent)
        {
            this._parentWindow = parent;
            foreach(var pos in _positions)
            {
                pos.Element.ParentWindow = parent;
            }
        }

        public void AddElement(UIElement element, int row, int column)
        {
            element.Parent = this;
            element.ParentWindow = this.ParentWindow;
            _positions.Add(new GridPosition
            {
                Element = element,
                Row = row,
                Column = column
            });
            Children.Add(element);

            foreach (var control in element.GetKeyboardControls())
            {
                if(!control.Key.HasValue)
                {
                    control.Key = ConsoleKey.None;
                }

                if (!_aggregatedControls.ContainsKey(control.Key.Value))
                {
                    _aggregatedControls[control.Key.Value] = new List<KeyboardControl>();
                }
                _aggregatedControls[control.Key.Value].Add(control);
            }
        }
        public void RemoveElement(UIElement element)
        {
            Children.Remove(element);
            foreach (var control in element.GetKeyboardControls())
            {
                if (!control.Key.HasValue)
                {
                    control.Key = ConsoleKey.None;
                }

                if (_aggregatedControls.ContainsKey(control.Key.Value))
                {
                    _aggregatedControls[control.Key.Value].Remove(control);
                }
            }
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (!IsVisible)
            {
                _currentAnimationStep = 1;
                _renderCounter = 0;
                return;
            }

            if (_animateCollapse && _currentAnimationStep <= 1)
            {
                _animateCollapse = false;
                IsVisible = false;
                if (tempCloseAction != null)
                {
                    tempCloseAction();
                    tempCloseAction = null;
                }
            }

            if(_animateCollapse || _animateExpansion)
            {
                NeedsRecalculation = true;
            }
            else
            {
                NeedsRecalculation = false;
            }

            // Calculate the layout for all elements first
            int innerWidth = buffer.Width - (ShowBorder ? 2 : 0);
            int innerHeight = buffer.Height - (ShowBorder ? 2 : 0);

            // Determine the current height and width based on animation step
            int animatedWidth = innerWidth * _currentAnimationStep / AnimationSteps;
            int animatedHeight = innerHeight * _currentAnimationStep / AnimationSteps;

            // Calculate starting position to center the animation
            int startXOffset = (buffer.Width - animatedWidth) / 2;
            int startYOffset = (buffer.Height - animatedHeight) / 2;
            int rowHeight = (animatedHeight-1) / _rows;

            // Draw the border scaled with the animation
            DrawAnimatedBorder(buffer, animatedWidth, animatedHeight, startXOffset, startYOffset);

            foreach (var pos in _positions)
            {
                int columnWidth =( animatedWidth / _columnsPerRow[pos.Row]) - 1;
                int startX = startXOffset + (columnWidth * pos.Column) + 1; // Adjust for accurate positioning
                int startY = startYOffset + (rowHeight * pos.Row) + 1; // Adjust for accurate positioning
                int adjustedColumnWidth = columnWidth;
                int adjustedRowHeight = rowHeight;

                // Ensure the last column and row fit perfectly within the border
                if (pos.Column == _columnsPerRow[pos.Row] - 1)
                {
                    adjustedColumnWidth = animatedWidth - (columnWidth * pos.Column) - 2;
                }
                if (pos.Row == _rows - 1)
                {
                    adjustedRowHeight = animatedHeight - (rowHeight * pos.Row) - 2;
                }

                pos.Element.Width = adjustedColumnWidth.ToString();
                pos.Element.Height = adjustedRowHeight.ToString();

                pos.Element.CalculateLayout(startX, startY, adjustedColumnWidth, adjustedRowHeight);
                var elementBuffer = pos.Element.Render();
                buffer.Write(pos.Element.ActualX, pos.Element.ActualY, elementBuffer);
            }


            if (_animateCollapse && _currentAnimationStep > 0)
            {
                _renderCounter++;
                if (_renderCounter >= 1) // Update animation step after every few renders
                {
                    _currentAnimationStep--;
                    _renderCounter = 0;
                }
            }

            if (_animateExpansion  && !_animateCollapse && _currentAnimationStep < AnimationSteps)
            {
                _renderCounter++;
                if (_renderCounter >= 1) // Update animation step after every few renders
                {
                    _currentAnimationStep++;
                    _renderCounter = 0;
                }
            }

        }
        public void AnimateClose(Action finalAction = null)
        {
            if (!IsVisible) return;
            _animateCollapse = true;
            _currentAnimationStep = AnimationSteps;
            tempCloseAction = finalAction;
        }

        private void DrawAnimatedBorder(ConsoleBuffer buffer, int width, int height, int offsetX, int offsetY)
        {
            var foreground = IsFocused ? FocusedBorderColor : BorderColor;
            var background = IsFocused ? FocusedBackground : Background;

            // Draw corners
            buffer.SetPixel(offsetX, offsetY, BoxTopLeft, foreground, background);
            buffer.SetPixel(offsetX + width - 1, offsetY, BoxTopRight, foreground, background);
            buffer.SetPixel(offsetX, offsetY + height - 1, BoxBottomLeft, foreground, background);
            buffer.SetPixel(offsetX + width - 1, offsetY + height - 1, BoxBottomRight, foreground, background);

            // Draw top and bottom edges
            for (int x = 1; x < width - 1; x++)
            {
                buffer.SetPixel(offsetX + x, offsetY, BoxHorizontal, foreground, background);
                buffer.SetPixel(offsetX + x, offsetY + height - 1, BoxHorizontal, foreground, background);
            }

            // Draw left and right edges
            for (int y = 1; y < height - 1; y++)
            {
                buffer.SetPixel(offsetX, offsetY + y, BoxVertical, foreground, background);
                buffer.SetPixel(offsetX + width - 1, offsetY + y, BoxVertical, foreground, background);
            }
        }

        public override void HandleKey(ConsoleKeyInfo keyInfo)
        {
            bool match = false;
            if (_aggregatedControls.TryGetValue(keyInfo.Key, out var controls))
            {
                foreach (var control in controls)
                {
                    if (control.Matches(keyInfo))
                    {
                        control.Action();
                        match = true;
                    }
                }
            }

            if (_aggregatedControls.ContainsKey(ConsoleKey.None))
            {
                foreach (var wild in _aggregatedControls[ConsoleKey.None])
                {
                    if (wild.Matches(keyInfo))
                    {
                        wild.Action();
                        match = true;
                    }
                }
            }

            foreach (var item in Children)
            {
                if (!item.DefaultKeyControl)
                {
                    item.HandleKey(keyInfo);
                }
            }
        }
    }
}
