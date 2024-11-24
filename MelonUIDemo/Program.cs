using MelonUI.Default;
using MelonUI.Managers;

Console.WriteLine("[+] MelonUI V2.0 Demo (b0.1)");

Console.WriteLine("[+] Creating Window Manager");
var manager = new ConsoleWindowManager();
manager.SetTitle("MelonUI V2.0 Demo (b0.1)");
manager.SetStatus($"Started: {DateTime.Now}");

Console.WriteLine("[+] Creating TextBox");
var grid = new GridContainer()
{
    RelativeX = "0%",
    RelativeY = "0%",
    RelativeWidth = "50%",
    RelativeHeight = "50%",
    Rows = 2,
    Columns = 2,
};
var pbar = new ProgressBar()
{
    RelativeX = "20%",
    RelativeY = "80%",
    RelativeWidth = "80%",
    RelativeHeight = "20%"
};
var tBox = new TextBox()
{
    RelativeX = "60%",
    RelativeY = "80%",
    RelativeWidth = "40%",
    RelativeHeight = "20%"
};
grid.AddElement(pbar, 1, 1);
manager.AddElement(grid);
manager.AddElement(tBox);

Console.WriteLine("[+] Start Rendering");
while (true)
{
    manager.HandleInput();
    manager.Render();
    Thread.Sleep(16); // ~60fps
}