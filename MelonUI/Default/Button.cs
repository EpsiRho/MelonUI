using MelonUI.Base;
using System;

namespace MelonUI.Default
{
    public class Button : UIElement
    {
        public string Text { get; set; } = "";
        public ConsoleColor TextColor { get; set; } = ConsoleColor.White;
        public event Action OnPressed;
        private readonly ConsoleKey _key;

        public Button(ConsoleKey key)
        {
            _key = key;
            ShowBorder = true;

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
            string keyText = $"[{GetKeyDisplay(_key)}]";
            int keyStart = (contentWidth - keyText.Length) / 2;

            for (int x = 0; x < contentWidth; x++)
            {
                if (x >= keyStart && x < keyStart + keyText.Length)
                {
                    // Draw the key display
                    buffer.SetPixel(x, 0, keyText[x - keyStart], foreground, background);
                }
            }

            // Draw centered button text
            int textStart = Math.Max(1, (contentWidth - Text.Length) / 2);
            for (int i = 0; i < Text.Length && textStart + i < contentWidth - 2; i++)
            {
                buffer.SetPixel(textStart + i, contentHeight / 2, Text[i], TextColor, background);
            }
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
    }
}