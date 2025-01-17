﻿using MelonUI.Default;
using MelonUI.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUIDemo.Testing
{
    public static class TestPageBackend
    {
        public static string Name { get; set; } = "Penis";
        public static Action OnUp { get; set; } = ()=> { Debug.WriteLine("UUUUPPPPP!!!!!"); };
        public static Action OnAnyLetter { get; set; } = ()=> { Debug.WriteLine("OnAnyLetter!!!!!");};
        public static Func<ConsoleKeyInfo, bool> IsAnyLetter { get; set; } = IsThereAnyLetter;
        public static Color LineColor { get; set; } = Color.FromArgb(255,255,255,255);
        public static Color AntiLineColor { get; set; } = Color.FromArgb(255, 255, 255, 255);
        public static string TestText { get; set; } = $"Oh wow a bunch of text being used to check if this alignment works";
        public static List<MenuItem> Items { get; set; } = new List<MenuItem>()
        {
            new MenuItem()
            {
                Option = "Get Files"
            },
            new MenuItem()
            {
                Option = "Delete Cache"
            },
        };
        public static ConsoleWindowManager CWM { get; set; }
        public static bool IsThereAnyLetter(ConsoleKeyInfo key)
        {
            return true;
        }

        // Line Testing
        public static string CurrentPosStr { get; set; } = "Point A = (0,0)\nPoint B = (0,0)";
        public static string CurX1 { get; set; } = "0";
        public static string CurY1 { get; set; } = "0";
        public static string CurX2 { get; set; } = "0";
        public static string CurY2 { get; set; } = "0";
    }
}
