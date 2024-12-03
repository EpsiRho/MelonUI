using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using MelonUI.Base;

namespace MelonUI.Components
{
    public class FilePicker : UIElement
    {
        private DirectoryInfo _currentDirectory;
        private List<FileSystemInfo> _entries;
        private int _currentIndex;
        private int _scrollOffset;
        public Action OnDirectorySelected;
        public Action OnFileSelected;

        public string Path { get; private set; }

        public FilePicker(string dir = null)
        {
            if (String.IsNullOrEmpty(dir))
            {
                _currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

            }
            else
            {
                Directory.SetCurrentDirectory(dir);
                _currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            }
            _entries = _currentDirectory.GetFileSystemInfos().ToList();
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
                    if (_currentIndex < _entries.Count - 1)
                        _currentIndex++;
                    NeedsRecalculation = true;
                },
                $"Scroll Down"
            );
            RegisterKeyboardControl(
                ConsoleKey.LeftArrow,
                () => {
                    NavigateToParentDirectory();
                    NeedsRecalculation = true;
                },
                $"Go Back"
            );
            RegisterKeyboardControl(
                ConsoleKey.RightArrow,
                () => {
                    NavigateToSelectedEntry();
                    NeedsRecalculation = true;
                },
                $"Select"
            );
            RegisterKeyboardControl(
                ConsoleKey.Enter,
                () => {
                    NavigateToSelectedEntry();
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
                var item = _entries.FirstOrDefault(x => x.Name.ToUpper().StartsWith(GetKeyDisplay(ctl.Key.Value)));
                var curItem = _entries[_currentIndex];
                if (item != null)
                {
                    var idx = _entries.IndexOf(item);
                    if (idx <= _currentIndex)
                    {
                        if (_currentIndex < _entries.Count - 1 && curItem.Name.First() == item.Name.First())
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
            if (!IsVisible) return;

            if(this.ParentWindow.FocusedElement.Equals( this.Parent ))
            {
                this.ParentWindow.Status = $"[{_currentDirectory.FullName}]";
            }

            int displayableItems = buffer.Height - 2; // Adjust for border if necessary
            int totalItems = _entries.Count;

            // Ensure scrolling is within bounds
            if (_currentIndex < _scrollOffset)
                _scrollOffset = _currentIndex;
            else if (_currentIndex >= _scrollOffset + displayableItems)
                _scrollOffset = _currentIndex - displayableItems + 1;

            for (int i = 0; i < displayableItems && i + _scrollOffset < totalItems; i++)
            {
                FileSystemInfo entry = _entries[i + _scrollOffset];
                bool isSelected = (i + _scrollOffset) == _currentIndex;
                string entryName = entry.Name;

                // Highlight the selected entry
                Color foreground = isSelected ? Color.Black : Foreground;
                Color background = isSelected ? Color.Cyan : Background;

                buffer.WriteString(1, i + 1, entryName.PadRight(buffer.Width - 2).Substring(0, buffer.Width - 2), foreground, background);
            }
            NeedsRecalculation = false;
        }

        private void NavigateToParentDirectory()
        {
            if (_currentDirectory.Parent != null)
            {
                _currentDirectory = _currentDirectory.Parent;
                _entries = _currentDirectory.GetFileSystemInfos().ToList();
                _currentIndex = 0;
                _scrollOffset = 0;
            }
        }

        private void NavigateToSelectedEntry()
        {
            FileSystemInfo selectedEntry = _entries[_currentIndex];
            ParentWindow.SetStatus($"[{selectedEntry.FullName}]");
            if (selectedEntry is DirectoryInfo directory)
            {
                _currentDirectory = directory;
                _entries = _currentDirectory.GetFileSystemInfos().ToList();
                _currentIndex = 0;
                _scrollOffset = 0;
                if (OnDirectorySelected != null)
                {
                    OnDirectorySelected();
                }
            }
            else if (selectedEntry is FileInfo file)
            {
                Path = file.FullName;
                if(OnFileSelected != null)
                {
                    OnFileSelected();
                }
            }
        }
    }
}
