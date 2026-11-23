# MelonUI

A .NET 10 console UI library with C# composition, MXML 1.0 markup and generated bindable properties.
MIT licensed. Local package version: **1.0.0**. The binding analyzer targets **netstandard2.0**.

`MelonUI` is the terminal core. `MelonUI.Shaders` is an optional OpenGL/Windows Direct3D extension;
ordinary applications and Melon's future console do not need graphics or audio dependencies.

```csharp
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;

await using var window = new ConsoleWindowManager { Title = "Melon" };
var page = new MUIPage
{
    ShowBorder = false,
    Children = { new TextBox { Name = "Name", Height = "3", Text = "Listener" } }
};
window.Elements.Add(page);
window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
await window.RunAsync();
```

Construction has no terminal side effects. RunAsync owns one UI loop. Queue background changes
with Post/InvokeAsync. Children have one owner; removal detaches for reuse, and Dispose is final.
Title sets the native title where supported. Headings/status are ordinary controls.

MXML uses the same tree:

```xml
<MUIPage MxmlVersion="1" ShowBorder="false">
  <TextBox Name="Name" Text="{App.Name}" Height="3"/>
  <Button Text="Save" Y="3" OnPressed="{App.Save}">
    <Button.OnPressed><Show Target="Result" Focus="false"/></Button.OnPressed>
  </Button>
  <TextBlock Name="Result" Text="Saved" Y="6" Height="1" IsVisible="false"/>
</MUIPage>
```

Register a borrowed backend with `page.BackendObjects.Add("App", viewModel)`, then call CompileFile
or Compile. A failed reload preserves the live page. Property/event subscriptions follow UI lifetime;
ordered actions validate targets before publishing the new tree. MXML is trusted application code.

## Build, examples and packages

```powershell
# Windows: complete solution, including shaders and demos
dotnet build MelonUIDemo.sln -c Release --warnaserror
dotnet run --project MelonUI.Tests -c Release --no-build -- --gpu --soak
dotnet run --no-launch-profile --project MelonUIDemo -- --mxml
dotnet run --no-launch-profile --project MelonUIDemo -- --controls
dotnet run --no-launch-profile --project MelonUIDemo -- --shader --smoke
./scripts/Verify-Packages.ps1
```

For Linux/macOS, build **MelonUI.slnf**, then run MelonUI.Tests with `--soak` and omit `--gpu`.
ComputeSharp's native shader compiler requires Windows, so the full optional shader source build
is a Windows build entry point. `Verify-Packages.ps1 -CoreOnly` validates a fresh application and
separate plugin against local NuGet packages; it selects core-only automatically outside Windows.
Packages include the analyzer, README, license and notices. Validation builds packages locally;
it does not publish a release or push packages.

The default demo demonstrates C# lifecycle/reuse. `--controls` is the control gallery. `--mxml`
(also `--markup`, and the Visual Studio launch profile) creates a fresh `Demo.mxml` in a unique
`MelonUI-MXML` folder under the system temp directory and opens it in Notepad on Windows.
Edit the markup and save to recompile the live preview. A footer shows the file path and compiler
diagnostics; invalid edits retain the last working UI. F5 retries/recompiles; Escape quits.
The file is kept after exit, and the demo never overwrites the shipped starter or closes your editor.
On other platforms, open the displayed path in your editor. The borrowed `App` backend retains form
values across reloads. The demo watches the main file; after editing an included file, press F5.

`--shader` selects the optional OpenGL shader demo. Every mode accepts `--smoke` for a bounded
interactive check. The MXML smoke check does not launch an editor; it writes actual file changes
and verifies compilation, invalid edits, atomic saves, delete/recreate recovery and cleanup.

Tab/Shift+Tab changes focus, Alt+arrows moves spatially, ordinary arrows edit the focused control,
and modals contain focus/shortcuts. Password fields stay masked in ordinary captures. Cached idle
pages stop writing identical terminal frames; Updating continues at the configured cadence.

## Documentation website

The documentation site is at [epsirho.github.io/MelonUI](https://epsirho.github.io/MelonUI/) (source in [docs/site](docs/site/README.md)), with a quick start,
paired C#/MXML recipes, real text render captures, custom control/generator guides and the public API index.
Build it with Node.js alone using the committed API/capture data, or refresh examples with .NET 10.

## Guides

- [MXML language, bindings, actions and generator](docs/mxml.md)
- [C# ownership, dispatch and lifetime](docs/csharp-lifecycle.md)
- [Default controls and keyboard contract](docs/default-controls.md)
- [1.0 build/release boundaries and migration](docs/release-1.0.md)
- [Changelog](CHANGELOG.md), [MIT license](LICENSE), [dependency notices](THIRD-PARTY-NOTICES.md)

The runtime contract is untrimmed/JIT .NET 10; arbitrary reflection-based MXML/plugins are not a
NativeAOT or trimming contract. Local folder enumeration is synchronous. Complex Unicode shaping
depends on terminal/font support. Windows and Linux have local validation evidence; native macOS
and other terminals still need hands-on checks. Optional shaders have capability and main-thread
requirements. See the release guide for exact limits and migration details.
