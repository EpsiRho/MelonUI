using MelonUI.Default;
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
        public static Color ProgColor { get; set; } = Color.FromArgb(255,255,255,255);
        public static Color ProgBgColor { get; set; }
        public static string TestText { get; set; } = $"Oh wow a bunch of [Color(0,255,0)]text [Color(255,255,255)]being used to check if this alignment works";
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
    }
}
