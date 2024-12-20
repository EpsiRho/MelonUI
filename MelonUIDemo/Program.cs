using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Testing;
using System.Drawing;
using MelonUI.Enums;
using System.Diagnostics;
using System.Collections.Concurrent;
using MelonUI.Base;

var manager = new ConsoleWindowManager(); // Create a new window manager
TestPageBackend.CWM = manager;
manager.SetTitle("TEST");

TextBlock tb = new TextBlock()
{
    Text = "",
    X = "25%",
    Y = "25%",
    Width = "50%",
    Height = "50%",
};

string xml = File.ReadAllText(@"C:\Users\jhset\Desktop\TestPage.xml");
var page = new MUIPage();


var success = page.Compile(xml);

if(!success)
{
    manager.SetStatus("Invalid mxml, compile failed.");
    foreach (var line in page.CompilerMessages)
    {
        tb.Text += $"{line}\n";
    }
    manager.AddElement(tb);
}
else
{
    manager.SetStatus("MUIPage compiled.");
    manager.AddElement(page);

}
manager.SetTitle("MXML Demo");
//var item = (FPSCounter)manager.GetSubChildByName("FPSDisplay");
//manager.FrameRendered += item.OnFrameRendered;

CancellationTokenSource CancelSource = new CancellationTokenSource();
manager.ManageConsole(CancelSource.Token);

FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
fileSystemWatcher.Path = @"C:\Users\jhset\Desktop";
fileSystemWatcher.Filter = "*.xml";
fileSystemWatcher.IncludeSubdirectories = false;
fileSystemWatcher.EnableRaisingEvents = true;
fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;

bool ping = false;
fileSystemWatcher.Changed += (s, e) =>
{
    manager.SetStatus("Changes Found");
    if (e.ChangeType != WatcherChangeTypes.Changed)
    {
        return;
    }
    //if (!ping)
    //{
    //    ping = true;
    //    return;
    //}
    //else
    //{
    //    ping = false;
    //}
    manager.SetStatus("Waiting for file to unlock");
    //Thread.Sleep(1000);
    try
    {
        manager.SetStatus("Compiling MXML");
        manager.RemoveElement(page);
        try
        {
            xml = File.ReadAllText(@"C:\Users\jhset\Desktop\TestPage.xml");
        }
        catch (Exception)
        {
            while (true)
            {
                try
                {
                    xml = File.ReadAllText(@"C:\Users\jhset\Desktop\TestPage.xml");
                    break;
                }
                catch (Exception)
                {

                }
            }
        }
        page = new MUIPage();
        var chk = page.Compile(xml);
        if (!chk)
        {
            manager.SetStatus($"Page compilation failed");
            tb.Text = "";
            foreach (var line in page.CompilerMessages)
            {
                tb.Text += $"{line}\n";
            }
            manager.RemoveElement(page);
            manager.AddElement(tb);
            return;
        }
        manager.AddElement(page);
        manager.RemoveElement(tb);
        manager.SetStatus($"Page compiled!");
        manager.MoveFocus(page.Children.FirstOrDefault(x => x.ConsiderForFocus && x.IsVisible));
    }
    catch (Exception ex) { }
};

int secs = 0;
while (true)
{
    TestPageBackend.Name = $"{secs}s online";
    Thread.Sleep(1000);
    secs++;
}

return;

