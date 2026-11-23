using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using MelonUI.Managers;

namespace MelonUIDemo;

// Builds a small page in plain C#: a text box, a button, a menu, and a status line.
internal static class CSharpDemo
{
    public static async Task RunAsync()
    {
        await using var window = new ConsoleWindowManager { Title = "MelonUI Demo" };

        // The status line along the bottom. Everything else writes to it.
        var status = new TextBlock
        {
            Text = "Tab moves between controls. Esc quits.",
            Height = "1",
            ShowBorder = false,
            XYAlignment = Alignment.BottomLeft,
        };

        var name = new TextBox { Label = "Your name", Text = "Melon listener", Width = "42", Height = "5" };

        var hello = new Button { Text = "Say hello", Y = "5", Width = "42", Height = "3" };
        hello.OnPressed = () => status.Text = $"Hello, {name.Text}!";

        // A menu takes a name and an action for each option.
        var menu = new OptionsMenu { MenuName = "Pick one", X = "44", Width = "30", Height = "8" };
        menu.Options.Add(new MenuItem("Shout the name", () => status.Text = name.Text.ToUpper()));
        menu.Options.Add(new MenuItem("Clear the name", () => name.Text = ""));
        menu.Options.Add(new MenuItem("Quit", window.Stop));

        var page = new MUIPage { ShowBorder = false, Children = { name, hello, menu, status } };
        window.Elements.Add(page);

        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
        await window.RunAsync();
    }
}
