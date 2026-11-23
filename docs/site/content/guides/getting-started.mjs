export function gettingStarted(h) {
  const {p, code, pair, terminal, file, list, links, subheading} = h;
  const s = (id,title,html) => ({id,title,html});
  return [
    {slug:'introduction',title:'MelonUI',nav:'Home',group:'Getting Started',summary:'MelonUI is a C# library for building console user interfaces. You can create pages and controls in C#, or use MXML, an XML-based markup language, to describe the UI in a file.',sections:[
      s('about','About',p('The library is built around UIElement, the base class used by each control. ConsoleWindowManager handles displaying the UI and processing input, while your application provides the data and the methods called by the controls.')+p('C# and MXML use the same controls and element tree, so you can choose the approach that works best for your application. You can also load an MXML page and access its elements from C#.')),
      s('features','Features',list([
        '<strong>Built-in Controls:</strong> Text input, menus, buttons, progress bars, color pickers, and layout containers are included.',
        '<strong>MXML:</strong> Pages can be written in markup and recompiled while the application is running. If an edit fails to compile, the previous page is kept.',
        '<strong>Data Binding:</strong> Controls can read and update properties on your application objects, and call methods when the user interacts with them.',
        '<strong>Custom Controls:</strong> You can derive from UIElement and use the binding generator to add your own properties.',
        '<strong>Keyboard Controls:</strong> The window handles focus navigation, while individual controls handle editing and selection.'
      ])),
      s('getting-started','Get Started Using MelonUI',p('See the <a href="@quick-start">Quick Start Guide</a> for setting up an application and creating your first page.')+subheading('C# and MXML')+p('For example, these two declarations create the same text input:')+pair(code('csharp',`var page = new MUIPage { ShowBorder = false };
page.Children.Add(new TextBox
{
    Label = "Display name", Text = "Melon listener", Width = "42", Height = "5"
});`),code('xml',`<MUIPage ShowBorder="false">
  <TextBox Label="Display name" Text="Melon listener" Width="42" Height="5"/>
</MUIPage>`))+terminal('textbox')),
      s('guides','Guides',p('There are guides on how the different parts of MelonUI work and how to use them:')+links([
        ['layout','Layout and Styling','How to position elements, divide a page into cells, and change its colors.'],
        ['ownership','Managing Elements','How to add, remove, reuse and dispose controls.'],
        ['window','ConsoleWindowManager','How to run a window and update its UI from other threads.'],
        ['focus','Focus and Keyboard Controls','How focus moves between controls and how input is handled.'],
        ['popups','Popups and Dialogs','How to open a popup and handle its lifetime.'],
        ['mxml','MXML Guide','How to write, load and update markup pages.'],
        ['data-binding','Data Binding','How to connect controls to your application data.']
      ])),
      s('custom-controls','Developing Custom Controls',p('If the built-in controls do not cover what you need, you can create a class that derives from UIElement. See the <a href="@custom-controls">Custom Controls Guide</a> for a complete example, and the <a href="@generator">Binding Generator</a> documentation for adding bindable properties.')),
      s('reference','Reference Documentation',p('The <a href="@controls">Controls Overview</a> lists the built-in elements with C# and MXML examples. The <a href="@api">API Reference</a> also includes the supporting types, such as MenuItem, WorkItem and the binding classes.')+p('MelonUI targets .NET 10. The optional MelonUI.Shaders library contains the GPU controls; it is not needed for a normal console UI. Build instructions and platform information are in <a href="@build-release">Building MelonUI</a>.'))
    ]},
    {slug:'quick-start',title:'Quick Start',group:'Getting Started',summary:"In this guide, we'll set up a console application and create a simple page with a text input, button and status message. After that, we'll create a similar page using MXML.",sections:[
      s('prerequisites','Prerequisites',list(['<strong>.NET SDK:</strong> .NET 10.','<strong>Development Environment:</strong> Visual Studio or another C# IDE. The commands below also work from a terminal.','<strong>MelonUI Source:</strong> A local checkout of the MelonUI repository.','<strong>Interactive Terminal:</strong> Running the window requires console input and output. Use CaptureText when you only need an offline render.'])),
      s('create','1. Create the Application',p('Create a console project and add a reference to MelonUI:')+code('shell',`dotnet new console -n HelloMelon -f net10.0
dotnet add HelloMelon reference /path/to/MelonUI/MelonUI/MelonUI.csproj`)+p('Replace the reference path with the location of your checkout. For example, on Windows this might be D:/Documents/GitHub/MelonUI/MelonUI/MelonUI.csproj. The project reference will also restore the Pastel and Wcwidth dependencies.')),
      s('compose','2. Create the Page',p("Let's make the page display a name field and a button. When the user presses the button, we'll show a greeting below it.")+p('Add QuickStart.cs to your project with the following code:')+code('csharp',file('QuickStart.cs'),'QuickStart.cs')+p('Replace the contents of Program.cs with:')+code('csharp','await MelonUI.Docs.QuickStart.RunAsync();','Program.cs')+subheading('Understanding the Code')+list(['<strong>ConsoleWindowManager:</strong> Manages the console window and runs the input/render loop.','<strong>MUIPage.Children:</strong> Holds the controls on the page. Each control is added once.','<strong>TextBox:</strong> Contains the name entered by the user. The label requires a height of 5 rows when a border is shown.','<strong>Button.OnPressed:</strong> Updates the status TextBlock using the current input value.','<strong>Y:</strong> Sets the vertical position. MUIPage does not automatically stack its children.','<strong>RunAsync:</strong> Runs until Stop is called or the cancellation token is cancelled.'])),
      s('run','3. Run and Test',code('shell','dotnet run --project HelloMelon')+p('You should see a page like this:')+terminal('quickstart')+list(['Enter a name in the TextBox.','Press Tab to focus the Say hello button.','Press Enter. The status should change to Hello, followed by the name you entered.','Press Escape to stop the window and restore the console.'],true)),
      s('markup','4. Create an MXML Page',p('The same controls can be declared in MXML. Save Settings.mxml in your application project:')+code('xml',file('Settings.mxml'),'Settings.mxml')+p('Add the following ItemGroup inside your .csproj so the file is copied beside the executable:')+code('xml',`<ItemGroup>
  <None Update="Settings.mxml" CopyToOutputDirectory="PreserveNewest"/>
</ItemGroup>`,'HelloMelon.csproj')+p('The App bindings refer to a SettingsModel object. Add this class to your project:')+code('csharp',file('SettingsModel.cs'),'SettingsModel.cs')+p('Now add the code that loads the page and registers the model:')+code('csharp',file('MxmlStart.cs'),'MxmlStart.cs')+p('To run this version, use the following in Program.cs:')+code('csharp','await MelonUI.Docs.MxmlStart.RunAsync(\n    Path.Combine(AppContext.BaseDirectory, "Settings.mxml"));','Program.cs')+p('BackendObjects gives MXML access to the existing model. The page does not own that model, so recompiling the markup will not replace or dispose it. See the <a href="@data-binding">Data Binding Guide</a> for more info.')),
      s('demo','5. Try the Live MXML Demo',p('From the MelonUI checkout, you can run the demo to edit markup while viewing the result:')+code('shell','dotnet run --no-launch-profile --project MelonUIDemo -- --mxml')+p('On Windows, the demo creates a file and opens it in Notepad. Save your changes to recompile the page. If there is an error, it will be shown below the last working page. F5 recompiles manually, and Escape closes the preview. The file is kept in the system temp directory after exit.')+p('On other platforms, open the displayed path in your editor.')),
      s('packages','Using Local Packages',p('The current code can also build MelonUI 1.0.0 as a local NuGet package. From the repository, run:')+code('shell',`pwsh ./scripts/Verify-Packages.ps1
dotnet nuget add source /path/to/MelonUI/artifacts/package-validation/feed --name MelonUI-local
dotnet add HelloMelon package MelonUI --version 1.0.0`)+p('Keep nuget.org configured alongside the local source so dependencies can be restored. Use -CoreOnly with Verify-Packages.ps1 if you do not need the shader package. These commands create local packages; they do not publish a public NuGet release.')+p('The NuGet package includes the binding generator. If you are using project references and adding custom bindable fields, see the <a href="@generator">generator setup</a>.'))
    ]}
  ];
}
