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

string wpxml = File.ReadAllText(@"C:\Users\jhset\source\repos\MelonUIDemo\MelonUIDemo\Pages\DemoPage.xml");
var WelcomePage = new MUIPage();
var wpcompiled = WelcomePage.Compile(wpxml);

// Compiler Test Code
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("[Compiler Output]");

var msg = WelcomePage.GetSimpleCompilerDisplay();
Console.WriteLine($"{msg}");

int w = Console.WindowWidth;
while (true)
{
    if(w != Console.WindowWidth)
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
void Draw(List<CompilerMessage> lines)
{
    foreach (var line in lines)
    {
        Color pc = Color.White;
        switch (line.Severity)
        {
            case MessageSeverity.Debug:
                pc = Color.FromArgb(255, 120, 120, 120);
                break;
            case MessageSeverity.Info:
                pc = Color.FromArgb(255, 255, 255, 255);
                break;
            case MessageSeverity.Warning:
                pc = Color.FromArgb(255, 255, 255, 0);
                break;
            case MessageSeverity.Error:
                pc = Color.FromArgb(255, 255, 0, 0);
                break;
            case MessageSeverity.Success:
                pc = Color.FromArgb(255, 0, 255, 0);
                break;
        }
        if (line == WelcomePage.CompilerFocusedMessage)
        {
            pc = Color.Cyan;
        }
        Console.WriteLine($"{line}".Pastel(pc));
    }
}

Draw(WelcomePage.CompilerMessages);
while (true)
{
    var k = Console.ReadKey(true);

    Console.Clear();
    if (k.Key == ConsoleKey.Escape)
    {
        break;
    }
    else if(k.Key == ConsoleKey.NumPad0)
    {
        Draw(WelcomePage.CompilerMessages); // 0 - All
    }
    else if (k.Key == ConsoleKey.NumPad1)
    {
        Draw(WelcomePage.CompilerMessages.Where(x=>x.Severity == MessageSeverity.Info || x.Severity == MessageSeverity.Warning || x.Severity == MessageSeverity.Error || x.Severity == MessageSeverity.Success).ToList()); // 1 - Info+
    }
    else if (k.Key == ConsoleKey.NumPad2)
    {
        Draw(WelcomePage.CompilerMessages.Where(x => x.Severity == MessageSeverity.Warning || x.Severity == MessageSeverity.Error || x.Severity == MessageSeverity.Success).ToList()); // 2 - Warnings+
    }
    else if (k.Key == ConsoleKey.NumPad3)
    {
        Draw(WelcomePage.CompilerMessages.Where(x => x.Severity == MessageSeverity.Error || x.Severity == MessageSeverity.Success).ToList()); // 3 - Errors+
    }
    else if (k.Key == ConsoleKey.NumPad4) 
    {
        Draw(WelcomePage.CompilerMessages.Where(x => x.Severity == MessageSeverity.Success).ToList()); // 4 - Success+
    }
    else if (k.Key == ConsoleKey.NumPad5)
    {
        Draw(WelcomePage.CompilerMessages.Where(x => x.Severity == MessageSeverity.Debug).ToList()); // 5 - Debug Only
    }
    else if (k.Key == ConsoleKey.NumPad6)
    {
        Draw(WelcomePage.CompilerMessages.Where(x => x.Severity == MessageSeverity.Info).ToList()); // 6 - Info Only
    }
    else if (k.Key == ConsoleKey.NumPad7)
    {
        Draw(WelcomePage.CompilerMessages.Where(x => x.Severity == MessageSeverity.Warning).ToList()); // 7 - Warnings Only
    }
    else if (k.Key == ConsoleKey.NumPad8)
    {
        Draw(WelcomePage.CompilerMessages.Where(x => x.Severity == MessageSeverity.Error).ToList()); // 8 - Errors Only
    }
}

return;

// Test Code
if (!wpcompiled)
{
    DemoWelcomeBackend.CWM.SetStatus("What have you done");
    foreach (var line in WelcomePage.CompilerMessages)
    {
        tb.Text += $"{line}\n";
    }
    DemoWelcomeBackend.CWM.AddElement(tb);
}
else
{
    DemoWelcomeBackend.CWM.SetStatus("MUIPage compiled.");
    DemoWelcomeBackend.CWM.AddElement(WelcomePage);

}

CancellationTokenSource CancelSource = new CancellationTokenSource();
DemoWelcomeBackend.CWM.ManageConsole(CancelSource.Token);

bool XLeft = false;
bool XRight = true;
bool YUp = false;
bool YDown = false;

int CurX1 = 90;
int CurY1 = 90;

int CurX2 = 10;
int CurY2 = 10;

float step = 0.0f;
                             // Red                          Orange                            Yellow                            Green                           Light Blue                        Blue                            Purple                            Red
Color[] Line1Gradient = new[] { Color.FromArgb(255,255,0,0), Color.FromArgb(255, 255, 160, 0), Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 0, 255, 0), Color.FromArgb(255, 0, 190, 255), Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 255, 160, 0), Color.FromArgb(255, 255, 0, 0) };
                             // Cyan                         Pink                                Cyan
Color[] Line2Gradient = new[] { Color.FromArgb(255,0,255,255), Color.FromArgb(255, 255, 0, 200), Color.FromArgb(255, 0, 255, 255) };

DemoWelcomeBackend.CWM.RegisterKeyboardControl(ConsoleKey.LeftArrow, () =>
{
    TestPageBackend.LineColor = ParamParser.GetGradientColor(Line2Gradient, step);
    TestPageBackend.AntiLineColor = ParamParser.GetGradientColor(Line2Gradient, 1.0f - step);
    step -= 0.005f;
    if (step <= 0.0f)
    {
        step += 1.0f;
    }
}, "Gradient Animation Step--");
DemoWelcomeBackend.CWM.RegisterKeyboardControl(ConsoleKey.RightArrow, () =>
{
    TestPageBackend.LineColor = ParamParser.GetGradientColor(Line2Gradient, step);
    TestPageBackend.AntiLineColor = ParamParser.GetGradientColor(Line2Gradient, 1.0f - step);
    step += 0.005f;
    if (step >= 1.0f)
    {
        step = 0.0f;
    }
}, "Gradient Animation Step++");

while (true) 
{
    TestPageBackend.CurX1 = $"{CurX1}%";
    TestPageBackend.CurY1 = $"{CurY1}%";
    TestPageBackend.CurX2 = $"{CurX2}%";
    TestPageBackend.CurY2 = $"{CurY2}%";
    if(step >= 1.0f)
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

