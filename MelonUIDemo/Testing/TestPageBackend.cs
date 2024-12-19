using MelonUI.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUIDemo.Testing
{
    public static class TestPageBackend
    {
        public static string Name { get; set; } = "Penis";
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
    }
}
