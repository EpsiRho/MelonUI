﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public class KeyboardControl
    {
        public ConsoleKey Key { get; set; }
        public bool RequireShift { get; set; }
        public bool RequireControl { get; set; }
        public bool RequireAlt { get; set; }
        public Action Action { get; set; }
        public string Description { get; set; }

        public bool Matches(ConsoleKeyInfo keyInfo)
        {
            return keyInfo.Key == Key &&
                   keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) == RequireShift &&
                   keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) == RequireControl &&
                   keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt) == RequireAlt;
        }
    }

}