using System.Drawing;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using MelonUI.Managers;

namespace MelonUI.Docs;

public static class Recipes
{
    public static MUIPage Layout()
    {
        // sample:layout
        var page = new MUIPage { ShowBorder = false };
        page.Children.Add(new TextBlock { Text = "Library", Height = "1", ShowBorder = false });
        var grid = new GridContainer(1, false, 2) { Y = "1", Height = "80%" };
        grid.AddElement(new TextBlock { Text = "Albums" }, 0, 0);
        grid.AddElement(new TextBlock { Text = "Artists" }, 0, 1);
        page.Children.Add(grid);
        page.Children.Add(new TextBlock
        {
            Text = "Ready", Height = "1", XYAlignment = Alignment.BottomLeft, ShowBorder = false
        });
        // end:layout
        return page;
    }
    public static void Reuse(MUIPage first, MUIPage second)
    {
        // sample:ownership
        var card = new TextBlock { Text = "Cached result" };
        first.Children.Add(card);
        card.Remove();                   // detached, still usable
        second.Children.Add(card);       // one owner at a time
        card.IsVisible = false;          // hidden, still owned
        card.Dispose();                  // detached and permanently destroyed
        // end:ownership
    }
    public static void ColorPopup(ConsoleWindowManager window, TextBlock preview)
    {
        // sample:popups
        var picker = new RgbColorPicker
        {
            Width = "70%", Height = "8", XYAlignment = Alignment.Centered
        };
        picker.BeginEdit(preview.Foreground);
        var dialog = window.ShowPopup(picker);
        picker.ColorChanged += color => preview.Foreground = color;
        picker.Confirmed += _ => dialog.Dispose();
        picker.Cancelled += color => { preview.Foreground = color; dialog.Dispose(); };
        // end:popups
    }
    public static async Task ReloadAsync(ConsoleWindowManager window, MUIPage page, string path,
        TextBlock status, CancellationToken token)
    {
        // sample:reload
        var markup = await File.ReadAllTextAsync(path, token);
        await window.InvokeAsync(() =>
        {
            status.Text = page.Compile(markup, path)
                ? "Compiled"
                : page.GetSimpleCompilerDisplay();
        });
        // end:reload
    }
    public static void Bind(TextBox input, SettingsModel model)
    {
        // sample:binding
        input.SetBinding(nameof(TextBox.Text),
            Binding.CreatePath(model, nameof(SettingsModel.Name), typeof(string)));
        input.Text = "Ada";              // writes model.Name
        input.RemoveBinding(nameof(TextBox.Text));
        // end:binding
    }
    public static void Capture()
    {
        // sample:capture
        using var page = new MUIPage { Height = "5", Width = "32", ShowBorder = true };
        page.Children.Add(new TextBlock { Text = "Captured without a terminal", Height = "1", ShowBorder = false });
        page.CalculateLayout(0, 0, 32, 5);
        string text = page.Render().Screenshot(keepColor: false);
        // end:capture
    }
}
