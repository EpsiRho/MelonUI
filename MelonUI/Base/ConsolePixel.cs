using Pastel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    [StructLayout(LayoutKind.Explicit, Size = 10)]
    public struct ConsolePixel
    {
        [FieldOffset(0)]
        public int ForegroundARGB; // Internal storage for Foreground as ARGB

        [FieldOffset(4)]
        public int BackgroundARGB; // Internal storage for Background as ARGB

        [FieldOffset(8)]
        public bool IsWide;

        [FieldOffset(9)]
        public char Character;

        // Property for Foreground color
        public Color Foreground
        {
            get => Color.FromArgb(ForegroundARGB);
            set => ForegroundARGB = value.ToArgb();
        }

        // Property for Background color
        public Color Background
        {
            get => Color.FromArgb(BackgroundARGB);
            set => BackgroundARGB = value.ToArgb();
        }

        public ConsolePixel(char character, Color foreground, Color background, bool isWide)
        {
            Character = character;
            Foreground = foreground;
            Background = background;
            IsWide = isWide;
        }

        public override string ToString()
        {
            return $"{Character},{Foreground},{Background},{IsWide}";
        }
    }
}
