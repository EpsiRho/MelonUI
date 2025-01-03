using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Testing;
using System.Drawing;
using MelonUI.Enums;
using System.Diagnostics;
using System.Collections.Concurrent;
using MelonUI.Base;
using MelonUIDemo.Backends;

DemoWelcomeBackend.CWM = new ConsoleWindowManager();
DemoWelcomeBackend.CWM.SetTitle("[MelonUI V1.0b MXML Demo (1216122424)]");

TextBlock tb = new TextBlock()
{
    Text = "",
    X = "25%",
    Y = "25%",
    Width = "50%",
    Height = "50%",
};

string wpxml = File.ReadAllText(@"Pages\DemoWelcome.xml");
var WelcomePage = new MUIPage();
var wpcompiled = WelcomePage.Compile(wpxml);
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

while (true) 
{
    TestPageBackend.CurX1 = $"{CurX1}%";
    TestPageBackend.CurY1 = $"{CurY1}%";
    TestPageBackend.CurX2 = $"{CurX2}%";
    TestPageBackend.CurY2 = $"{CurY2}%";
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
    TestPageBackend.CurrentPosStr = $"Position A: ({TestPageBackend.CurX1},{TestPageBackend.CurY1})\nPosition B: ({TestPageBackend.CurX2},{TestPageBackend.CurY2})";
    Thread.Sleep(20);
}

return;

