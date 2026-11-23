using MelonUI.Base;
using MelonUI.Managers;

namespace MelonUI.Docs;

public static class MxmlStart
{
    // Pass the full path of Settings.mxml, or copy it beside your executable.
    public static async Task RunAsync(string filePath)
    {
        await using var window = new ConsoleWindowManager { Title = "Settings" };
        var model = new SettingsModel();
        var page = new MUIPage();
        page.BackendObjects.Add("App", model);
        if (!page.CompileFile(filePath))
            throw new InvalidOperationException(page.GetSimpleCompilerDisplay());
        window.Elements.Add(page);
        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
        await window.RunAsync();
    }
}
