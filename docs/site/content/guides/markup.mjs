export function markup(h) {
  const {p,code,file,recipe,list,subheading}=h;
  const s=(id,title,html)=>({id,title,html});
  return [
    {slug:'mxml',title:'MXML Guide',group:'MXML',summary:'MXML is an XML-based markup language used to create MelonUI pages. It describes the same elements, properties and callbacks that you use when building the UI in C#.',sections:[
      s('document','Page Structure',p('The root element must be MUIPage. You can set MxmlVersion="1" to explicitly select the language version; unsupported versions fail compilation.')+code('xml',file('Settings.mxml'),'Settings.mxml')+p('Core types in MelonUI.Base and MelonUI.Default are available without namespace declarations. Attributes set public CLR properties. Normal XML escaping applies, such as &amp;amp; for an ampersand and &amp;#10; for a line break inside an attribute.')),
      s('host','Compiling a Page',p('Register any application objects that the markup uses, then compile the file:')+code('csharp',`var page = new MUIPage();
page.BackendObjects.Add("App", new SettingsModel());
if (!page.CompileFile("Settings.mxml"))
    throw new InvalidOperationException(page.GetSimpleCompilerDisplay());`)+p('BackendObjects refers to existing objects that your application owns. The compiler does not dispose those objects. Backends declared in markup with Backends="Alias=TypeName" are created when needed and are owned by their page scope. A disposable backend created this way is cleaned up after failure, replacement or final page disposal.')+p('Child pages inherit the available aliases. New declarations remain inside the child scope. Static backend classes can also be referenced without constructing an instance.')),
      s('children','Children and Property Elements',code('xml',`<MUIPage ShowBorder="false">
  <MUIPage Name="Details" Width="50%">
    <MUIPage.Children>
      <TextBlock Text="Nested page" Height="1" ShowBorder="false"/>
    </MUIPage.Children>
  </MUIPage>
  <OptionsMenu X="50%" Width="50%">
    <OptionsMenu.Options>
      <MenuItem Option="Albums"/>
      <MenuItem Option="Artists"/>
    </OptionsMenu.Options>
  </OptionsMenu>
</MUIPage>`)+p('UI elements belong to the element containing them, in declaration order. Data objects such as MenuItem and WorkItem go inside an explicit collection property instead of Children.')+p('Property elements use Owner.Property, where Owner is the current type or one of its base types. Collections can contain one or more objects. A scalar property element can contain text or a binding.')),
      s('includes','Including Other Pages',code('xml',`<MUIPage ShowBorder="false">
  <MUIPage Name="Settings" Source="pages/settings.mxml" Width="60%"/>
  <Button Text="Focus name" X="60%" Width="40%">
    <Button.OnPressed><Focus Target="Settings/Name"/></Button.OnPressed>
  </Button>
</MUIPage>`)+p('Relative includes are resolved beside the file containing them. Attributes on the include element override settings from the included page. MXMLFilePath is also accepted as an include alias.')+p('Compile(text, sourcePath) compiles the text you supply without rereading the file. sourcePath provides the directory used for relative includes. Without a source path, CompilerOptions.BaseDirectory is used, followed by the process working directory. Compiling never changes that working directory.')+p('Recursive includes and documents exceeding the configured size/depth limits are reported as errors.')),
      s('scopes','Element Names',p('Names must be unique within the nearest page, including ordinary nested containers. A nested MUIPage creates a new scope.')+list(['<strong>Name:</strong> Looks for an element inside the current page scope.','<strong>Settings/Name:</strong> Looks for Name inside the named Settings child page.','<strong>.:</strong> Refers to the current page.'])+p('Action targets can refer to elements declared later in the file. They are resolved after the page has been parsed.')),
      s('custom-types','Custom Types and Constructors',code('csharp',`page.CompilerOptions.Assemblies.Add(typeof(Badge).Assembly);
page.CompilerOptions.Namespaces.Add("MelonUI.Docs");
page.CompilerOptions.Factories.Add(typeof(GridContainer),
    () => new GridContainer(2, false, 2, 1));`)+p('Normally the compiler uses a public parameterless constructor. You can register a factory when a type needs constructor arguments or services from the application. The factory must return a new, unattached and undisposed instance of the requested type.')+p('Assemblies, Namespaces and Backends declarations in MXML are handled in phases, so their attribute order does not matter. Fully qualified names or CLR XML namespace prefixes can be used when short type names would be ambiguous.')),
      s('reload','Reloading MXML',p('The compiler builds a new page before replacing the current one. This allows an application to keep showing a working UI while a file is being edited:')+code('csharp',recipe('reload'))+list(['<strong>Compilation Fails:</strong> The current children, root settings, bindings and focus are kept. Read the diagnostics to show the error.','<strong>Compilation Succeeds:</strong> Children are replaced and the old owned elements and bindings are disposed. A uniquely named eligible focus target is restored.','<strong>Root Properties:</strong> Properties included in the new markup replace their previous values. Omitted root settings remain unchanged.'])+p('While the window is running, compile on the UI thread using Post or InvokeAsync. Read the file outside that action. Custom setters and lifetime methods should not throw or cause external changes during compilation; normal rollback cannot undo arbitrary application behavior.')),
      s('notes','Notes',p('MXML is intended for trusted application and plugin code. It can create registered classes, call backends and include local files, so it should not be treated as a sandbox for untrusted input. DTDs and external XML entities are prohibited.')+p('See <a href="@data-binding">Data Binding</a>, <a href="@mxml-actions">MXML Actions</a>, and <a href="@diagnostics">Diagnostics and Troubleshooting</a> for more information.'))
    ]},
    {slug:'data-binding',title:'Data Binding',group:'MXML',summary:'Data binding connects an element property or callback to an object in your application. This allows controls to display and update data without manually copying every value between the model and UI.',sections:[
      s('model','Creating a Backend',p('A backend can be a normal class with public properties and methods. This example uses INotifyPropertyChanged so the UI can be told when a value changes:')+code('csharp',file('SettingsModel.cs'),'SettingsModel.cs')+p('Register the instance before compiling:')+code('csharp','page.BackendObjects.Add("App", model);')+p('Name can be read and written. Status has no public setter, so it is read-only to MXML. Save, Submit and the color/file methods are callbacks that controls can call.')),
      s('modes','Binding Properties',code('xml',`<MUIPage ShowBorder="false">
  <TextBox Text="{Binding Path=App.Name, Mode=TwoWay}" Height="3"/>
  <TextBlock Text="{Binding Path=App.Status, Mode=OneWay}" Y="3" Height="1" ShowBorder="false"/>
  <TextBlock Text="{Binding Path=App.Name, Mode=OneTime}" Y="4" Height="1" ShowBorder="false"/>
</MUIPage>`)+list(['<strong>TwoWay:</strong> Reads and writes a public writable property. This is the default when the source has a public setter.','<strong>OneWay:</strong> Reads the value without writing back. This is the default for read-only properties and methods. Trying to write through a OneWay binding throws an error.','<strong>OneTime:</strong> Copies the value during compilation without installing a live binding.'])+p('The shorter form {App.Name} selects the default mode. Bindings apply to the whole value; they do not evaluate arbitrary C# expressions or interpolate a binding into the middle of a string.')+p('Binding endpoints must be public properties, methods or events. Fields, indexers and private members are not supported.')),
      s('callbacks','Callbacks and Events',code('xml',`<MUIPage ShowBorder="false">
  <TextBox OnEnter="{App.Submit}" Height="5"/>
  <RgbColorPicker Y="5" Height="8" ColorValue="{App.Accent}"
                  ColorChanged="{App.PreviewColor}" Confirmed="{App.SaveColor}"/>
</MUIPage>`)+p('Callback methods must match the delegate or event signature. For example:')+code('csharp',`public void Submit(string text, TextBox input) { /* handle input */ }
public void SaveColor(System.Drawing.Color color) { /* save the color */ }`)+p('Delegate properties call compatible public methods or the current delegate value. CLR control events can call a backend handler. The reverse also works: a public control method can handle a backend event. Overloaded methods are resolved against the expected delegate signature.')+p('A background event is dispatched to the attached window\'s UI thread. Do not use async void callbacks as a substitute for awaited application work.')),
      s('notifications','Updating Bound Values',p('INotifyPropertyChanged and INotifyCollectionChanged allow MelonUI to invalidate controls when their data changes. Nested paths are followed again when an intermediate object is replaced. For App.Profile.Name, notify Profile when replacing that object, and notify Name on its new instance when the name changes.')+p('Background notifications queue UI invalidation, but this does not make your model or ordinary lists thread-safe. Manage changes to shared state deliberately, normally on the UI thread.')+p('Plain/static sources and some nested paths use polling. Subscriptions suspend when a control is detached, resume once when it is attached again, and are released after replacement or disposal.')),
      s('csharp','Binding from C#',code('csharp',recipe('binding'))+p('CreatePath accepts the source instance, a member path and an optional target type. A Type can be supplied for static members. There are also constructors taking PropertyInfo, MethodInfo or EventInfo.')+p('Generated setters write through an installed binding. RemoveBinding removes and disposes that binding.')),
      s('conversion','Values and Conversion',code('xml',`<MUIPage ShowBorder="false">
  <TextBlock Text="{}{literal braces}" Foreground="#12ABEF" Height="1" ShowBorder="false"/>
  <TextBlock Text="{Null}" Y="1" Height="1" ShowBorder="false"/>
  <Waveform WaveformData="{App.Peaks}" Y="2" Height="7"/>
</MUIPage>`)+p('Conversions use invariant culture, so decimal values use a period. Supported values include primitives, enums, nullable values, Guid, TimeSpan, colors, compatible typed arrays and type converters.')+list(['<strong>{Null}:</strong> Supplies null to a compatible reference or nullable property.','<strong>{}:</strong> Escapes a literal opening brace.','<strong>Arrays:</strong> Bind a typed array such as App.Peaks; a comma-separated string does not automatically create it.','<strong>TypeExtensions:</strong> Register a host conversion with page.TypeExtensions[typeof(MyType)] when a custom type needs one.'])+p('Errors from direct C# binding getters and setters remain visible to the application rather than silently using an old value.'))
    ]},
    {slug:'mxml-actions',title:'MXML Actions',group:'MXML',summary:'MXML actions allow a callback to show, hide, focus or update elements without writing a separate method for each operation. Actions are validated during compilation and run in the order they are declared.',sections:[
      s('ordered','Example Usage',code('xml',`<MUIPage ShowBorder="false">
  <Button Text="Save" OnPressed="{App.Save}" Height="3">
    <Button.OnPressed>
      <Set Target="Result" Property="Text" Value="Saved"/>
      <Show Target="Result" Focus="false"/>
    </Button.OnPressed>
  </Button>
  <TextBlock Name="Result" Y="3" Height="1" ShowBorder="false" IsVisible="false"/>
</MUIPage>`)+subheading('Understanding the Code')+list(['<strong>OnPressed:</strong> Calls App.Save first.','<strong>Set:</strong> Changes the Result text to Saved.','<strong>Show:</strong> Makes Result visible without moving focus.'])+p('If the bound callback throws, later actions are not run. Repeated actions are allowed and keep their declaration order. Targets can be declared later in the file.')),
      s('available','Available Actions',list(['<strong>Show:</strong> Takes Target and an optional Focus value, which defaults to true.','<strong>Hide:</strong> Takes Target.','<strong>Toggle:</strong> Takes Target and an optional Focus value, which defaults to false.','<strong>Focus:</strong> Takes Target.','<strong>Set:</strong> Takes Target, Property and Value. The property and conversion are checked during compilation.','<strong>Invoke:</strong> Takes Path, pointing to a public parameterless backend method.'])+p('An Actions wrapper can be used to group actions. Blocks require a void-returning callback or event. Targets use the current page scope, with paths such as Settings/Name for named child pages. Actions from a replaced document will not operate on its detached or disposed targets.')),
      s('flags','MXMLFlags',p('For short sequences, MXMLFlags provides a compact version of the same actions:')+code('xml','<Button.OnPressed MXMLFlags="Show(Result:false),Hide(Editor),Toggle(Help),Focus(Name)"/>')+p('Place this property element inside a Button, with Result, Editor, Help and Name declared in its page. Commas or semicolons separate the flags. Show and Toggle accept an optional :true/:false or second comma argument.')+p('Flags append after an existing callback rather than replacing it. Unknown flags, empty arguments and invalid boolean values are compilation errors.')),
      s('notes','Notes',p('Declare UI elements normally, then use Show or Hide to change them. UI declarations cannot be embedded inside a callback. To create a modal popup, call ConsoleWindowManager.ShowPopup from a backend method.'))
    ]}
  ];
}
