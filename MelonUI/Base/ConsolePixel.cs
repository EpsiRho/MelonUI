using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public struct ConsolePixel
    {
        public char Character;
        public ConsoleColor Foreground;
        public ConsoleColor Background;

        public ConsolePixel(char character, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            Character = character;
            Foreground = foreground;
            Background = background;
        }
    }
}
