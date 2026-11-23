using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using MelonUI.Managers;

namespace MelonUIDemo;

// Loads Pages/Settings.mxml and hooks it up to a SettingsModel.
// Edit the .mxml in the build folder while it's running and press F5 to see your changes.
internal static class MxmlDemo
{
    public static async Task RunAsync()
    {
        await using var window = new ConsoleWindowManager { Title = "MelonUI MXML Demo" };
        var file = Path.Combine(AppContext.BaseDirectory, "Pages", "Settings.mxml");
        var model = new SettingsModel();

        var help = new TextBlock
        {
            Text = $"F5 reloads {file}. Esc quits.",
            Height = "1",
            ShowBorder = false,
            XYAlignment = Alignment.BottomLeft,
        };

        MUIPage? page = null;
        void Load()
        {
            // "App" is the name the markup uses, like Text="{App.Name}".
            var next = new MUIPage();
            next.BackendObjects.Add("App", model);
            if (!next.CompileFile(file))
            {
                // Keep the last page that worked and show what went wrong.
                help.Text = next.GetSimpleCompilerDisplay();
                next.Dispose();
                return;
            }
            if (page != null) { window.Elements.Remove(page); page.Dispose(); }
            page = next;
            window.Elements.Insert(0, page);
            help.Text = $"F5 reloads {file}. Esc quits.";
        }

        window.Elements.Add(help);
        Load();

        window.RegisterKeyboardControl(ConsoleKey.F5, Load, "Reload");
        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
        await window.RunAsync();
    }
}
