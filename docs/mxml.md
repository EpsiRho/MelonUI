# MXML 1.0

MXML builds the same owned tree as C# `Children` composition. The document root is `MUIPage`;
`MxmlVersion="1"` is optional and rejects unsupported language versions when present.
MXML is trusted application/plugin code: constructors, backend calls and file includes can execute
host operations. It is not an untrusted-configuration sandbox. DTDs/external XML entities are prohibited.

## Compile and reload

```csharp
using MelonUI.Base;
using MelonUI.Managers;

await using var window = new ConsoleWindowManager { Title = "Settings" };
var page = new MUIPage();
page.BackendObjects.Add("App", viewModel); // Borrowed; the application owns this object.
if (!page.CompileFile("pages/settings.xml"))
    throw new InvalidOperationException(page.GetSimpleCompilerDisplay());
window.Elements.Add(page);
window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
await window.RunAsync();
```

`Compile(markup, sourcePath: optionalPath)` compiles supplied text without rereading that file.
`CompileFile` reads and compiles it. Relative includes resolve beside their containing file; text
without a source path uses `CompilerOptions.BaseDirectory`, falling back to process CWD. Compilation
does not change CWD. Recursive includes and excessive depth/document length produce diagnostics.

Build/configuration happens on an unattached draft. A validation failure leaves the live tree,
root settings, bindings and focus intact. Success replaces the children, releases the old owned
resources and restores a uniquely named eligible focused control. New root attributes overwrite
their named properties; omitted root attributes preserve the existing page settings. Binding declarations
from the old document are replaced. Custom attach/detach/property hooks must not throw; commit attempts
to roll back ordinary tree/settings changes if a hook fails, but cannot undo arbitrary external side effects.

While running, call compile/reload on the UI owner through `Post`/`InvokeAsync`. Read file contents on
a background task, then queue the compile. A failed reload is expected application data: check the
returned bool. Runtime callback/binding failures use the normal window exception path.

`CompilerMessages` contains severity, stable `Code`, source path and XML line/column.
`CompilerFocusedMessage` is the current result and `CompilationFinished` is elapsed compile time.
`GetSimpleCompilerDisplay(maxWidth: 100)` is terminal-independent and safe at narrow widths.
`MXML` and `MXMLFilePath` describe the last successful document.

## Types, backends and constructors

Core types default to `MelonUI.Base` and `MelonUI.Default`. Add host registrations in C#:

```csharp
page.CompilerOptions.Assemblies.Add(typeof(MyWidget).Assembly);
page.CompilerOptions.Namespaces.Add("MyApp.Controls");
page.CompilerOptions.Factories.Add(typeof(MyWidget), () => new MyWidget(myService));
```

Factories return a new, unattached, undisposed compatible object. Otherwise a public parameterless
constructor is required. Factory failures are diagnostics, and partial trees/resources are cleaned up.
The compiler does not construct a window manager just to validate markup. OpenGL controls are inert
until explicitly initialized; see the release guide for their main-thread requirement.

XML `Assemblies`, `Namespaces` and `Backends` declarations are parsed in phases, independent of
attribute order. Comma-separated entries are trimmed. Short type names must be unambiguous; fully
qualified names or CLR XML namespace prefixes disambiguate them:

```xml
<MUIPage MxmlVersion="1"
         xmlns:app="clr-namespace:MyApp.Controls;assembly=MyApp"
         Backends="State=SettingsBackend" Namespaces="MyApp" Assemblies="MyApp">
  <app:MyWidget Name="Widget"/>
</MUIPage>
```

`Backends="Alias=TypeName"` creates an instance when needed; static classes are referenced without
construction. A declared backend implementing IDisposable belongs to its page scope and is disposed
on replacement/failure/final disposal. Objects injected through `BackendObjects` are borrowed and
never disposed by the compiler. Child pages inherit backend registrations; a matching existing alias
is reused, and new aliases are scoped locally. `GetBackend<T>(alias)` and `CompiledBackends` expose
the last successful registry without making compiler-created instances application-owned.

## Children, properties and name scopes

Normal UI child elements belong to their containing control, in declaration order. A page may contain
inline pages or `<MUIPage Source="relative.xml"/>`; `MXMLFilePath` remains an include alias.
Include-element attributes override the included root settings. Nested page declarations do not
leak into the parent scope.

Property elements use `Owner.Property`, where Owner must name the current type or a base type.
Collections accept one or many objects, with the same behavior in either case:

```xml
<MUIPage>
  <MUIPage Name="Settings">
    <MUIPage.Children><TextBox Name="Name" Height="3"/></MUIPage.Children>
  </MUIPage>
  <OptionsMenu>
    <OptionsMenu.Options>
      <MenuItem Option="First" OnSelect="{App.First}"/>
      <MenuItem Option="Second" OnSelect="{App.Second}"/>
    </OptionsMenu.Options>
  </OptionsMenu>
</MUIPage>
```

Non-UI data objects belong in explicit properties/collections. Arrays and ICollection<T> collections
are supported; the existing LockedList Add pattern is supported too. Custom controls with a UI-valued
property receive child ownership when their setter has not already assigned it. Use `Children` for
ButtonPanel rather than its read-only Buttons snapshot. Scalar property text may also contain a binding.
Grid placements use `Grid.Row="0"` and `Grid.Column="1"` after registering an appropriately configured
GridContainer factory when more than its default one cell is needed.

Names must be unique within the nearest page scope, including its ordinary nested containers.
Nested pages create new scopes. Action target `Field` searches the current scope; `Settings/Field`
crosses a named child page. `.` means the current page. Forward targets are resolved after parsing
the complete tree, so declaration order does not affect actions. Unknown/ambiguous targets fail compilation.
C# `Find<T>` retains its broader recursive lookup contract and diagnoses ambiguity.

## Values and bindings

`{App.Name}` is a live public property/method path. Intermediate properties are traversed again when
read, so replacement objects are observed. Public static members and nested static classes are allowed;
fields, indexers and private members are not markup binding endpoints. Method overloads are resolved
against the target delegate signature. Whole-value bindings are supported; arbitrary string interpolation
or C# expressions are not executed.

```xml
<TextBox Text="{Binding Path=App.Name, Mode=TwoWay}" Height="3"/>
<TextBlock Text="{Binding Path=App.Summary, Mode=OneWay}"/>
<TextBlock Text="{Binding Path=App.StartupLabel, Mode=OneTime}"/>
<TextBlock Text="{}{literal braces}" Foreground="#12ABEF"/>
```

Default mode is TwoWay for writable properties and OneWay for read-only properties/methods.
Explicit TwoWay requires a public setter. Writing a OneWay source throws; use read-only/display
controls for OneWay values. OneTime snapshots without installing a live binding. `{Null}` supplies
null to a compatible nullable/reference property. Prefix a literal opening brace with `{}`.

Conversions are invariant across system cultures. Nullable values, primitive types, enums, arrays,
delegates, Guid/TimeSpan and type converters are supported. Colors accept named colors, RGB/ARGB
comma-separated bytes, `#RRGGBB` or `#AARRGGBB`. Invalid/range errors are diagnostics. Host-specific
conversions can be registered with `page.TypeExtensions[typeof(MyType)]`.

INotifyPropertyChanged and INotifyCollectionChanged subscriptions invalidate their UI owner and rewire
when intermediate objects change. Notifications from background code dispatch to the UI owner.
Detach suspends subscriptions; reattach resumes once; replace/dispose releases them. Plain/static
property sources and conservative nested-path cases retain polling. Callback properties read their
current delegate when invoked and do not force idle repainting. Direct C# binding getter/setter errors
are observable rather than silently falling back to stale values.

## Callbacks, events and actions

Delegate properties such as Button.OnPressed and TextBox.OnEnter bind to compatible public backend
methods/delegates. CLR events such as RgbColorPicker.Confirmed work the same way:

```xml
<RgbColorPicker Confirmed="{App.SaveColor}"/>
```

Conversely, a public control method can handle a backend event:
`<MyWidget OnTick="{App.Tick}"/>`. Both directions are UI-owned subscriptions and suspend on detach.
Background events dispatch to the owning window. Callback parameters are preserved. Fire-and-forget
`async void` is not an awaited UI operation; run asynchronous host work outside the UI callback and
queue its completion explicitly.

Ordered actions append to an existing callback rather than replacing it. The bound callback runs first;
if it fails, subsequent actions do not run. Action blocks require void delegate/event signatures:

```xml
<Button Text="Save" OnPressed="{App.Save}">
  <Button.OnPressed>
    <Invoke Path="App.Refresh"/>
    <Set Target="Result" Property="Text" Value="Saved"/>
    <Show Target="Result" Focus="false"/>
    <Hide Target="Editor"/>
  </Button.OnPressed>
</Button>
```

`Show`, `Hide`, `Toggle`, `Focus`, `Set` and parameterless `Invoke` are supported. Show defaults to
focusing eligible content; Toggle defaults to no focus. Set validates the target property and value
during compilation. Actions execute in order, including repeated operations, and do not act on disposed
or detached old document targets. An optional `<Actions>` wrapper groups them.

The shorthand uses the same implementation:

```xml
<Button.OnPressed MXMLFlags="Show(Result:false),Hide(Editor),Toggle(Help),Focus(Name)"/>
```

Commas/semicolons separate ordered flags. Show/Toggle optionally accept `:true`/`:false` or a second
comma argument. Unknown flags, empty arguments and invalid booleans are errors. Flags belong on the
callback property element, not on every control attribute. Implicitly embedding UI inside a callback
is removed: declare the target normally and use Show, or create an owned popup in backend C#.

## Generated properties

```csharp
public partial class MyWidget : UIElement
{
    [MelonUI.Attributes.Binding]
    private string _caption = "Ready"; // Generates public Caption.
    protected override void RenderContent(ConsoleBuffer buffer) { /* use Caption */ }
}
```

The analyzer is included in the MelonUI NuGet package. It targets netstandard2.0 while the runtime
targets .NET 10. Mutable instance fields in partial classes are supported; multiple fields, qualified
attribute names/aliases, global/nested/generic classes and same simple names in different namespaces
are covered. Generated types use fully qualified names and do not rely on consumer usings.

The first letter after leading underscores becomes uppercase. Existing/inherited/generated name
collisions are rejected. Unsupported properties/events, readonly/static fields, nonpartial containers
and reserved helper collisions receive MUIB001–MUIB005 diagnostics rather than crashing the generator.
Non-UI partial data classes receive IBindingHost storage; compiler-owned subscriptions still belong
to their containing UI element. Applications using those data classes directly should remove bindings
when no longer needed. Reflection/dynamic-code MXML and arbitrary plugins are not trimming/NativeAOT
contracts in 1.0; use an untrimmed normal .NET deployment.
