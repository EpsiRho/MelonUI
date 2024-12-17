using MelonUI.Components;
using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Testing;
using System.Drawing;
using MelonUI.Enums;
using System.Diagnostics;
using System.Collections.Concurrent;

var manager = new ConsoleWindowManager(); // Create a new window manager

manager.SetTitle("TEST");

LockedList<WorkItem> workItems = new LockedList<WorkItem>();
workItems.Add(new WorkItem("Fetch data", true));
WorkLog log = new WorkLog(workItems)
{
    X = "0",
    Y = "0",
    Width = "88%",
    Height = "98%",
    ShowProgressBar = true,
};
FPSCounter fps = new FPSCounter()
{
    X = "90%",
    Y = "0",
    Width = "5",
    Height = "4",
    Name = "FPSCounter",
    FocusedBorderColor = Color.FromArgb(255, 255, 6, 77)
};

manager.FrameRendered += fps.OnFrameRendered;

log.RegisterKeyboardControl(ConsoleKey.NumPad1, () => { log.ShowItemCount = !log.ShowItemCount; }, "ItemCountToggle");
log.RegisterKeyboardControl(ConsoleKey.NumPad2, () => { log.ShowProgressBar = !log.ShowProgressBar; }, "ProgressBarToggle");
log.RegisterKeyboardControl(ConsoleKey.NumPad3, () => { log.ShowStatus = !log.ShowStatus; }, "ShowStatusToggle");
log.RegisterKeyboardControl(ConsoleKey.NumPad4, () => { log.ShowTime = !log.ShowTime; }, "ShowTimeToggle");
log.RegisterKeyboardControl(ConsoleKey.NumPad5, () => { log.ShowBorder = !log.ShowBorder; }, "ShowBorderToggle");

manager.AddElement(fps);
manager.AddElement(log);

Console.ReadLine();

//MelonOOBE OOBE = new MelonOOBE();
//OOBE.ShowOOBE(manager);

CancellationTokenSource RenderSource = new CancellationTokenSource();
manager.ManageConsole(RenderSource.Token);

Thread.Sleep(2000);

workItems[0].Status = WorkStatus.Completed;
workItems.Remove(workItems[0]);

var rand = new Random();
var rand2 = new Random();
bool stop = false;
Thread t = new Thread(async () =>
{
    while (!RenderSource.Token.IsCancellationRequested)
    {
        try
        {
            if (stop)
            {
                stop = false;
                return;
            }
            int max = workItems.Count > 20 ? 10 : workItems.Count;
            var temp = workItems.Where(x=>x.Status != WorkStatus.Completed).Take(20);
            foreach(var item in temp)
            {
                if (stop)
                {
                    stop = false;
                    return;
                }
                int idx = rand2.Next(0, max);
                int nextAdd = rand2.Next(0, 20);
                if (item.Status == WorkStatus.Errored || item.Status == WorkStatus.Completed || nextAdd == 0)
                {
                    continue;
                }
                else if (item.Status == WorkStatus.Running) 
                {
                    //var err = rand2.Next(0, 200);
                    //if (err == 0)
                    //{
                    //    int coin = rand2.Next(0, 2);
                    //    if (coin == 1)
                    //    {
                    //        workItems[idx].Status = WorkStatus.Errored;
                    //    }
                    //    continue;
                    //}
                }

                workItems[workItems.IndexOf(item)].CompletedItems += nextAdd > 10 ? nextAdd : 0;

                if (item.Status == WorkStatus.Completed)
                {
                    //Thread.Sleep(50);
                    //workItems.Remove(workItems[idx]);
                }
                if (item.CompletedItems >= item.TotalItems)
                {
                    if (stop)
                    {
                        stop = false;
                        return;
                    }
                    workItems[workItems.IndexOf(item)].Status = WorkStatus.Completed;
                    workItems[workItems.IndexOf(item)].CompletedItems = workItems[workItems.IndexOf(item)].TotalItems;
                } 
                else if (item.CompletedItems > 0)
                {
                    workItems[workItems.IndexOf(item)].Status = WorkStatus.Running;
                }
            }
        }
        catch (Exception)
        {

        }
        Thread.Sleep(rand2.Next(0, 50));
    }
    stop = false;
});
t.Start();

int count = 0;
while (count <= 80)
{
    int newItemsCount = rand.Next(0, 10);
    for (int i = 0; i < newItemsCount; i++)
    {
        var idx = rand.Next(100000, 1000000);
        var item = new WorkItem($"Processing {count}", rand.Next(300,1000), false);
        workItems.Add(item);
        count++;
    }
    Thread.Sleep(600);
}


while(workItems.Where(x=>x.Status == WorkStatus.Completed).Count() != workItems.Count)
{
    Thread.Sleep(1000);
}
stop = true;
while (stop)
{
    Thread.Sleep(100);
}

workItems.Clear();
workItems.Add(new WorkItem("Save to Disk", true));
workItems.Add(new WorkItem("Upload to DB", false));

Thread.Sleep(600);
workItems[0].Complete();
workItems[1].Run();

Thread.Sleep(2000);
workItems[1].Complete();
Thread.Sleep(1000);

RenderSource.Cancel();

while (!RenderSource.Token.IsCancellationRequested)
{
    Thread.Sleep(16);
}
return;

OptionsMenu menu = new()
{
    X = "0",
    Y = "0",
    Width = "50%",
    Height = "50%",
    MenuName = "Default UI Objects",
    UseStatusBar = true,
    Background = Color.FromArgb(0, 255, 255, 255)
};
menu.Options.Add(("Show Music Player", () =>
{
    var mgrid = new GridContainer(1, true, 1) // Create a Grid to put the item in so we can use it's animation model (true)
    {
        X = "60%",
        Y = "10%",
        Width = "35%",
        Height = "40%",
        Name = "MusicGrid",
        Background = Color.FromArgb(0, 255, 255, 255)
    };

    MusicPlayerElement mp = new(null) // Takes in a PlaybackManager based class, so you can define an audio manager. I'm using a wrapper for NAudio, Windows Only. 
    {                                 // Here it's null because we haven't preloaded a file. You could open a FilePicker first or open it from the Music Player.
        ShowBorder = false
    };

    mgrid.AddElement(mp, 0, 0);
    manager.AddElement(mgrid);
}
));
menu.Options.Add(("Show Image", () =>
{
    // Image
    ConsoleImage img = new ConsoleImage(@"", "40%", "99%")
    {
        X = "60%",
        Y = "0",
        Name = "EpsiPic",
        UseBg = true
    };
    int mode = 0;
    img.RegisterKeyboardControl(ConsoleKey.M, () =>
    {
        switch (mode)
        {
            case 0:
                img.UseBg = false;
                img.UseColor = true;
                img.InitializeImageAsync();
                mode++;
                break;
            case 1:
                img.UseBg = false;
                img.UseColor = false;
                img.InitializeImageAsync();
                mode++;
                break;
            case 2:
                img.UseBg = true;
                img.UseColor = true;
                img.InitializeImageAsync();
                mode = 0;
                break;
        }
    }, "Change Diplay Mode");
    img.RegisterKeyboardControl(ConsoleKey.O, () =>
    {
        try
        {
            var filepickergrid = new GridContainer(1, true, 1)
            {
                X = "25%",
                Y = "25%",
                Width = "55%",
                Height = "55%",
                Name = "MPFilePickerGrid"
            };
            FilePicker picker = new FilePicker(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
            {
                ShowBorder = false,
                Name = "MPFilePicker"
            };
            picker.OnFileSelected += () =>
            {
                // On file select, close picker
                filepickergrid.AnimateClose(() =>
                {
                    // Remove File Picker
                    filepickergrid.RenderThreadDeleteMe = true;

                    Task.Run(() =>
                    {
                        try
                        {
                            img.Path = picker.Path;
                        }
                        catch (Exception ex)
                        {
                            img.ParentWindow.SetStatus("File Loading Error!");
                        }
                    });
                });

            };
            filepickergrid.AddElement(picker, 0, 0);
            img.ParentWindow.AddElement(filepickergrid, true);
            picker.Show();
        }
        catch (Exception e)
        {
            img.ParentWindow.SetStatus("File Picker Error!");
        }
    }, "Open File"); // When O is hit, add a filepicker and handle file input (done thru defining Actions)

    manager.AddElement(img); // Add the image viewer to the grid
}
));
//manager.AddElement(menu, false);


ProgressBar bar = new ProgressBar()
{
    X = "0",
    Y = "0",
    Width = "50%",
    Height = "3",
    Name = "FPSCounter",
    
    FocusedBorderColor = Color.FromArgb(255, 255, 6, 77),
    Style = ProgressBar.ProgressBarStyle.Loading
};

manager.AddElement(fps, false);
manager.AddElement(bar, false);

CancellationToken rrruh = new CancellationToken();
await manager.ManageConsole(rrruh); 

return;