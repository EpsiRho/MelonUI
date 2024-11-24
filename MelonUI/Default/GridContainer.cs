using System;
using System.Collections.Generic;
using System.Linq;
namespace MelonUI.Base
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
        private int _currentAnimationStep;
        private const int AnimationSteps = 4;
        private int _renderCounter;

        public GridContainer(int rows, bool animateExpansion = false, params int[] columnsPerRow)
        {
            _rows = rows;
            _columnsPerRow = columnsPerRow;
            ShowBorder = false; // Disable default border, as we will draw our own
            _animateExpansion = animateExpansion;
            _currentAnimationStep = animateExpansion ? 1 : AnimationSteps;
            _renderCounter = 0;
        }

        public void AddElement(UIElement element, int row, int column)
        {
            _positions.Add(new GridPosition
            {
                Element = element,
                Row = row,
                Column = column
            });
            Children.Add(element);
            element.Parent = this;

            foreach (var control in element.GetKeyboardControls())
            {
                if (!_aggregatedControls.ContainsKey(control.Key))
                {
                    _aggregatedControls[control.Key] = new List<KeyboardControl>();
                }
                _aggregatedControls[control.Key].Add(control);
            }
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
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

                pos.Element.RelativeWidth = adjustedColumnWidth.ToString();
                pos.Element.RelativeHeight = adjustedRowHeight.ToString();

                pos.Element.CalculateLayout(startX, startY, adjustedColumnWidth, adjustedRowHeight);
                var elementBuffer = pos.Element.Render();
                buffer.Write(pos.Element.ActualX, pos.Element.ActualY, elementBuffer);
            }



            // If animation is enabled and not complete, increment the step after a few renders
            if (_animateExpansion && _currentAnimationStep < AnimationSteps)
            {
                _renderCounter++;
                if (_renderCounter >= 3) // Update animation step after every few renders
                {
                    _currentAnimationStep++;
                    _renderCounter = 0;
                }
            }
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
            if (_aggregatedControls.TryGetValue(keyInfo.Key, out var controls))
            {
                foreach (var control in controls)
                {
                    if (control.Matches(keyInfo))
                    {
                        control.Action();
                    }
                }
            }
        }
    }
}
