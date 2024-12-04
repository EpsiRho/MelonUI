using MelonUI.Components;
using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Testing;
using System.Drawing;

var manager = new ConsoleWindowManager(); // Create a new window manager

// Melon OOBE Demo
manager.SetTitle("TEST");

MelonOOBE OOBE = new MelonOOBE();
OOBE.ShowOOBE(manager);

CancellationToken RenderToken = new CancellationToken();
await manager.ManageConsole(RenderToken); 

return;

// b0.2 Demo
manager.SetTitle("MelonUI V2.0 Demo (b0.2)"); // Set the title
manager.SetStatus($"Started: {DateTime.Now}"); // Set the status bar

// Menu
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
        X = "60%", // Relative X/Y/Width/Height supported by Percentage
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

    mgrid.AddElement(mp, 0, 0); // Add the Music Player to the Grid
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

QueueContainer queue = new QueueContainer()
{
    X = "0",
    Y = "0",
    Width = "50%",
    Height = "50%",
    Background = Color.FromArgb(0, 255, 255, 255)
};

OptionsMenu AnimalMenu = new()
{
    X = "0",
    Y = "0",
    Width = "50%",
    Height = "50%",
    MenuName = "Pick an animal",
    Background = Color.FromArgb(0, 0, 0, 0)
};
AnimalMenu.Options.Add(("Cat", () =>
{
    queue.PopElement();
    manager.SetStatus("Picked Cat!");
}
));
AnimalMenu.Options.Add(("Dog", () =>
{
    queue.PopElement();
    manager.SetStatus("Picked Dog!");
}
));
AnimalMenu.Options.Add(("Bird", () =>
{
    queue.PopElement();
    manager.SetStatus("Picked Bird!");
}
));
AnimalMenu.Options.Add(("Fish", () =>
{
    queue.PopElement();
    manager.SetStatus("Picked Fish!");
}
));

OptionsMenu BoolMenu = new()
{
    X = "0",
    Y = "0",
    Width = "50%",
    Height = "50%",
    MenuName = "Do you have a pet?",
    Background = Color.FromArgb(0, 0, 0, 0)
};
BoolMenu.Options.Add(("Yes", () =>
{
    queue.PopElement();
    manager.SetStatus("Picked Yes");
}
));
BoolMenu.Options.Add(("No", () =>
{
    queue.PopElement();
    manager.SetStatus("Picked No");
}
));

var nameInput = new TextBox()
{
    Background = Color.FromArgb(0, 0, 0, 0),
    FocusedBackground = Color.FromArgb(0, 0, 0, 0),
    Label="Input a name",
    HideCharacters = true,
};
nameInput.OnEnter += NameInput_OnEnter;

void NameInput_OnEnter(string obj)
{
    if (!string.IsNullOrEmpty(obj))
    {
        queue.PopElement();
        manager.SetStatus($"Input: {obj}");
    }
}

queue.QueueElement(nameInput);
queue.QueueElement(AnimalMenu);
queue.QueueElement(BoolMenu);
queue.QueueElement(menu);

manager.AddElement(queue);

//CancellationToken RenderToken = new CancellationToken(); // Token used to end rendering, if the application desires to do so.
//await manager.ManageConsole(RenderToken); // Otherwise, this will launch Render and Control threads, and hold the main thread so the app doesn't close.