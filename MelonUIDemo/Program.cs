using MelonUI.Components;
using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Testing;
using System.Drawing;

var manager = new ConsoleWindowManager(); // Create a new window manager

// Melon OOBE Demo
manager.SetTitle("TEST");

//MelonOOBE OOBE = new MelonOOBE();
//OOBE.ShowOOBE(manager);

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

FPSCounter fps = new FPSCounter()
{
    X = "0",
    Y = "60%",
    Width = "5",
    Height = "4",
    Name = "FPSCounter",
    FocusedBorderColor = Color.FromArgb(255, 255, 6, 77)
};
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

manager.FrameRendered += fps.OnFrameRendered;
manager.AddElement(fps, false);
manager.AddElement(bar, false);

CancellationToken RenderToken = new CancellationToken();
await manager.ManageConsole(RenderToken); 

return;