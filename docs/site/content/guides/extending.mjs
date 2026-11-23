export function extending(h) {
  const {p,code,pair,table,terminal,file,recipe,list,subheading}=h;
  const s=(id,title,html)=>({id,title,html});
  return [
    {slug:'custom-controls',title:'Creating Custom Controls',group:'Custom Controls',summary:"In this guide, we'll create a UIElement-derived control with a caption, active state and keyboard callback. We'll then use it in both C# and MXML.",sections:[
      s('prerequisites','Prerequisites',list(['A .NET 10 project referencing MelonUI.','The binding generator referenced by the project containing your annotated fields. See <a href="@generator">Binding Generator</a> for setup.','A basic understanding of the <a href="@uielement">UIElement</a> properties and the <a href="@consolebuffer">ConsoleBuffer</a>.'])),
      s('badge','1. Create the Control Class',p('Add the following Badge class to your project:')+code('csharp',file('Badge.cs'),'Badge.cs')+subheading('Understanding the Code')+list(['<strong>partial class:</strong> Allows the binding generator to add properties to the class.','<strong>Binding Fields:</strong> Generate Caption, Active and OnActivated. These properties use the base binding and invalidation behavior.','<strong>ContentRect:</strong> Gives the usable area inside the border. A bordered control with Height=3 has one content row.','<strong>RenderContent:</strong> Draws the active marker and caption, clipping by text elements so long text does not overwrite the border.','<strong>TryHandleKey:</strong> Toggles Active on Enter, calls OnActivated, and returns true to mark the key as handled.'])+p('After Enter changes the active state, the output looks like this:')+terminal('badge')),
      s('use-csharp','2. Add the Control in C#',code('csharp',`var badge = new MelonUI.Docs.Badge { Caption = "Ready", Active = false };
badge.OnActivated = () => status.Text = badge.Active ? "Active" : "Inactive";
page.Children.Add(badge);`)+p('The generated setters already invalidate the element. You do not need to call Invalidate again after assigning Caption or Active. State that is not handled by those setters should invalidate the control explicitly.')),
      s('use-mxml','3. Use the Control in MXML',p('Register the assembly and namespace before compiling the page:')+code('csharp',`page.CompilerOptions.Assemblies.Add(typeof(MelonUI.Docs.Badge).Assembly);
page.CompilerOptions.Namespaces.Add("MelonUI.Docs");`)+code('xml',`<MUIPage ShowBorder="false">
  <Badge Caption="Custom control" Active="true" OnActivated="{App.Save}"/>
</MUIPage>`)+p('App.Save is a public parameterless method on your registered backend. If two types have the same short name, use a CLR XML namespace prefix. For this example that would be xmlns:docs="clr-namespace:MelonUI.Docs;assembly=MelonUI.DocsExamples", followed by docs:Badge. Replace the assembly name with the name of your own assembly.')+p('If the control needs constructor arguments, register a factory through CompilerOptions.Factories.')),
      s('lifetime','4. Manage Subscriptions and Resources',p('A control may be removed and reused without being disposed. Subscribe to window events while attached and remove those subscriptions when detached:')+code('csharp',`protected override void OnAttachedToWindow(ConsoleWindowManager window)
{
    window.Updating += OnUpdate;
    base.OnAttachedToWindow(window);
}
protected override void OnDetachedFromWindow(ConsoleWindowManager window)
{
    window.Updating -= OnUpdate;
    base.OnDetachedFromWindow(window);
}
// OnUpdate is a short EventHandler<UiUpdateEventArgs>.
// DisposeCore releases permanent resources, then calls base.DisposeCore().`)+p('Stop or cancel window-specific work on detach, while keeping the object reusable. DisposeCore is for final resource cleanup. Keep lifetime methods synchronous and avoid throwing from them during cleanup.')),
      s('checklist','5. Build and Test',p('A control should be checked in more than its initial layout. Test the following:')+list(['<strong>Small Sizes:</strong> One row, no content area, clipped layouts, nested parents and repeated resizing.','<strong>Input:</strong> Modifier keys, disabled/hidden/locked state, unhandled keys, focus navigation and modals.','<strong>Bindings:</strong> Property changes, callbacks, detach/reattach, document replacement and disposal.','<strong>Drawing:</strong> Borders, transparent backgrounds, Unicode text and cache invalidation.','<strong>Lifetime:</strong> Repeated reuse without duplicate subscriptions or abandoned background work.'])+p('Custom containers also need to lay out and draw their children. If the standard page behavior is enough, derive from or use MUIPage instead.'))
    ]},
    {slug:'generator',title:'Binding Generator',group:'Custom Controls',summary:'The binding generator adds public binding-aware properties for annotated fields. This allows a custom control to expose properties to C# and MXML without writing the same getter and setter code for each one.',sections:[
      s('setup','Setting Up the Generator',p('The MelonUI NuGet package includes the analyzer. If you are using source project references, add an analyzer reference in the project that declares the annotated fields:')+code('xml',`<ItemGroup>
  <ProjectReference Include="../MelonUI/MelonUI.csproj"/>
  <ProjectReference Include="../MelonUIBindingGenerator/MelonUIBindingGenerator.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
</ItemGroup>`)+p('Adjust the paths relative to your .csproj. The generator targets netstandard2.0 because it runs as a build tool; MelonUI and the application target .NET 10.')),
      s('fields','Example Usage',p('Add the Binding attribute to a mutable instance field in a partial class:')+code('csharp',`public partial class Badge : UIElement
{
    [MelonUI.Attributes.Binding] private string _caption = "Ready";
    // Add RenderContent as shown in the complete custom control example.
}`)+p('This generates a Caption property. Its behavior is equivalent to the following simplified code; you do not add this property yourself:')+code('csharp',`public string Caption
{
    get => (string)GetBoundValue(nameof(Caption), _caption)!;
    set => SetBoundValue(nameof(Caption), value, ref _caption);
}`)+p('Leading underscores are removed, then the first remaining letter is capitalized. For example, _caption becomes Caption, _IsVisible becomes IsVisible, and onActivated becomes OnActivated.')),
      s('rules','Supported Fields and Classes',list(['Fields must be mutable instance fields. Static, readonly and const fields are not supported.','The class and every containing class must be nonstatic partial classes.','A generated property cannot share a name with another generated, existing or inherited property.','Do not declare binding helper members that conflict with the generated helpers. UIElement already provides these methods.'])+p('Multiple fields, nested/generic classes, qualified attributes and attribute aliases are supported. Classes in different namespaces can have the same short name.')+p('Using the fully qualified MelonUI.Attributes.Binding attribute avoids ambiguity with the MelonUI.Base.Binding class. MXML binds to the generated public property, not the field.')),
      s('diagnostics','Compiler Diagnostics',table(['Code','Description'],[['MUIB001','The attribute was applied to an unsupported declaration. Use a mutable field rather than a property or event.'],['MUIB002','The class or one of its containing classes is not a nonstatic partial class.'],['MUIB003','The field is static, readonly or const, so it cannot be generated.'],['MUIB004','The generated property name conflicts with an existing, inherited or generated member.'],['MUIB005','The type declares reserved binding helpers. Remove the conflicting helpers or derive from UIElement.']])),
      s('models','Using the Generator on Data Classes',p('A non-UI partial data class can also receive IBindingHost storage. Generated setters support binding, but they do not automatically implement INotifyPropertyChanged. Add notifications yourself when you want consumers to be invalidated efficiently.')+p('If you install bindings directly on a non-UI object, remove them when they are no longer needed. UIElement normally handles that lifetime for its own bindings.'))
    ]},
    {slug:'consolebuffer',title:'ConsoleBuffer',group:'Custom Controls',type:'ConsoleBuffer',summary:'ConsoleBuffer holds the characters and colors used to draw a control. A custom UIElement receives its buffer in RenderContent and writes into it using local coordinates.',sections:[
      s('drawing','Drawing Text and Cells',code('csharp',`protected override void RenderContent(ConsoleBuffer buffer)
{
    var a = ContentRect;
    if (a.Width == 0 || a.Height == 0) return;
    buffer.FillRect(a.X, a.Y, a.Width, a.Height, ' ', Foreground, Background);
    buffer.WriteString(a.X, a.Y, "Ready", Foreground, Background);
}`)+p('ContentRect excludes the control\'s border. Drawing methods are bounded by the buffer, but you should also clip content to ContentRect if it must not overwrite the border. The <a href="@custom-controls">Badge example</a> shows text-element clipping.')),
      s('methods','Drawing Methods',list(['<strong>SetPixel:</strong> Sets the character, foreground and background of a cell.','<strong>WriteString / WriteStringWrapped:</strong> Writes a string directly or wraps it to a width.','<strong>FillRect / DrawRect:</strong> Fills an area or draws a box.','<strong>WriteBuffer:</strong> Combines a child buffer at a local position. respectBackground defaults to true.','<strong>CreateSubBuffer:</strong> Creates a buffer from a bounded subregion.','<strong>GetStringWidth / GetTextElementWidth:</strong> Measures best-effort terminal cell widths.','<strong>Screenshot:</strong> Returns text output, with optional color escape sequences.'])+p('The buffer indexer uses [row, column], while drawing methods use (x, y). ConsoleWindowManager handles presenting the frame; a custom control should not call RenderToConsole itself.')),
      s('capture','Capturing a Render',code('csharp',recipe('capture'))+p('The output examples in these docs use a 64 by 12 window capture, without ANSI colors. Trailing whitespace is removed. FilePicker\'s directory caption is replaced with /demo so it does not show a machine-specific path.')+p('Display widths and shaping for combining characters and emoji can still depend on the terminal and font.'))
    ]},
    {slug:'diagnostics',title:'Diagnostics and Troubleshooting',group:'Reference',summary:'MelonUI reports build-time generator errors, MXML compilation errors and runtime UI errors separately. This page explains where to find those errors and some common causes.',sections:[
      s('compile','MXML Compilation Errors',code('csharp',`if (!page.Compile(markup, sourcePath))
{
    var result = page.CompilerFocusedMessage;
    // Code, SourcePath, MxmlLineNumber.Line / Position, Message, Severity
    status.Text = page.GetSimpleCompilerDisplay(maxWidth: 100);
}`)+list(['<strong>CompilerMessages:</strong> All messages produced by the last compilation.','<strong>CompilerFocusedMessage:</strong> The current result, including the source path and XML line/column.','<strong>CompilationFinished:</strong> The elapsed compilation time.','<strong>MXML / MXMLFilePath:</strong> The last successfully compiled document. A failed edit does not replace these.'])+p('GetSimpleCompilerDisplay formats the messages without writing to the console.')),
      s('symptoms','Common Problems',table(['Problem','Things to Check'],[['Content is blank or overlapping','Check X/Y, Height, visibility and the content space left by borders. Pages do not automatically stack children.'],['A labeled TextBox has no input row','Use Height="5" or larger when a border is shown.'],['A grid cell is out of range','Register a GridContainer factory with the intended constructor row and column counts.'],['A generated property is missing','Check that the field is mutable, its class is partial, and the analyzer is referenced by that project.'],['A bound value does not update','Check the public property path, backend alias, notifications and any caching/polling requirements.'],['An included file cannot be found','Supply sourcePath or CompilerOptions.BaseDirectory. Includes are relative to the file containing them.'],['Changing an attached control throws','Queue the change with Post or InvokeAsync while RunAsync is active. Stop producers during shutdown.'],['RunAsync requires an interactive terminal','Avoid redirected input/output, or use CaptureText for headless rendering.'],['FilePicker still shows the previous filter','Call Refresh after changing Extensions.'],['OpenGL initialization fails','Call Init on the application main thread before awaiting RunAsync, and check graphics support.']])),
      s('runtime','Runtime Errors',p('Compile returns false for expected markup errors. Once the page is running, failures in callbacks or bindings use the normal runtime error handling.')+p('InvokeAsync faults its returned task. Unexpected posted, keyboard and frame/update errors reach UnhandledException and otherwise fault RunAsync after terminal cleanup. Only set Handled=true if the application can safely continue.'))
    ]},
    {slug:'build-release',title:'Building MelonUI',group:'Reference',summary:'MelonUI targets .NET 10, with the binding generator targeting netstandard2.0. The repository has separate build entry points for the full solution and the portable core.',sections:[
      s('build','Building and Running Tests',code('shell',`# Windows: full solution, including optional shaders and demos.
dotnet build MelonUIDemo.sln -c Release --warnaserror
dotnet run --project MelonUI.Tests -c Release --no-build -- --gpu --soak

# Linux/macOS: core, generator and tests.
dotnet build MelonUI.slnf -c Release --warnaserror
dotnet run --project MelonUI.Tests -c Release --no-build -- --soak`)+p('MelonUI.Tests is an executable regression runner rather than a dotnet test adapter. --soak runs 500 MXML reloads to check cleanup. --gpu adds a Windows hardware test.')+p('The full shader source build currently requires Windows because ComputeSharp\'s source generator invokes native Windows compiler APIs.')),
      s('packages','Building Local Packages',code('shell',`pwsh ./scripts/Verify-Packages.ps1
pwsh ./scripts/Verify-Packages.ps1 -CoreOnly`)+p('This script builds local 1.0.0 packages and checks them from separate application/plugin consumers. It does not publish to NuGet, create a Git tag or make a release.')+p('The core runtime depends on Pastel and Wcwidth. Its package includes the binding analyzer. MelonUI.Shaders has the optional graphics dependencies.')),
      s('limits','Platform and Runtime Information',list(['<strong>Runtime:</strong> Normal untrimmed/JIT .NET 10. Reflection-based MXML and arbitrary plugins are not supported as trimming/NativeAOT deployments in 1.0.','<strong>Platforms:</strong> Windows and Linux have been checked locally. Native macOS and more terminal/SSH environments still need hands-on testing.','<strong>File Browsing:</strong> Local directory enumeration is synchronous and is not a sandbox.','<strong>Text:</strong> Console widths are best-effort; some shaping depends on the terminal and font.','<strong>Graphics:</strong> Optional controls have device and main-thread requirements. Normal controls do not need a GPU.','<strong>Removed Prototypes:</strong> The image viewer, audio player, decoders and playback managers are not part of the core. Waveform takes peaks supplied by the host.'])+p('The library uses the MIT license. See LICENSE and THIRD-PARTY-NOTICES.md in the repository for licensing and dependency notices. The docs describe the current source and local packages, rather than claiming that a public package release has been made.')),
      s('site','Building the Documentation',p('From docs/site in the repository:')+code('shell',`node scripts/prepare-examples.mjs
dotnet run --project examples/DocsExamples.csproj -c Release -- .
node scripts/build.mjs
node scripts/check.mjs
node scripts/serve.mjs`)+p('There are no npm dependencies. The .NET example runner refreshes the API list and control output examples. The Node build creates static HTML in dist, which can be served by a static host, including from a subfolder.')+p('The guide index is content/guides.mjs, with individual guides in content/guides/. Control examples are in content/controls.mjs. See content/STYLE.md for the writing and layout conventions used by these docs.'))
    ]},
    {slug:'shaders',title:'Optional Shader Controls',group:'Reference',summary:'MelonUI.Shaders contains OpenGLShaderElement and ShaderDisplay. These are optional graphics controls and are not required for the normal console UI library.',sections:[
      s('opengl','OpenGLShaderElement',p('OpenGLShaderElement renders a GLSL shader. Constructing or compiling the control does not create a graphics context. Call Init on the application main thread before awaiting the UI loop:')+pair(code('csharp',`using MelonUI.Shaders;
var shader = new OpenGLShaderElement { ShaderPath = "effect.glsl" };
page.Children.Add(shader);
window.Elements.Add(page);
shader.Init();
await window.RunAsync();`),code('xml',`<MUIPage ShowBorder="false" Assemblies="MelonUI.Shaders" Namespaces="MelonUI.Shaders">
  <OpenGLShaderElement Name="Shader" ShaderPath="effect.glsl"/>
</MUIPage>`))+p('After compiling the MXML, use page.Find&lt;OpenGLShaderElement&gt;("Shader")!.Init() before awaiting RunAsync. ShaderSource can supply the GLSL directly instead of a file. Shader file loading is separate from MXML includes; provide a host path that can be read. ErrorMessage describes initialization or rendering failures.')+p('Actual output depends on the shader, GPU and driver. The repository shader demo includes a complete shader and startup/resize checks.')),
      s('compute','ShaderDisplay',p('ShaderDisplay uses Windows/Direct3D compute. It allocates its resources when needed and displays its built-in gradient unless you supply another shader:')+pair(code('csharp',`using MelonUI.Shaders;
var display = new ShaderDisplay();
page.Children.Add(display);
// Use UpdateShader<T>(texture => shader) to supply a typed ComputeSharp shader.`),code('xml',`<MUIPage ShowBorder="false" Assemblies="MelonUI.Shaders" Namespaces="MelonUI.Shaders">
  <ShaderDisplay Name="Compute"/>
</MUIPage>`))+p('UpdateShader&lt;T&gt; takes a factory receiving ReadWriteTexture2D&lt;float4&gt;. T must be a struct implementing IComputeShader and IComputeShaderDescriptor&lt;T&gt;. Check ErrorMessage when the device or shader cannot run.')),
      s('lifetime','Lifetime and Testing',p('Workers publish complete frames together with their dimensions. During resizing, the previous completed frame is clipped using its own size. Detach pauses reusable graphics activity, while final disposal joins workers and releases native resources.')+code('shell','dotnet run --no-launch-profile --project MelonUIDemo -- --shader --smoke'))
    ]}
  ];
}
