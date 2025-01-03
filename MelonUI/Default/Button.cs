using MelonUI.Attributes;
using MelonUI.Base;
using System;

namespace MelonUI.Default
{
    public partial class Button : UIElement
    {
        [Binding]
        public string text = "";
        [Binding]
        public Action onPressed;
        public readonly ConsoleKey _key;

        public Button(ConsoleKey key)
        {
            _key = key;
            ShowBorder = false;

            RegisterKeyboardControl(
                key,
                () => OnPressed?.Invoke(),
                $"Activate {Text}"
            );
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var foreground = IsFocused ? FocusedBorderColor : BorderColor;
            var background = IsFocused ? FocusedBackground : Background;

            // Calculate content area
            int contentWidth = buffer.Width;
            int contentHeight = buffer.Height;

            // Draw top border with key
            string keyText = $"[({GetKeyDisplay(_key)}) {Text}]";

            // Draw centered button text
            int textStart = Math.Max(1, (contentWidth - keyText.Length) / 2);
            for (int i = 0; i < keyText.Length && textStart + i < contentWidth - 2; i++)
            {
                buffer.SetPixel(textStart + i, contentHeight / 2, keyText[i], foreground, background);
            }
        }

        public static string GetKeyDisplay(ConsoleKey key)
        {
            return key switch
            {
                ConsoleKey.Spacebar => "_",
                ConsoleKey.Enter => "Etr",
                ConsoleKey.Escape => "Esc",
                ConsoleKey.Delete => "Del",
                ConsoleKey.Backspace => "Bck",
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
    }
}