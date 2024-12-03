using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public struct ConsolePixel
    {
        public char Character;
        public Color Foreground;
        public Color Background;
        public bool IsWide;

        public ConsolePixel(char character, Color foreground, Color background, bool isWide)
        {
            Character = character;
            Foreground = foreground;
            Background = background;
            IsWide = isWide;
        }
    }
}
