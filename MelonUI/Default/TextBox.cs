using MelonUI.Attributes;
using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MelonUI.Default
{
    public partial class TextBox : UIElement
    {
        [Binding]
        private string text = "";
        [Binding]
        private string label = "";
        public int CursorPosition { get; private set; } = 0;
        [Binding]
        public char hiddenCharacter = '*';
        private object _HideCharacters = false;
        public bool HideCharacters
        {
            get
            {
                return (bool)GetBoundValue(nameof(HideCharacters), _HideCharacters);
            }
            set
            {
                SetBoundValue(nameof(HideCharacters), value, ref _HideCharacters);
                IsCharactersHidden = value;
            }
        }
        [Binding]
        private bool isCharactersHidden = false;
        [Binding]
        private Color cursorColorForeground = Color.Cyan;
        [Binding]
        private Color cursorColorBackground = Color.Gray;
        [Binding]
        private Action<string, TextBox> onTextChanged;
        [Binding]
        public Action<string, TextBox> onEnter;
        [Binding]
        public Action enterHit;

        [Binding]
        private bool multiline = false;

        private int _scrollOffset;
        private int _verticalScrollOffset;
        private int? _preferredColumn;
        private const int SCROLL_MARGIN = 2;

        public TextBox()
        {
            var enter = new KeyboardControl
            {
                Description = "Enter (Shift+Enter submits in multiline mode)",
                Wildcard = key => key.Key == ConsoleKey.Enter
            };
            enter.Action = () =>
            {
                if (Multiline && (enter.KeyInfo.Value.Modifiers & ConsoleModifiers.Shift) == 0)
                    InsertText("\n");
                else
                {
                    OnEnter?.Invoke(Text, this);
                    EnterHit?.Invoke();
                }
            };
            RegisterKeyboardControl(enter);
            RegisterKeyboardControl(ConsoleKey.LeftArrow, () => MoveHorizontal(-1), "Move cursor left");
            RegisterKeyboardControl(ConsoleKey.RightArrow, () => MoveHorizontal(1), "Move cursor right");
            RegisterKeyboardControl(ConsoleKey.Home, () => MoveToEdge(false), "Move to line start");
            RegisterKeyboardControl(ConsoleKey.End, () => MoveToEdge(true), "Move to line end");

            // Leave Up/Down available to parent navigation in single-line mode.
            var vertical = new KeyboardControl
            {
                Description = "Move cursor up/down",
                Wildcard = key => Multiline &&
                    (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow)
            };
            vertical.Action = () => MoveVertical(vertical.KeyInfo.Value.Key == ConsoleKey.UpArrow ? -1 : 1);
            RegisterKeyboardControl(vertical);

            RegisterKeyboardControl(ConsoleKey.Backspace, () => DeleteCluster(true), "Delete previous character");
            RegisterKeyboardControl(ConsoleKey.Delete, () => DeleteCluster(false), "Delete next character");
            RegisterKeyboardControl(ConsoleKey.Tab, () =>
            {
                if (HideCharacters)
                {
                    IsCharactersHidden = !IsCharactersHidden;
                    _preferredColumn = null;
                    NeedsRecalculation = true;
                }
            }, "Toggle hidden characters");

            var typing = new KeyboardControl
            {
                Description = "Typing",
                Wildcard = key => !char.IsControl(key.KeyChar)
            };
            typing.Action = () => InsertText(typing.KeyInfo.Value.KeyChar.ToString());
            RegisterKeyboardControl(typing);
        }

        private void NormalizeCursor()
        {
            CursorPosition = Math.Clamp(CursorPosition, 0, (Text ?? "").Length);
            if (CursorPosition == 0 || CursorPosition == (Text ?? "").Length) return;
            var starts = StringInfo.ParseCombiningCharacters(Text);
            int index = Array.BinarySearch(starts, CursorPosition);
            if (index < 0) CursorPosition = starts[Math.Max(0, ~index - 1)];
        }

        private int AdjacentBoundary(int direction)
        {
            var starts = StringInfo.ParseCombiningCharacters(Text ?? "");
            if (direction > 0)
            {
                foreach (int start in starts) if (start > CursorPosition) return start;
                return (Text ?? "").Length;
            }
            for (int i = starts.Length - 1; i >= 0; i--)
                if (starts[i] < CursorPosition) return starts[i];
            return 0;
        }

        private void MoveHorizontal(int direction)
        {
            NormalizeCursor();
            CursorPosition = AdjacentBoundary(direction);
            _preferredColumn = null;
            NeedsRecalculation = true;
        }

        private void InsertText(string value)
        {
            NormalizeCursor();
            Text = (Text ?? "").Insert(CursorPosition, value);
            CursorPosition += value.Length;
            TextEdited();
        }

        private void DeleteCluster(bool backwards)
        {
            NormalizeCursor();
            int other = AdjacentBoundary(backwards ? -1 : 1);
            int start = Math.Min(CursorPosition, other);
            int length = Math.Abs(CursorPosition - other);
            if (length == 0) return;
            Text = Text.Remove(start, length);
            CursorPosition = start;
            TextEdited();
        }

        private void TextEdited()
        {
            _preferredColumn = null;
            NeedsRecalculation = true;
            OnTextChanged?.Invoke(Text, this);
        }

        private List<(int Start, int End)> GetLines()
        {
            string value = Text ?? "";
            var lines = new List<(int Start, int End)>();
            int start = 0;
            if (Multiline)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] != '\r' && value[i] != '\n') continue;
                    lines.Add((start, i));
                    if (value[i] == '\r' && i + 1 < value.Length && value[i + 1] == '\n') i++;
                    start = i + 1;
                }
            }
            lines.Add((start, value.Length)); // Includes empty text and a trailing empty line.
            return lines;
        }

        private int CursorLine(List<(int Start, int End)> lines)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
                if (CursorPosition >= lines[i].Start) return i;
            return 0;
        }

        private string DisplayCluster(string cluster)
        {
            if (IsCharactersHidden) return HiddenCharacter.ToString();
            // Bound single-line text may contain newlines; never write control codes to the buffer.
            if (cluster.Any(char.IsControl)) return " ";
            return cluster;
        }

        private int ClusterWidth(string cluster) =>
            Math.Max(1, ConsoleBuffer.GetStringWidth(DisplayCluster(cluster)));

        private int VisualColumn(int start, int end)
        {
            string value = (Text ?? "").Substring(start, Math.Max(0, end - start));
            var enumerator = StringInfo.GetTextElementEnumerator(value);
            int column = 0;
            while (enumerator.MoveNext()) column += ClusterWidth(enumerator.GetTextElement());
            return column;
        }

        private void MoveToEdge(bool end)
        {
            NormalizeCursor();
            var lines = GetLines();
            var line = lines[CursorLine(lines)];
            CursorPosition = end ? line.End : line.Start;
            _preferredColumn = null;
            NeedsRecalculation = true;
        }

        private void MoveVertical(int direction)
        {
            NormalizeCursor();
            var lines = GetLines();
            int row = CursorLine(lines);
            int target = Math.Clamp(row + direction, 0, lines.Count - 1);
            if (target == row) return;
            _preferredColumn ??= VisualColumn(lines[row].Start, Math.Min(CursorPosition, lines[row].End));
            var line = lines[target];
            int position = line.Start, column = 0;
            var enumerator = StringInfo.GetTextElementEnumerator((Text ?? "").Substring(line.Start, line.End - line.Start));
            while (enumerator.MoveNext())
            {
                string cluster = enumerator.GetTextElement();
                int width = ClusterWidth(cluster);
                if (column + width > _preferredColumn.Value) break;
                column += width;
                position += cluster.Length;
            }
            CursorPosition = position;
            NeedsRecalculation = true;
        }

        private static int ScrollToCursor(int offset, int cursor, int size, int extent)
        {
            int margin = Math.Min(SCROLL_MARGIN, Math.Max(0, (size - 1) / 2));
            if (cursor < offset + margin) offset = cursor - margin;
            else if (cursor >= offset + size - margin) offset = cursor - size + margin + 1;
            return Math.Clamp(offset, 0, Math.Max(0, extent - size));
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            NormalizeCursor();
            int border = ShowBorder ? 1 : 0;
            int labelY = border;
            int labelWidth = Math.Max(0, buffer.Width - 2);
            if (labelY < buffer.Height && labelWidth > 0)
            {
                buffer.WriteString(1, labelY, new string(' ', labelWidth), Foreground, Background);
                int x = 1;
                var labelElements = StringInfo.GetTextElementEnumerator(Label ?? "");
                while (labelElements.MoveNext())
                {
                    string part = labelElements.GetTextElement();
                    if (part.Any(char.IsControl)) part = " ";
                    int width = Math.Max(1, ConsoleBuffer.GetStringWidth(part));
                    if (x + width > buffer.Width - 1) break;
                    buffer.WriteString(x, labelY, part, Foreground, Background);
                    x += width;
                }
            }
            int top = ShowBorder ? 3 : 1;
            if (ShowBorder && buffer.Width >= 2 && buffer.Height > 3)
                buffer.WriteString(0, 2, "├" + new string(BoxHorizontal, buffer.Width - 2) + "┤",
                    IsFocused ? FocusedBorderColor : BorderColor, IsFocused ? FocusedBackground : Background);

            int visibleWidth = buffer.Width - 2 * border;
            int availableHeight = buffer.Height - top - border;
            if (visibleWidth <= 0 || availableHeight <= 0) return;
            int visibleHeight = Multiline ? availableHeight : 1;
            var lines = GetLines();
            int row = CursorLine(lines);
            int cursorColumn = VisualColumn(lines[row].Start, Math.Min(CursorPosition, lines[row].End));
            int lineWidth = VisualColumn(lines[row].Start, lines[row].End);
            _scrollOffset = ScrollToCursor(_scrollOffset, cursorColumn, visibleWidth, lineWidth + 1);
            _verticalScrollOffset = Multiline
                ? ScrollToCursor(_verticalScrollOffset, row, visibleHeight, lines.Count) : 0;

            // Clear the whole input area so deleted lines and mode changes leave no stale text.
            for (int y = top; y < top + availableHeight; y++)
                buffer.WriteString(border, y, new string(' ', visibleWidth), Foreground, Background);

            for (int screenRow = 0; screenRow < visibleHeight; screenRow++)
            {
                int lineIndex = _verticalScrollOffset + screenRow;
                if (lineIndex >= lines.Count) break;
                var line = lines[lineIndex];
                var elements = StringInfo.GetTextElementEnumerator((Text ?? "").Substring(line.Start, line.End - line.Start));
                int column = 0, position = line.Start;
                bool cursorDrawn = false;
                while (elements.MoveNext())
                {
                    string cluster = elements.GetTextElement();
                    int width = ClusterWidth(cluster);
                    int x = column - _scrollOffset;
                    bool selected = lineIndex == row && position == CursorPosition;
                    // Never draw half of a wide grapheme over a border or outside the viewport.
                    if (x >= 0 && x + width <= visibleWidth)
                    {
                        buffer.WriteString(border + x, top + screenRow, DisplayCluster(cluster),
                            selected ? CursorColorForeground : Foreground,
                            selected ? CursorColorBackground : Background);
                        cursorDrawn |= selected;
                    }
                    column += width;
                    position += cluster.Length;
                    if (column >= _scrollOffset + visibleWidth) break;
                }
                if (lineIndex == row && !cursorDrawn)
                {
                    int x = cursorColumn - _scrollOffset;
                    if (x >= 0 && x < visibleWidth)
                        buffer.SetPixel(border + x, top + screenRow, ' ', CursorColorForeground, CursorColorBackground);
                }
            }
        }

        public void Clear()
        {
            Text = "";
            CursorPosition = 0;
            _scrollOffset = _verticalScrollOffset = 0;
            TextEdited();
        }
    }
}
