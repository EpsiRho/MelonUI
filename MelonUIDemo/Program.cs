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

while (true) 
{
    Thread.Sleep(1000);
}

return;

