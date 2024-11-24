using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;

Console.WriteLine("[+] MelonUI V2.0 Demo (b0.1)");

Console.WriteLine("[+] Creating Window Manager");
var manager = new ConsoleWindowManager();
manager.SetTitle("MelonUI V2.0 Demo (b0.1)");
manager.SetStatus($"Started: {DateTime.Now}");

Console.WriteLine("[+] Creating TextBox");
var grid = new GridContainer(4, true, 1, 1, 3, 3)
{
    RelativeX = "10%",
    RelativeY = "10%",
    RelativeWidth = "25%",
    RelativeHeight = "50%",
};


var pbar = new ProgressBar()
{
};

var Rewind = new Button(ConsoleKey.LeftArrow)
{
    Text = "<<"
};
var Play = new Button(ConsoleKey.Spacebar)
{
    Text = "Play"
};
var Skip = new Button(ConsoleKey.RightArrow)
{
    Text = ">>"
};
Rewind.OnPressed += () => 
{ 
    manager.SetStatus("Rewind -10 seconds");
    pbar.Progress -= 0.10f;
};
Play.OnPressed += () =>
{
    if (Play.Text == "Play")
    {
        Play.Text = "Pause";
        manager.SetStatus("Now Playing");
    }
    else
    {
        Play.Text = "Play";
        manager.SetStatus("Paused");
    }
};
Skip.OnPressed += () =>
{
    manager.SetStatus("Skip +10 seconds");
    pbar.Progress += 0.10f;
};

var Volume = new Button(ConsoleKey.UpArrow)
{
    Text = "D))"
};
var OpenFile = new Button(ConsoleKey.O)
{
    Text = "[+]"
};
var WaveFormToggle = new Button(ConsoleKey.W)
{
    Text = "~"
};
Volume.OnPressed += () =>
{
    manager.SetStatus("VOLUME");
    pbar.Progress -= 0.10f;
};
OpenFile.OnPressed += () =>
{

};
WaveFormToggle.OnPressed += () =>
{

};

grid.AddElement(pbar, 0, 0); // Audio Progress

grid.AddElement(Rewind, 2, 0);
grid.AddElement(Play, 2, 1);
grid.AddElement(Skip, 2, 2);

grid.AddElement(Volume, 3, 0);
grid.AddElement(OpenFile, 3, 1);
grid.AddElement(WaveFormToggle, 3, 2);

manager.AddElement(grid);



Console.WriteLine("[+] Start Rendering");
while (true)
{
    manager.HandleInput();
    manager.Render();
    Thread.Sleep(16); // ~60fps
}