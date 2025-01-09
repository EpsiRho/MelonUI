using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Testing;
using System.Drawing;
using MelonUI.Enums;
using System.Diagnostics;
using System.Collections.Concurrent;
using MelonUI.Base;
using MelonUIDemo.Backends;
using Pastel;
using MelonUI.Helpers;

DemoWelcomeBackend.CWM = new ConsoleWindowManager();
DemoWelcomeBackend.CWM.EnableTitleBar = true;
DemoWelcomeBackend.CWM.SetTitle("[MelonUI V1.0b MXML Demo (1216122424)]");

TextBlock tb = new TextBlock()
{
    Text = "",
    X = "25%",
    Y = "25%",
    Width = "50%",
    Height = "50%",
};

string wpxml = File.ReadAllText(@"D:\Documents\GitHub\MelonUI\MelonUIDemo\Pages\DemoMenu.xml");
//string wpxml = File.ReadAllText(@"C:\Users\jhset\Desktop\test.xml");
var WelcomePage = new MUIPage();
var wpcompiled = WelcomePage.Compile(wpxml);

//foreach(var m in WelcomePage.CompilerMessages)
//{
//    Console.WriteLine(m);
//}
//return;

// Compiler Test Code
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("[Compiler Output]");

var msg = WelcomePage.GetSimpleCompilerDisplay();
Console.WriteLine($"{msg}");

TestPageBackend.TestText = ParamParser.GetGradientString(TestPageBackend.TestText, new[] { Color.FromArgb(255,255,50,50), Color.FromArgb(255, 0, 100, 255) });


// Test Code
if (!wpcompiled)
{
    DemoWelcomeBackend.CWM.SetStatus("What have you done");
    int w = Console.WindowWidth;
    while (true)
    {
        if (w != Console.WindowWidth)
        {
            Console.Clear();
            Console.WriteLine($"[Compiler Output ({w})]");
            msg = WelcomePage.GetSimpleCompilerDisplay();
            Console.WriteLine($"{msg}");
            w = Console.WindowWidth;
        }
        Thread.Sleep(10);
    }

    return;
}
else
{
    DemoWelcomeBackend.CWM.SetStatus("MUIPage compiled.");
    DemoWelcomeBackend.CWM.AddElement(WelcomePage);

}

DemoWelcomeBackend.CWM.ManageConsole(DemoWelcomeBackend.CancelSource.Token);

bool XLeft = false;
bool XRight = true;
bool YUp = false;
bool YDown = false;

int CurX1 = 90;
int CurY1 = 90;

int CurX2 = 10;
int CurY2 = 10;

float step = 0.0f;
                             // Red                          Orange                            Yellow                            Green                           Light Blue                        Blue                            Purple                            Pink                              Red
Color[] Line1Gradient = new[] { Color.FromArgb(255,255,0,0), Color.FromArgb(255, 255, 160, 0), Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 0, 255, 0), Color.FromArgb(255, 0, 190, 255), Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 137, 0, 255), Color.FromArgb(255, 255, 0, 205), Color.FromArgb(255, 255, 0, 0) };
                             // Cyan                         Pink                                Cyan
Color[] Line2Gradient = new[] { Color.FromArgb(255,0,255,255), Color.FromArgb(255, 255, 0, 200), Color.FromArgb(255, 0, 255, 255) };

DemoWelcomeBackend.CWM.RegisterKeyboardControl(ConsoleKey.F12, () =>
{
    var str = DemoWelcomeBackend.CWM.Screenshot(false);
    string dir = $"{DateTime.Now}.txt";
    var chs = Path.GetInvalidFileNameChars();
    foreach (var item in chs)
    {
        dir = dir.Replace(item, '-');
    }
    Directory.CreateDirectory("Screenshots");
    File.WriteAllText($"Screenshots\\{dir}", str);
}, "Screenshot");

while (true) 
{
    TestPageBackend.CurX1 = $"{CurX1}%";
    TestPageBackend.CurY1 = $"{CurY1}%";
    TestPageBackend.CurX2 = $"{CurX2}%";
    TestPageBackend.CurY2 = $"{CurY2}%";
    TestPageBackend.LineColor = ParamParser.GetGradientColor(Line1Gradient, step);
    TestPageBackend.AntiLineColor = ParamParser.GetGradientColor(Line2Gradient, 1.0f - step);
    step += 0.005f;
    if (step >= 1.0f)
    {
        step = 0.0f;
    }
    if (XRight)
    {
        CurX2++;
        CurX1--;
        if (CurX2 >= 85)
        {
            XRight = false;
            YDown = true;
        }
    }
    else if (XLeft)
    {
        CurX2--;
        CurX1++;
        if (CurX2 <= 10)
        {
            XLeft = false;
            YUp = true;
        }
    }
    else if (YUp)
    {
        CurY2--;
        CurY1++;
        if (CurY2 <= 10)
        {
            YUp = false;
            XRight = true;
        }
    }
    else if (YDown)
    {
        CurY2++;
        CurY1--;
        if (CurY2 >= 85)
        {
            YDown = false;
            XLeft = true;
        }
    }



    TestPageBackend.CurrentPosStr = $"Position A: ({TestPageBackend.CurX1},{TestPageBackend.CurY1})\nPosition B: ({TestPageBackend.CurX2},{TestPageBackend.CurY2})\nStep: {step}";
    Thread.Sleep(20);
}

return;

