using MelonUI.Base;
using Pastel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MelonUI.Helpers
{
    public static class ParamParser
    {
        public static Color GetGradientColor(Color[] colors, double step)
        {
            if (colors == null || colors.Length < 2)
                throw new ArgumentException("At least two colors are required to create a gradient.");

            if (step < 0.0 || step > 1.0)
                throw new ArgumentOutOfRangeException(nameof(step), "Step must be between 0.0 and 1.0.");

            // Determine the range of colors to interpolate between
            int segmentCount = colors.Length - 1;
            double scaledStep = step * segmentCount;
            int lowerIndex = (int)Math.Floor(scaledStep);
            int upperIndex = Math.Min(lowerIndex + 1, colors.Length - 1);

            double localStep = scaledStep - lowerIndex; // Fractional part for interpolation

            // Interpolate between the two colors
            Color lowerColor = colors[lowerIndex];
            Color upperColor = colors[upperIndex];

            int r = (int)(lowerColor.R + (upperColor.R - lowerColor.R) * localStep);
            int g = (int)(lowerColor.G + (upperColor.G - lowerColor.G) * localStep);
            int b = (int)(lowerColor.B + (upperColor.B - lowerColor.B) * localStep);

            return Color.FromArgb(r, g, b);
        }
        public static int GetVisibleLength(string text)
        {
            int length = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
                {
                    i = text.IndexOf('m', i);
                    if (i == -1) break;
                    continue;
                }
                length++;
            }
            return length;
        }
        public static string GetGradientString(string input, Color[] colors)
        {
            string output = "";

            for(int i = 0; i < input.Length; i++)
            {
                Color curColor = GetGradientColor(colors, (double)i / input.Length);
                output += $"{input[i]}".Pastel(curColor);
            }
            return output;
        }
    }
}
