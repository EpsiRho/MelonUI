using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using MelonUI.Base;

namespace MelonUI.Components
{
    public class OptionsMenu : UIElement
    {
        public List<(string Option, Action OnSelect)> Options;
        public string MenuName = "";
        public bool UseStatusBar;
        private int _currentIndex;
        private int _scrollOffset;
        public Color SelectedForeground { get; set; } = Color.Cyan;
        public Color SelectedBackground { get; set; } = Color.FromArgb(255, 80, 80, 80);

        public OptionsMenu()
        {
            Options = new();
            _currentIndex = 0;
            _scrollOffset = 0;
            IsVisible = true;

            RegisterKeyboardControl(
                ConsoleKey.UpArrow,
                () => {
                    if (_currentIndex > 0)
                        _currentIndex--;
                    NeedsRecalculation = true;
                },
                $"Scroll Up"
            );
            RegisterKeyboardControl(
                ConsoleKey.DownArrow,
                () => {
                    if (_currentIndex < Options.Count - 1)
                        _currentIndex++;
                    NeedsRecalculation = true;
                },
                $"Scroll Down"
            );
            RegisterKeyboardControl(
                ConsoleKey.RightArrow,
                () => {
                    OnSelect();
                    NeedsRecalculation = true;
                },
                $"Select"
            );
            RegisterKeyboardControl(
                ConsoleKey.Enter,
                () => {
                    OnSelect();
                    NeedsRecalculation = true;
                },
                $"Select"
            );
            var ctl = new KeyboardControl()
            {
                Description = "Jump to section",
                Wildcard = (keyInfo) =>
                {
                    if (char.IsLetterOrDigit(keyInfo.KeyChar))
                    {
                        return true;
                    }
                    return false;
                },
            };
            ctl.Action = () =>
            {
                var item = Options.FirstOrDefault(x => x.Option.ToUpper().StartsWith(GetKeyDisplay(ctl.Key.Value)));
                var curItem = Options[_currentIndex];
                if (item.Option != null)
                {
                    var idx = Options.IndexOf(item);
                    if (idx <= _currentIndex)
                    {
                        if (_currentIndex < Options.Count - 1 && curItem.Option.First() == item.Option.First())
                        {
                            _currentIndex++;
                            return;
                        }
                    }

                    _currentIndex = idx;
                }
                NeedsRecalculation = true;
            };
            RegisterKeyboardControl(ctl);
        }
        private string GetKeyDisplay(ConsoleKey key)
        {
            return key switch
            {
                ConsoleKey.Spacebar => "Space",
                ConsoleKey.Enter => "Enter",
                ConsoleKey.Escape => "Esc",
                ConsoleKey.Delete => "Del",
                ConsoleKey.Backspace => "Back",
                ConsoleKey.Tab => "Tab",
                ConsoleKey.UpArrow => "↑",
                ConsoleKey.DownArrow => "↓",
                ConsoleKey.LeftArrow => "←",
                ConsoleKey.RightArrow => "→",
                ConsoleKey.F1 => "F1",
                ConsoleKey.F2 => "F2",
                ConsoleKey.F3 => "F3",
                ConsoleKey.F4 => "F4",
                ConsoleKey.F5 => "F5",
                ConsoleKey.F6 => "F6",
                ConsoleKey.F7 => "F7",
                ConsoleKey.F8 => "F8",
                ConsoleKey.F9 => "F9",
                ConsoleKey.F10 => "F10",
                ConsoleKey.F11 => "F11",
                ConsoleKey.F12 => "F12",
                _ => key.ToString().Last().ToString()
            };
        }

        public void Show()
        {
            IsVisible = true;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            int displayableItems = buffer.Height - 2;
            int totalItems = Options.Count;
            int bump = ShowBorder ? 1 : 0;

            // Ensure scrolling is within bounds
            if (_currentIndex < _scrollOffset)
                _scrollOffset = _currentIndex;
            else if (_currentIndex >= _scrollOffset + displayableItems)
                _scrollOffset = _currentIndex - displayableItems + 1;

            if (!UseStatusBar && !string.IsNullOrEmpty(MenuName))
            {
                if (ShowBorder)
                {
                    var borderForeground = IsFocused ? FocusedBorderColor : BorderColor;
                    var borderBackground = IsFocused ? FocusedBackground : Background;

                    buffer.WriteString(1, bump, MenuName.PadRight(buffer.Width - 2).Substring(0, buffer.Width - 2), Foreground, Background);
                    bump++;
                    buffer.WriteString(0, bump, $"{"├".PadRight(buffer.Width - 1, BoxHorizontal).Substring(0, buffer.Width - 1)}┤", borderForeground, borderBackground);
                    bump++;
                }
                else
                {
                    buffer.WriteString(1, bump, MenuName.PadRight(buffer.Width - 2).Substring(0, buffer.Width - 2), Foreground, Background);
                    bump++;
                }
            }
            else if (UseStatusBar && !string.IsNullOrEmpty(MenuName))
            {
                ParentWindow.SetStatus(MenuName);
            }
            for (int i = 0; i < displayableItems && i + _scrollOffset < totalItems; i++)
            {
                var entry = Options[i + _scrollOffset];
                bool isSelected = (i + _scrollOffset) == _currentIndex;
                string entryName = entry.Option;

                // Highlight the selected entry
                Color foreground = isSelected ? SelectedForeground : Foreground;
                Color background = isSelected ? SelectedBackground : Background;

                buffer.WriteString(1, i + bump, entryName.PadRight(buffer.Width - 2).Substring(0, buffer.Width - 2), foreground, background);
            }
            NeedsRecalculation = false;
        }

        private void OnSelect()
        {
            var selectedEntry = Options[_currentIndex];

            if(selectedEntry.OnSelect != null)
            {
                selectedEntry.OnSelect();
            }
        }
    }
}
