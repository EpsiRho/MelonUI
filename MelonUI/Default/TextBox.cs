using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class TextBox : UIElement
    {
        public object _Text = "";
        public string Text
        {
            get => (string)GetBoundValue(nameof(Text), _Text);
            set => SetBoundValue(nameof(Text), value, ref _Text);
        }
        public object _Label = "";
        public string Label
        {
            get => (string)GetBoundValue(nameof(Label), _Label);
            set => SetBoundValue(nameof(Label), value, ref _Label);
        }
        public int CursorPosition { get; private set; } = 0;
        public object _HiddenCharacter = '*';
        public char HiddenCharacter
        {
            get => (char)GetBoundValue(nameof(HiddenCharacter), _HiddenCharacter);
            set => SetBoundValue(nameof(HiddenCharacter), value, ref _HiddenCharacter);
        }
        private object _HideCharacters;
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
        private bool IsCharactersHidden { get; set; } = false;
        public Color CursorColorForeground { get; set; } = Color.Cyan;
        public Color CursorColorBackground { get; set; } = Color.Gray;
        public object _OnTextChanged;
        public Action<string, TextBox> OnTextChanged
        {
            get => (Action<string, TextBox>)GetBoundValue(nameof(OnTextChanged), _OnTextChanged);
            set => SetBoundValue(nameof(OnTextChanged), value, ref _OnTextChanged);
        }
        public object _OnEnter;
        public Action<string, TextBox> OnEnter
        {
            get => (Action<string, TextBox>)GetBoundValue(nameof(OnEnter), _OnEnter);
            set => SetBoundValue(nameof(OnEnter), value, ref _OnEnter);
        }
        public Action EnterHit { get; set; }
        private int _scrollOffset = 0;
        private const int SCROLL_MARGIN = 2;

        public TextBox()
        {
            RegisterKeyboardControl(
                ConsoleKey.Enter,
                () => { OnEnter?.Invoke(Text, this); EnterHit?.Invoke(); },
                "Enter"
            );

            RegisterKeyboardControl(
                ConsoleKey.LeftArrow,
                () => { if (CursorPosition > 0) CursorPosition--; },
                "Move cursor left"
            );

            RegisterKeyboardControl(
                ConsoleKey.RightArrow,
                () => { if (CursorPosition < Text.Length) CursorPosition++; },
                "Move cursor right"
            );

            RegisterKeyboardControl(
                ConsoleKey.Backspace,
                () => {
                    if (CursorPosition > 0)
                    {
                        Text = Text.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                        OnTextChanged?.Invoke(Text, this);
                    }
                },
                "Delete character back"
            );

            RegisterKeyboardControl(
                ConsoleKey.Delete,
                () => {
                    if (CursorPosition > 0 && CursorPosition < Text.Length)
                    {
                        Text = Text.Remove(CursorPosition, 1);
                        OnTextChanged?.Invoke(Text, this);
                    }
                },
                "Delete character current"
            );

            RegisterKeyboardControl(
                ConsoleKey.Tab,
                () => {
                    if (HideCharacters)
                    {
                        IsCharactersHidden = !IsCharactersHidden;
                    }
                },
                "Delete character current"
            );

            var ctl = new KeyboardControl()
            {
                Description = "Typing",
                Wildcard = (keyInfo) =>
                {
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        return true;
                    }
                    return false;
                },
            };
            ctl.Action = () =>
            {
                Text = Text.Insert(CursorPosition, ctl.KeyInfo.Value.KeyChar.ToString());
                CursorPosition++;
                OnTextChanged?.Invoke(Text, this);
                NeedsRecalculation = true;
            };
            RegisterKeyboardControl(ctl);
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            int bump = ShowBorder ? 1 : 0;
            int bumpX = ShowBorder ? 1 : 0;
            int visibleWidth = ActualWidth - 4;

            // Calculate the scroll position
            UpdateScrollOffset(visibleWidth);

            if (ShowBorder)
            {
                var borderForeground = IsFocused ? FocusedBorderColor : BorderColor;
                var borderBackground = IsFocused ? FocusedBackground : Background;

                buffer.WriteString(1, bump, Label.PadRight(buffer.Width - 2).Substring(0, buffer.Width - 2), Foreground, Background);
                bump++;
                buffer.WriteString(0, bump, $"{"├".PadRight(buffer.Width - 1, BoxHorizontal).Substring(0, buffer.Width - 1)}┤", borderForeground, borderBackground);
                bump++;
            }
            else
            {
                buffer.WriteString(1, bump, Label.PadRight(buffer.Width - 2).Substring(0, buffer.Width - 2), Foreground, Background);
                bump++;
            }

            // Draw text
            bool cursorDrawn = false;
            int max = bumpX;

            for (int i = 0; i < visibleWidth && (_scrollOffset + i) < Text.Length; i++)
            {
                var entry = !IsCharactersHidden ? Text[_scrollOffset + i] : HiddenCharacter;
                bool isSelected = (_scrollOffset + i) == CursorPosition;
                if (isSelected)
                {
                    cursorDrawn = true;
                }

                // Highlight the selected char
                Color foreground = isSelected ? CursorColorForeground : Foreground;
                Color background = isSelected ? CursorColorBackground : Background;

                max = i + bumpX;
                buffer.SetPixel(max, bump, entry, foreground, background);
            }

            // Draw cursor at end if needed
            if (!cursorDrawn && CursorPosition >= _scrollOffset)
            {
                max = Math.Min(CursorPosition - _scrollOffset + bumpX, visibleWidth + bumpX);
                buffer.SetPixel(max, bump, ' ', CursorColorForeground, CursorColorBackground);
            }
        }

        private void UpdateScrollOffset(int visibleWidth)
        {
            // If cursor moves too far right
            if (CursorPosition >= _scrollOffset + visibleWidth - SCROLL_MARGIN)
            {
                _scrollOffset = Math.Min(
                    CursorPosition - visibleWidth + SCROLL_MARGIN + 1,
                    Math.Max(0, Text.Length - visibleWidth)
                );
            }
            // If cursor moves too far left
            else if (CursorPosition < _scrollOffset + SCROLL_MARGIN)
            {
                _scrollOffset = Math.Max(0, CursorPosition - SCROLL_MARGIN);
            }
        }
        public void Clear()
        {
            Text = "";
            CursorPosition = 0;
        }

    }
}