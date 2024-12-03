using MelonUI.Components;
using MelonUI.Default;
using MelonUI.Managers;

var manager = new ConsoleWindowManager(); // Create a new window manager
// b0.2 Demo
manager.SetTitle("MelonUI V2.0 Demo (b0.2)"); // Set the title
manager.SetStatus($"Started: {DateTime.Now}"); // Set the status bar

// Menu


// b0.1 Demo
manager.SetTitle("MelonUI V2.0 Demo (b0.1)"); // Set the title
manager.SetStatus($"Started: {DateTime.Now}"); // Set the status bar

// Music Player
var mgrid = new GridContainer(1, true, 1) // Create a Grid to put the item in so we can use it's animation model (true)
{
    X = "60%", // Relative X/Y/Width/Height supported by Percentage
    Y = "10%",
    Width = "35%",
    Height = "40%",
    Name = "MusicGrid"
};

MusicPlayerElement mp = new(null) // Takes in a PlaybackManager based class, so you can define an audio manager. I'm using a wrapper for NAudio, Windows Only. 
{                                 // Here it's null because we haven't preloaded a file. You could open a FilePicker first or open it from the Music Player.
    ShowBorder = false
};

mgrid.AddElement(mp, 0, 0); // Add the Music Player to the Grid

// Image
ConsoleImage img = new ConsoleImage(@"E:\Pictures\Epsi\iconcommission13-2-2019.png", "60%", "99%") // Create an image viewer with x image at 60% w and 90% height.
{                                                                                                  // Important to note Image dimensions will not be 1:1, i.e 50% 50% or 20 20 will not be a 1:1 ration.
    X = "0",                                                                                       // This is because Console Pixels are taller than they are wide.
    Y = "0",
    Name = "EpsiPic",
    UseBg = true
};
img.RegisterKeyboardControl(ConsoleKey.M, () =>
{
    manager.AddElement(mgrid);
}, "Open Music Player"); // Custom control for M so I can show the music player
int mode = 0;
img.RegisterKeyboardControl(ConsoleKey.T, () =>
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
}, "Open Music Player"); // Custom control for M so I can show the music player
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

CancellationToken RenderToken = new CancellationToken(); // Token used to end rendering, if the application desires to do so.
await manager.ManageConsole(RenderToken); // Otherwise, this will launch Render and Control threads, and hold the main thread so the app doesn't close.