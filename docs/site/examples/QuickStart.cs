using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;

namespace MelonUI.Docs;

public static class QuickStart
{
    // Call this from Program.cs: await QuickStart.RunAsync();
    public static async Task RunAsync()
    {
        await using var window = new ConsoleWindowManager { Title = "Hello, MelonUI" };
        window.Elements.Add(CreatePage());
        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
        await window.RunAsync();
    }

    public static MUIPage CreatePage()
    {
        var status = new TextBlock { Text = "Ready", Y = "8", Height = "1", ShowBorder = false };
        var input = new TextBox { Label = "Your name", Text = "Melon listener", Width = "42", Height = "5" };
        var save = new Button { Text = "Say hello", Y = "5", Width = "42", Height = "3" };
        save.OnPressed = () => status.Text = $"Hello, {input.Text}!";
        var page = new MUIPage { ShowBorder = false, Children = { input, save, status } };
        return page;
    }
}
