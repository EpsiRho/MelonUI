# MelonUI

A .NET 10 console UI library with C# composition, MXML 1.0 markup and generated bindable properties.
MIT licensed. Local package version: **1.0.0**. The binding analyzer targets **netstandard2.0**.

- `MelonUI` is the core library. 
- `MelonUI.Shaders` is an optional OpenGL/Windows Direct3D extension (Windows Only).

Getting Started:
```csharp
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;

// Create a new window manager, this is the base that controls everything.
await using var window = new ConsoleWindowManager { Title = "Melon" };

// Create a new Page, these contain children elements and typically define one view at a time.
var page = new MUIPage
{
    ShowBorder = false, // No auto border
    Children = {    //  Add children elements, 
        new TextBox //  there are many default ones in MelonUI.Default,
        {           //  and an easy system for making custom elements.
            Name = "Name",
            Height = "3",
            Text = "Listener" 
        } 
    }  
};

 // add the page to the window (you can also just add any UIElement directly).
window.Elements.Add(page);

// Adds a keyboard control to the base window, to add an exit button
window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");

// Begin render and keyboard watchers
await window.RunAsync(); 
```

MXML allows you to define a MUIPage in XML style markup format:
```xml
<MUIPage MxmlVersion="1" ShowBorder="false">
  <TextBox Name="Name" Text="{App.Name}" Height="3"/>
  <Button Text="Save" Y="3" OnPressed="{App.Save}">
    <Button.OnPressed><Show Target="Result" Focus="false"/></Button.OnPressed>
  </Button>
  <TextBlock Name="Result" Text="Saved" Y="6" Height="1" IsVisible="false"/>
</MUIPage>
```

By default, Tab/Shift+Tab changes focus, Alt+arrows moves spatially. This allows users to select elements on a page and give control to them. For example, moving within an OptionsMenu to navigate a settings menu, then Alt+Arrow to switch to another control hosting page navigation.

## Documentation website

See [Documentation](https://epsirho.github.io/MelonUI/) for more info
