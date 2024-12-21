using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using MelonUI.Base;
using static System.Net.Mime.MediaTypeNames;

namespace MelonUI.Default
{
    public class MenuItem : UIElement
    {
        private object _Option;
        public string Option
        {
            get => (string)GetBoundValue(nameof(Option), _Option);
            set => SetBoundValue(nameof(Option), value, ref _Option);
        }
        public Action OnSelect { get; set; }
        public MenuItem()
        {

        }
        public MenuItem(string option, Action onSelect)
        {
            Option = option;
            OnSelect = onSelect;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            // Dont
        }
    }
    public class OptionsMenu : UIElement
    {
        private object _Options;
        public List<MenuItem> Options
        {
            get => (List<MenuItem>)GetBoundValue(nameof(Options), _Options);
            set => SetBoundValue(nameof(Options), value, ref _Options);
        }
        public string MenuName { get; set; } = "";
        public bool UseStatusBar { get; set; }
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
                    {
                        _currentIndex--;
                    }
                    else
                    {
                        _currentIndex = Options.Count - 1;
                    }
                    NeedsRecalculation = true;
                },
                $"Scroll Up"
            );
            RegisterKeyboardControl(
                ConsoleKey.DownArrow,
                () => {
                    if (_currentIndex < Options.Count - 1)
                    {
                        _currentIndex++;
                    }
                    else
                    {
                        _currentIndex = 0;
                    }
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
                if(item == null)
                {
                    return;
                }
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
            if (!IsVisible)
            {
                return;
            }
            int totalItems = Options.Count;
            int bump = ShowBorder ? 1 : 0;
            int displayableItems = ShowBorder ? buffer.Height - 4 : buffer.Height - 2;

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
