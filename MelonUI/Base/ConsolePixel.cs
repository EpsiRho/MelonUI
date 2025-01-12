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
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct ConsolePixel
    {
        [FieldOffset(0)]
        public int ForegroundARGB; // Internal storage for Foreground as ARGB
        [FieldOffset(0)]
        public uint B;

        [FieldOffset(4)]
        public int BackgroundARGB; // Internal storage for Background as ARGB
        [FieldOffset(4)]
        public uint G;

        [FieldOffset(8)]
        public bool IsWide;
        [FieldOffset(8)]
        public uint R;

        [FieldOffset(12)]
        public uint A;
        [FieldOffset(12)]
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
        public int ToOleColor()
        {
            // Combine the colors into OLE color format (0x00BBGGRR)
            return (int)(R << 16 | G << 8 | B);
        }
    }
}
