using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class TextBox : UIElement
    {
        public string Text { get; set; } = "";
        public int CursorPosition { get; private set; } = 0;
        public ConsoleColor TextColor { get; set; } = ConsoleColor.White;
        public ConsoleColor CursorColor { get; set; } = ConsoleColor.Gray;
        public event Action<string> OnTextChanged;

        private const char BoxTopLeft = '┌';
        private const char BoxTopRight = '┐';
        private const char BoxBottomLeft = '└';
        private const char BoxBottomRight = '┘';
        private const char BoxHorizontal = '─';
        private const char BoxVertical = '│';
        public TextBox()
        {
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
                        OnTextChanged?.Invoke(Text);
                    }
                },
                "Delete character"
            );
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            //var buffer = new ConsoleBuffer(ActualWidth, ActualHeight);
            //var background = IsFocused ? FocusedBackground : DefaultBackground;
            //buffer.Clear(background);

            // Draw border
            //ConsoleColor borderColor = IsFocused ? ConsoleColor.White : ConsoleColor.Gray;

            

            // Draw text
            for (int i = 0; i < Text.Length && i < ActualWidth - 2; i++)
            {
                buffer.SetPixel(i + 1, 1, Text[i], TextColor, Background);
            }

            // Draw cursor if focused
            if (IsFocused && CursorPosition < ActualWidth - 2)
            {
                if(Text.Length <= CursorPosition)
                {
                    buffer.SetPixel(CursorPosition + 1, 1, ' ', TextColor, CursorColor);

                }
                else
                {
                    buffer.SetPixel(CursorPosition + 1, 1, Text[CursorPosition], TextColor, CursorColor);

                }
            }

            //return buffer;
        }

        public override void HandleKey(ConsoleKeyInfo keyInfo)
        {
            if (!IsFocused) return;

            switch (keyInfo.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (CursorPosition > 0) CursorPosition--;
                    break;

                case ConsoleKey.RightArrow:
                    if (CursorPosition < Text.Length) CursorPosition++;
                    break;

                case ConsoleKey.Backspace:
                    if (CursorPosition > 0)
                    {
                        Text = Text.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                        OnTextChanged?.Invoke(Text);
                    }
                    break;

                default:
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        Text = Text.Insert(CursorPosition, keyInfo.KeyChar.ToString());
                        CursorPosition++;
                        OnTextChanged?.Invoke(Text);
                    }
                    break;
            }
        }
    }
}
