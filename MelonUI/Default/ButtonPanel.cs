using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace MelonUI.Default
{
    public class ButtonPanel : UIElement
    {
        public List<Button> Buttons { get; set; } = new List<Button>();
        private Button overflowButton;
        private bool isOverflowMenuOpen;
        private List<Button> overflowButtons;
        public int MaxLines = 1;
        public int MaxOverflowMenuLines = 5;
        public override bool DefaultKeyControl { get; set; } = false;

        public ButtonPanel()
        {
            ShowBorder = false;
            overflowButton = new Button(ConsoleKey.Oem3) // '`' key
            {
                Text = "•••",
            };
            overflowButton.OnPressed += ToggleOverflowMenu;
            overflowButtons = new List<Button>();
        }

        public void AddButton(Button element)
        {
            element.Parent = this;
            element.ParentWindow = this.ParentWindow;

            Buttons.Add(element);

        }
        public void RemoveButton(Button element)
        {
            Buttons.Remove(element);

        }

        private void ToggleOverflowMenu()
        {
            isOverflowMenuOpen = !isOverflowMenuOpen;
            NeedsRecalculation = true;
        }

        private string overflowButtonText = $"[(`) •••]";

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            int availableWidth = ActualWidth - 2 - overflowButtonText.Length; // Adjust for borders
            int x = 1; // Start after the left border
            int y = 1;
            Height = $"{MaxLines}";

            overflowButtons.Clear();

            foreach (var button in Buttons)
            {
                string buttonText = $"[({Button.GetKeyDisplay(button._key)}) {button.Text}]";
                if (x + buttonText.Length < availableWidth)
                {
                    RenderButton(buffer, button, x, y, buttonText);
                    x += buttonText.Length + 1;
                }
                else if(MaxLines > y)
                {
                    x = 1;
                    y++;
                    if (x + buttonText.Length < availableWidth)
                    {
                        RenderButton(buffer, button, x, y, buttonText);
                        x += buttonText.Length + 1;
                    }
                }
                else
                {
                    overflowButtons.Add(button);
                }
            }

            // Render overflow button if necessary
            if (overflowButtons.Any())
            {
                RenderButton(buffer, overflowButton, x, MaxLines, overflowButtonText);
            }

            // Render overflow menu if open
            if (isOverflowMenuOpen && overflowButtons.Any())
            {
                //RenderOverflowMenu(buffer);
                var overflowMenu = new OverflowMenu(overflowButtons);
                if (Parent != null)
                {
                    Parent.Children.Add(overflowMenu);
                }
                else
                {
                    ParentWindow.AddElement(overflowMenu);
                }
            }
        }

        public override void HandleKey(ConsoleKeyInfo keyInfo)
        {
            List<Button> buttons = new();
            buttons.AddRange(Buttons);
            buttons.Add(overflowButton);
            foreach (var element in buttons)
            {
                var keyControls = element.GetKeyboardControls().Where(x => x.Key == keyInfo.Key).ToList();
                keyControls.AddRange(element.GetKeyboardControls().Where(x => x.Key == ConsoleKey.None));
                if (keyControls != null)
                {
                    foreach (var control in keyControls)
                    {
                        if (control.Matches(keyInfo))
                        {
                            control.Action();
                        }
                    }
                }
            }

        }

        private void RenderButton(ConsoleBuffer buffer, Button button, int x, int y, string text)
        {
            var foreground = IsFocused ? button.FocusedForeground : button.Foreground;
            var background = IsFocused ? button.FocusedBackground : button.Background;
            string buttonText = text;

            for (int i = 0; i < buttonText.Length; i++)
            {
                buffer.SetPixel(x + i, y, buttonText[i], foreground, background);
            }
        }

    }
    public class OverflowMenu : UIElement
    {
        private List<Button> buttons;
        public int MaxOverflowMenuLines = 5;

        public OverflowMenu(List<Button> buttons)
        {
            this.buttons = buttons;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            int x = ActualX;
            int y = ActualY;
            int maxLines = Math.Min(buttons.Count, MaxOverflowMenuLines);

            for (int i = 0; i < maxLines; i++)
            {
                var button = buttons[i];
                string buttonText = $"[({button.Text[0]}) {button.Text}]";
                for (int j = 0; j < buttonText.Length; j++)
                {
                    buffer.SetPixel(x + j, y + i, buttonText[j], Foreground, Background);
                }
            }
        }
    }
}
