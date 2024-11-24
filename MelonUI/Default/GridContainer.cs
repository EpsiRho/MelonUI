using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class GridContainer : UIElement
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int CellPadding { get; set; } = 1;
        public bool EqualCellSize { get; set; } = true;

        private int _selectedCell = 0;
        private List<(UIElement element, int row, int col)> _gridItems = new();

        public GridContainer()
        {
            ShowBorder = true;
        }

        public void AddElement(UIElement element, int row, int col)
        {
            if (row >= Rows || col >= Columns)
                throw new ArgumentException($"Position ({row},{col}) is outside grid bounds ({Rows},{Columns})");

            // Remove focus capability from the element
            element.IsFocused = false;

            _gridItems.Add((element, row, col));
            Children.Add(element);

            // Update grid dimensions if needed
            Rows = Math.Max(Rows, row + 1);
            Columns = Math.Max(Columns, col + 1);
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var background = IsFocused ? FocusedBackground : Background;

            // Calculate cell dimensions
            int contentWidth = ActualWidth - 2;  // Account for border
            int contentHeight = ActualHeight - 2;

            int cellWidth = (contentWidth - (CellPadding * (Columns - 1))) / Columns;
            int cellHeight = (contentHeight - (CellPadding * (Rows - 1))) / Rows;

            // If not using equal cell sizes, we need to calculate each cell's size based on its content
            if (!EqualCellSize)
            {
                // TODO: Implement variable cell sizing based on content
                // For now, we'll use equal sizing
            }

            // Render each cell
            foreach (var (element, row, col) in _gridItems)
            {
                // Calculate cell position
                int cellX = 1 + (col * (cellWidth + CellPadding));
                int cellY = 1 + (row * (cellHeight + CellPadding));

                // Calculate if this cell is selected
                int cellIndex = row * Columns + col;
                bool isSelected = IsFocused && cellIndex == _selectedCell;

                // Update element properties
                element.X = cellX;
                element.Y = cellY;
                element.RelativeWidth = cellWidth.ToString();
                element.RelativeHeight = cellHeight.ToString();

                // Create a sub-buffer for this cell
                var cellBuffer = new ConsoleBuffer(cellWidth, cellHeight);
                if (isSelected)
                {
                    cellBuffer.Clear(ConsoleColor.DarkBlue);
                }
                else
                {
                    cellBuffer.Clear(background);
                }

                // Render the element into the cell buffer
                var elementBuffer = element.Render();
                cellBuffer.WriteBuffer(0, 0, elementBuffer);

                // Copy the cell buffer to the main buffer
                buffer.WriteBuffer(cellX, cellY, cellBuffer);
            }
        }

        public override void HandleKey(ConsoleKeyInfo keyInfo)
        {
            if (!IsFocused || _gridItems.Count == 0) return;

            int currentRow = _selectedCell / Columns;
            int currentCol = _selectedCell % Columns;
            int newRow = currentRow;
            int newCol = currentCol;

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    newRow = Math.Max(0, currentRow - 1);
                    break;
                case ConsoleKey.DownArrow:
                    newRow = Math.Min(Rows - 1, currentRow + 1);
                    break;
                case ConsoleKey.LeftArrow:
                    newCol = Math.Max(0, currentCol - 1);
                    break;
                case ConsoleKey.RightArrow:
                    newCol = Math.Min(Columns - 1, currentCol + 1);
                    break;
                default:
                    // Forward the key press to the selected element
                    if (_selectedCell < _gridItems.Count)
                    {
                        _gridItems[_selectedCell].element.HandleKey(keyInfo);
                    }
                    break;
            }

            // Update selected cell if it changed
            int newSelected = newRow * Columns + newCol;
            if (newSelected != _selectedCell && HasElementAt(newRow, newCol))
            {
                _selectedCell = newSelected;
            }
        }

        private bool HasElementAt(int row, int col)
        {
            return _gridItems.Any(item => item.row == row && item.col == col);
        }
    }
}
