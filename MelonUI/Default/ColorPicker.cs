using MelonUI.Base;
using Pastel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class ColorPicker : UIElement
    {
        private int index;
        private int accel = 1;

        private Color _ColorValue = Color.Cyan;
        public Color ColorValue
        {
            get
            {
                return (Color)GetBoundValue(nameof(ColorValue), _ColorValue);
            }
            set
            {
                SetBoundValue(nameof(ColorValue), value, ref _ColorValue);
            }
        }
        private Stopwatch accelCounter = Stopwatch.StartNew();
        private Stopwatch timer = Stopwatch.StartNew();

        public ColorPicker()
        {
            RegisterKeyboardControl(ConsoleKey.LeftArrow, () => 
            {
                if (timer.ElapsedMilliseconds < 100)
                {
                    if (accelCounter.ElapsedMilliseconds > 500)
                    {
                        accel+=3;
                        accelCounter.Restart();
                    }
                }
                else
                {
                    accel = 0;
                }
                timer.Restart();
                switch (index)
                {
                    case 0:
                        int R = ColorValue.R - (1 + accel) >= 0 ? ColorValue.R - (1 + accel) : 0;
                        ColorValue = Color.FromArgb(255, R, ColorValue.G, ColorValue.B);
                        break;
                    case 1:
                        int G = ColorValue.G - (1 + accel) >= 0 ? ColorValue.G - (1 + accel) : 0;
                        ColorValue = Color.FromArgb(255, ColorValue.R, G, ColorValue.B);
                        break;
                    case 2:
                        int B = ColorValue.B - (1 + accel) >= 0 ? ColorValue.B - (1 + accel) : 0;
                        ColorValue = Color.FromArgb(255, ColorValue.R, ColorValue.G, B);
                        break;

                }
            }, "Reduce Color Value");
            RegisterKeyboardControl(ConsoleKey.RightArrow, () =>
            {
                if (timer.ElapsedMilliseconds < 100)
                {
                    if(accelCounter.ElapsedMilliseconds > 500)
                    {
                        accel+=3;
                        accelCounter.Restart();
                    }
                }
                else
                {
                    accel = 0;
                }
                timer.Restart();
                switch (index)
                {
                    case 0:
                        int R = ColorValue.R + (1 + accel) >= 0 ? ColorValue.R + (1 + accel) : 0;
                        ColorValue = Color.FromArgb(255, R, ColorValue.G, ColorValue.B);
                        break;
                    case 1:
                        int G = ColorValue.G + (1 + accel) >= 0 ? ColorValue.G + (1 + accel) : 0;
                        ColorValue = Color.FromArgb(255, ColorValue.R, G, ColorValue.B);
                        break;
                    case 2:
                        int B = ColorValue.B + (1 + accel) >= 0 ? ColorValue.B + (1 + accel) : 0;
                        ColorValue = Color.FromArgb(255, ColorValue.R, ColorValue.G, B);
                        break;

                }
            }, "Increase Color Value");
            RegisterKeyboardControl(ConsoleKey.DownArrow, () =>
            {
                switch (index)
                {
                    case 0:
                        index = 1;
                        break;
                    case 1:
                        index = 2;
                        break;
                    case 2:
                        index = 0;
                        break;

                }
            }, "Increase Color Value");
            RegisterKeyboardControl(ConsoleKey.UpArrow, () =>
            {
                switch (index)
                {
                    case 0:
                        index = 2;
                        break;
                    case 1:
                        index = 0;
                        break;
                    case 2:
                        index = 1;
                        break;

                }
            }, "Increase Color Value");
        }
        Color CombineColors(Color color1, Color color2)
        {
            // Average the RGB values of the two colors
            int r = (color1.R + color2.R) / 2;
            int g = (color1.G + color2.G) / 2;
            int b = (color1.B + color2.B) / 2;

            return Color.FromArgb(255, r, g, b);
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            // [#################------░▶
            // [#######################]
            // [#######----------------]
            var renderClr = ColorValue;
            var renderR = Color.FromArgb(255, 255, 255 - renderClr.R, 255 - renderClr.R);
            var renderG = Color.FromArgb(255, 255 - renderClr.G, 255, 255 - renderClr.G);
            var renderB = Color.FromArgb(255, 255 - renderClr.B, 255 - renderClr.B, 255);

            int width = ActualWidth - (ShowBorder ? 10 : 8);
            int height = ActualHeight - (ShowBorder ? 2 : 0);

            int x = ShowBorder ? 1 : 0;
            int y = ShowBorder ? 1 : 0;

            double RedPercent = (double)ColorValue.R / (double)256;
            double GreenPercent = (double)ColorValue.G / (double)256;
            double BluePercent = (double)ColorValue.B / (double)256;
            RedPercent = RedPercent == 0 ? 0.001 : RedPercent;
            GreenPercent = GreenPercent == 0 ? 0.001 : GreenPercent;
            BluePercent = BluePercent == 0 ? 0.001 : BluePercent;

            double RedLineFront = (width) * RedPercent;
            double RedLineBack = (width) - RedLineFront;

            double GreenLineFront = (width) * GreenPercent;
            double GreenLineBack = (width) - GreenLineFront;

            double BlueLineFront = (width) * BluePercent;
            double BlueLineBack = (width) - BlueLineFront;


            int sTop = Console.CursorTop;
            //Console.WriteLine($"{$"Old Color -".Pastel(CurColor)}{$"> {StateManager.StringsManager.GetString("NewColorDisplay")}".Pastel(NewColor)}");

            // Render the R,G, and B lines
            string RedBar = $"{new string('░', (int)RedLineFront)}█{new string('░', (int)RedLineBack)}";
            string GreenBar = $"{new string('░', (int)GreenLineFront)}█{new string('░', (int)GreenLineBack)}";
            string BlueBar = $"{new string('░', (int)BlueLineFront)}█{new string('░', (int)BlueLineBack)}";

            string rStr = $"┌{RedBar}";
            string gStr = $"├{GreenBar}";
            string bStr = $"├{BlueBar}";
            string nStr = $"└      Output Color";

            buffer.WriteString(x,y, rStr, renderR, Background);
            buffer.WriteString(x,y+1, gStr, renderG, Background);
            buffer.WriteString(x,y+2, bStr, renderB, Background);
            buffer.WriteString(x,y+3, "│", renderClr, Background);
            buffer.WriteString(x,y+4, nStr, Foreground, Background);
            buffer.WriteString(x,y+4, "└█████", renderClr, Color.FromArgb(255, 10,10,10));

            buffer.SetPixel(x, y, '┌', Color.FromArgb(255, ColorValue.R, 0, 0), Background);
            var combinedColor1 = CombineColors(Color.FromArgb(255, ColorValue.R, 0, 0), Color.FromArgb(255, 0, ColorValue.G, 0));
            var combinedColor2 = CombineColors(combinedColor1, Color.FromArgb(255, 0, 0, ColorValue.B));
            buffer.SetPixel(x, y+1, '├', Color.FromArgb(255, ColorValue.R, ColorValue.G, 0), Background);
            buffer.SetPixel(x, y+2, '├', Color.FromArgb(255, ColorValue.R, ColorValue.G, ColorValue.B), Background);

            string rStr2 = $"{renderClr.R.ToString("000")}";
            string gStr2 = $"{renderClr.G.ToString("000")}";
            string bStr2 = $"{renderClr.B.ToString("000")}";

            Color fg = Foreground;
            Color bg = Color.FromArgb(255, 120, 120, 120);
            Color sbg = Color.FromArgb(255, 200, 200, 200);

            buffer.WriteString(width + 5,y, rStr2, index == 0 ? fg : bg, Background);
            buffer.WriteString(width + 5,y+1, gStr2, index == 1 ? fg : bg, Background);
            buffer.WriteString(width + 5, y+2, bStr2, index == 2 ? fg : bg, Background);

            buffer.SetPixel((int)RedLineFront + 3, y, '█', index == 0 ? fg : sbg, Background);
            buffer.SetPixel((int)GreenLineFront + 3, y + 1, '█', index == 1 ? fg : sbg, Background);
            buffer.SetPixel((int)BlueLineFront + 3, y + 2, '█', index == 2 ? fg : sbg, Background);

            //for (int i = 0; i < width - 2; i++)
            //{
            //    buffer.SetPixel(i + 1, 1, '█', renderR, Color.Black);
            //    buffer.SetPixel(i + 1, 2, '█', renderG, Color.Black);
            //    buffer.SetPixel(i + 1, 3, '█', renderB, Color.Black);
            //}

            //buffer.SetPixel(width - 1, 1, '', renderR, Color.Black);
            //buffer.SetPixel(width - 1, 2, '', renderG, Color.Black);
            //buffer.SetPixel(width - 1, 3, '', renderB, Color.Black);
            //
            //buffer.SetPixel(width, 1, '┐', renderR, Color.Black);
            //buffer.SetPixel(width, 2, '┤', renderG, Color.Black);
            //buffer.SetPixel(width, 3, '┤', renderB, Color.Black);
        }
    }
}
