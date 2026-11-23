using System.ComponentModel;
using System.Drawing;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;

internal static class MxmlTests
{
    internal static IEnumerable<(string Name, Func<Task> Run)> All =>
    [
        ("MXML declarations are order independent and default namespaces work", Declarations),
        ("MXML nesting and single/multiple collection members retain their owners", Hierarchy),
        ("failed MXML reload preserves live children, root settings and focus", Transaction),
        ("MXML properties support one/two-way/one-time conversion and notifications", Properties),
        ("MXML source events suspend, reload once and clean up after failure", Events),
        ("compiler-created backends are disposed; injected backends stay borrowed", Resources),
        ("relative includes preserve scope and detect cycles and depth limits", Includes),
        ("ordered repeated flags compose with callbacks and validate forward targets", Flags),
        ("typed actions compose with parameterized callbacks and page-qualified targets", Actions),
        ("MXML conversion is invariant, nullable, color-safe and literal-escapable", Conversion),
        ("invalid MXML returns source diagnostics without terminal access", Diagnostics),
        ("MXML factories support custom constructors and clean up partial trees", Factories),
        ("background property notifications dispatch onto the UI owner", BackgroundNotification),
        ("idle cached pages stop terminal output and repaint after captured changes/removal", IdleOutput),
        ("CLR control events bind typed callbacks/actions and suspend on detach", ControlEvents),
        ("nested property/event bindings follow replaced objects and release old subscriptions", NestedBindings),
        ("draft assignments reject duplicates without writing through backend bindings", DraftBindings)
    ];
    private static void Check(bool value, string message = "Assertion failed") { if (!value) throw new Exception(message); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
    private static MUIPage Page()
    {
        var page = new MUIPage { ShowBorder = false };
        page.CompilerOptions.Assemblies.Add(typeof(MxmlFixtures.TestBackend).Assembly);
        page.CompilerOptions.Namespaces.Add("MxmlFixtures"); return page;
    }
    private static void Compile(MUIPage page, string markup) => Check(page.Compile(markup), page.GetSimpleCompilerDisplay(200));
    private static ConsoleWindowManager Window() => new(new FakeTerminal());
    private static Task Declarations()
    {
        using var page = Page();
        Compile(page, "<MUIPage Backends='TestBackend' Namespaces=' MxmlFixtures , MelonUI.Default ' Assemblies=' MelonUI.Tests , MelonUI '><TextBlock Name='Text' Text='{TestBackend.Text}'/></MUIPage>");
        Equal("initial", page.Find<TextBlock>("Text")!.Text);
        Compile(page, "<MUIPage><Button/><FilePicker/><GridContainer/><Waveform/></MUIPage>"); Equal(4, page.Children.Count);
        return Task.CompletedTask;
    }
    private static Task Hierarchy()
    {
        using var page = Page();
        Compile(page, """
            <MUIPage><MUIPage Name="Nested"><MUIPage.Children><TextBox Name="Input"/></MUIPage.Children></MUIPage>
              <OptionsMenu Name="Menu"><OptionsMenu.Options><MenuItem Option="First"/><MenuItem Option="Second"/></OptionsMenu.Options></OptionsMenu>
              <TextBlock Name="Keys"><TextBlock.KeyboardControls><KeyboardControl Key="A"/></TextBlock.KeyboardControls></TextBlock>
            </MUIPage>
            """);
        Equal(3, page.Children.Count); var nested = page.Find<MUIPage>("Nested")!;
        Equal(1, nested.Children.Count); Check(nested.Children[0].Parent == nested);
        Equal(2, page.Find<OptionsMenu>("Menu")!.Options.Count); Equal(1, page.Find<TextBlock>("Keys")!.KeyboardControls.Count);
        return Task.CompletedTask;
    }
    private static async Task Transaction()
    {
        await using var window = Window(); using var page = Page();
        Compile(page, "<MUIPage Name='Old' Width='80%'><TextBox Name='Input' Text='retained'/></MUIPage>"); window.Elements.Add(page);
        window.CaptureText(width: 80, height: 20); var input = page.Find<TextBox>("Input")!; window.Focus(input);
        Check(!page.Compile("<MUIPage Name='New' Width='10'><TextBox/><NotAControl/></MUIPage>"));
        Equal("Old", page.Name); Equal("80%", page.Width); Check(!input.IsDisposed && input.Parent == page && window.FocusedElement == input);
        Compile(page, "<MUIPage><TextBox Name='Input' Text='replacement'/></MUIPage>");
        Check(input.IsDisposed && window.FocusedElement == page.Find<TextBox>("Input"));
        Equal("replacement", page.Find<TextBox>("Input")!.Text);
    }
    private static async Task Properties()
    {
        await using var window = Window(); using var page = Page(); var backend = new MxmlFixtures.TestBackend(); page.BackendObjects.Add("App", backend);
        Compile(page, "<MUIPage><TextBox Name='Input' Text='{App.Text}'/><TextBlock Name='Once' Text='{Binding Path=App.Text, Mode=OneTime}'/></MUIPage>");
        window.Elements.Add(page); window.CaptureText(width: 50, height: 10); var input = page.Find<TextBox>("Input")!;
        Check(backend.Subscribers > 0); input.Text = "edited"; Equal("edited", backend.Text);
        backend.Text = "external"; Equal("external", input.Text); Equal("initial", page.Find<TextBlock>("Once")!.Text);
        page.Remove(); Equal(0, backend.Subscribers); window.Elements.Add(page); Check(backend.Subscribers > 0);
        page.Dispose(); Equal(0, backend.Subscribers);
    }
    private static async Task Events()
    {
        await using var window = Window(); using var page = Page(); var events = new MxmlFixtures.TestBackend(); page.BackendObjects.Add("App", events);
        const string markup = "<MUIPage><TickElement Name='Tick' OnTick='{App.Tick}'/></MUIPage>";
        Compile(page, markup); Equal(0, events.EventSubscribers); window.Elements.Add(page); Equal(1, events.EventSubscribers);
        events.Raise(); Equal(1, page.Find<MxmlFixtures.TickElement>("Tick")!.Ticks);
        Compile(page, markup); Equal(1, events.EventSubscribers);
        Check(!page.Compile("<MUIPage><TickElement OnTick='{App.Tick}'/><Unknown/></MUIPage>")); Equal(1, events.EventSubscribers);
        page.Remove(); Equal(0, events.EventSubscribers); window.Elements.Add(page); Equal(1, events.EventSubscribers);
        page.Dispose(); Equal(0, events.EventSubscribers);
    }
    private static Task Resources()
    {
        MxmlFixtures.OwnedBackend.Created = MxmlFixtures.OwnedBackend.Disposed = 0;
        using var page = Page();
        Compile(page, "<MUIPage Backends='OwnedBackend'><TextBlock Text='{OwnedBackend.Text}'/></MUIPage>");
        Equal(1, MxmlFixtures.OwnedBackend.Created); Equal(0, MxmlFixtures.OwnedBackend.Disposed);
        Check(page.GetBackend<MxmlFixtures.OwnedBackend>("OwnedBackend") is not null);
        Check(!page.Compile("<MUIPage Backends='OwnedBackend'><Unknown/></MUIPage>")); Equal(2, MxmlFixtures.OwnedBackend.Created); Equal(1, MxmlFixtures.OwnedBackend.Disposed);
        Compile(page, "<MUIPage/>"); Equal(2, MxmlFixtures.OwnedBackend.Disposed);
        var borrowed = new MxmlFixtures.OwnedBackend(); page.BackendObjects.Add("Borrowed", borrowed);
        Compile(page, "<MUIPage><TextBlock Text='{Borrowed.Text}'/></MUIPage>"); page.Dispose(); Equal(2, MxmlFixtures.OwnedBackend.Disposed); borrowed.Dispose();
        return Task.CompletedTask;
    }
    private static Task Includes()
    {
        string folder = Path.Combine(Path.GetTempPath(), "melonui-mxml-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "child.xml"), "<MUIPage><TextBox Name='Input' Text='included'/></MUIPage>");
            File.WriteAllText(Path.Combine(folder, "main.xml"), "<MUIPage><MUIPage Name='Child' Source='child.xml' Width='50%'/></MUIPage>");
            using var page = Page(); Check(page.CompileFile(Path.Combine(folder, "main.xml")), page.GetSimpleCompilerDisplay());
            Equal("included", page.Find<TextBox>("Input")!.Text); Equal("50%", page.Find<MUIPage>("Child")!.Width);
            File.WriteAllText(Path.Combine(folder, "child.xml"), "<MUIPage><MUIPage Source='main.xml'/></MUIPage>");
            Check(!page.CompileFile(Path.Combine(folder, "main.xml"))); Check(page.CompilerMessages.Any(m => m.Code == "MXML040"));
            Check(page.Find<TextBox>("Input") is not null);
        }
        finally { var resolved = Path.GetFullPath(folder); Check(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)); Directory.Delete(resolved, true); }
        return Task.CompletedTask;
    }
    private static async Task Flags()
    {
        await using var window = Window(); using var page = Page(); var backend = new MxmlFixtures.TestBackend(); page.BackendObjects.Add("App", backend);
        Compile(page, """
            <MUIPage><Button Name="Go" OnPressed="{App.Call}"><Button.OnPressed MXMLFlags="Show(Later:false),Hide(Later),Show(Later:false),Toggle(Other)"/></Button>
            <TextBox Name="Later" IsVisible="false"/><TextBox Name="Other"/></MUIPage>
            """);
        window.Elements.Add(page); page.Find<Button>("Go")!.PerformClick(); Equal(1, backend.Calls);
        Check(page.Find<TextBox>("Later")!.IsVisible && !page.Find<TextBox>("Other")!.IsVisible);
        Check(!page.Compile("<MUIPage><Button><Button.OnPressed MXMLFlags='Show(Missing)'/></Button></MUIPage>"));
        Check(!page.Compile("<MUIPage><Button><Button.OnPressed MXMLFlags='Explode(Go)'/></Button></MUIPage>"));
    }
    private static async Task Actions()
    {
        await using var window = Window(); using var page = Page(); var backend = new MxmlFixtures.TestBackend(); page.BackendObjects.Add("App", backend);
        Compile(page, """
            <MUIPage><TextBox Name="Entry" OnEnter="{App.Submit}"><TextBox.OnEnter>
              <Actions><Invoke Path="App.Call"/><Set Target="Child/Result" Property="Text" Value="saved"/><Show Target="Child/Result" Focus="false"/></Actions>
            </TextBox.OnEnter></TextBox><MUIPage Name="Child"><TextBlock Name="Result" IsVisible="false"/></MUIPage></MUIPage>
            """);
        window.Elements.Add(page); var entry = page.Find<TextBox>("Entry")!; entry.Text = "payload"; entry.OnEnter!(entry.Text, entry);
        Equal("payload", backend.Submitted); Equal(1, backend.Calls); Equal("saved", page.Find<TextBlock>("Result")!.Text); Check(page.Find<TextBlock>("Result")!.IsVisible);
        Check(!page.Compile("<MUIPage><TextBox Name='Same'/><TextBox Name='Same'/></MUIPage>"));
        Compile(page, "<MUIPage><MUIPage Name='One'><TextBox Name='Same'/></MUIPage><MUIPage Name='Two'><TextBox Name='Same'/></MUIPage></MUIPage>");
    }
    private static Task Conversion()
    {
        using var page = Page(); var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            Compile(page, "<MUIPage xmlns:d='clr-namespace:MelonUI.Default;assembly=MelonUI'><d:ProgressBar Name='P' ProgressValue='0.5' ProgressColor='#80123456'/><TextBlock Name='Literal' Text='{}{literal}'/><KeyboardHolder Name='K' Optional='{Null}'/></MUIPage>");
            Equal(.5f, page.Find<ProgressBar>("P")!.ProgressValue); Equal(Color.FromArgb(128, 18, 52, 86).ToArgb(), page.Find<ProgressBar>("P")!.ProgressColor.ToArgb());
            Equal("{literal}", page.Find<TextBlock>("Literal")!.Text); Check(page.Find<MxmlFixtures.KeyboardHolder>("K")!.Optional is null);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = saved; }
        return Task.CompletedTask;
    }
    private static Task Diagnostics()
    {
        using var page = Page();
        foreach (string markup in new[] { "<Wrong/>", "<MUIPage><TextBlock", "<MUIPage>\n<TextBlock Foreground='999,0,0'/>\n</MUIPage>", "<MUIPage><TextBlock Missing='a'/></MUIPage>", "<!DOCTYPE MUIPage [<!ENTITY x 'y'>]><MUIPage/>" })
        { Check(!page.Compile(markup)); Check(page.Failed && page.CompilerMessages.Any(m => m.Severity == MelonUI.Enums.MessageSeverity.Error)); Check(page.GetSimpleCompilerDisplay(1).Length > 0); }
        Check(!page.Compile("<MUIPage>\n <TextBlock Foreground='bad color'/>\n</MUIPage>")); Equal(2, page.CompilerFocusedMessage.MxmlLineNumber.Line);
        return Task.CompletedTask;
    }
    private static Task Factories()
    {
        MxmlFixtures.CountingElement.Disposals = 0;
        using var page = Page(); page.CompilerOptions.Factories.Add(typeof(MxmlFixtures.CountingElement), () => new MxmlFixtures.CountingElement(42));
        Compile(page, "<MUIPage><CountingElement Name='Custom'/></MUIPage>"); Equal(42, page.Find<MxmlFixtures.CountingElement>("Custom")!.Number);
        Check(!page.Compile("<MUIPage><CountingElement/><Unknown/></MUIPage>")); Equal(1, MxmlFixtures.CountingElement.Disposals);
        page.Dispose(); Equal(2, MxmlFixtures.CountingElement.Disposals); return Task.CompletedTask;
    }
    private static async Task BackgroundNotification()
    {
        var backend = new MxmlFixtures.TestBackend(); await using var window = Window(); var page = Page(); page.BackendObjects.Add("App", backend);
        Compile(page, "<MUIPage><TextBlock Name='Bound' Text='{App.Text}'/></MUIPage>"); window.Elements.Add(page);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.FrameRendered += (_, _) => ready.TrySetResult(); var run = window.RunAsync(); await ready.Task;
        await Task.Run(() => backend.Text = "background");
        await window.InvokeAsync(() => Check(window.CaptureText(width: 40, height: 8).Contains("background")));
        window.Stop(); await run;
    }
    private static async Task IdleOutput()
    {
        var terminal = new FakeTerminal(); await using var window = new ConsoleWindowManager(terminal) { TargetFrameRate = 60 };
        var text = new TextBlock { Text = "idle", ShowBorder = false }; window.Elements.Add(text);
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.FrameRendered += (_, _) => first.TrySetResult(); var run = window.RunAsync(); await first.Task;
        await Task.Delay(150); Equal(1, terminal.PresentCount);
        await window.InvokeAsync(() => { text.Text = "changed"; window.CaptureText(width: 40, height: 10); });
        await Task.Delay(80); Equal(2, terminal.PresentCount);
        await window.InvokeAsync(text.Remove); await Task.Delay(80); Equal(3, terminal.PresentCount); text.Dispose();
        window.Stop(); await run;
    }
    private static async Task ControlEvents()
    {
        await using var window = Window(); using var page = Page(); var backend = new MxmlFixtures.TestBackend(); page.BackendObjects.Add("App", backend);
        Compile(page, """
            <MUIPage><RgbColorPicker Name="Color" Confirmed="{App.AcceptColor}"><RgbColorPicker.Confirmed><Set Target="Status" Property="Text" Value="accepted"/></RgbColorPicker.Confirmed></RgbColorPicker>
            <TextBlock Name="Status"><TextBlock.Text>{App.Text}</TextBlock.Text></TextBlock></MUIPage>
            """);
        window.Elements.Add(page); var color = page.Find<RgbColorPicker>("Color")!; color.Commit(); Equal(1, backend.Calls); Equal("accepted", backend.Text);
        page.Remove(); color.Commit(); Equal(1, backend.Calls); window.Elements.Add(page); color.Commit(); Equal(2, backend.Calls);
        Check(!page.Compile("<MUIPage MxmlVersion='2'/>"));
    }
    private static async Task NestedBindings()
    {
        await using var window = Window(); using var page = Page(); var old = new MxmlFixtures.TestBackend();
        var root = new MxmlFixtures.TestBackend { Current = old }; page.BackendObjects.Add("App", root);
        Compile(page, "<MUIPage><TextBlock Name='Text' Text='{App.Current.Text}'/><TickElement Name='Tick' OnTick='{App.Current.Tick}'/></MUIPage>");
        window.Elements.Add(page); old.Raise(); Equal(1, page.Find<MxmlFixtures.TickElement>("Tick")!.Ticks);
        var replacement = new MxmlFixtures.TestBackend { Text = "replacement" }; root.Current = replacement;
        Equal(0, old.EventSubscribers); Equal(0, old.Subscribers); Equal("replacement", page.Find<TextBlock>("Text")!.Text);
        replacement.Raise(); Equal(2, page.Find<MxmlFixtures.TickElement>("Tick")!.Ticks);
        page.Dispose(); Equal(0, root.Subscribers); Equal(0, replacement.EventSubscribers); Equal(0, replacement.Subscribers);
    }
    private static Task DraftBindings()
    {
        using var page = Page(); var source = new MxmlFixtures.TestBackend(); page.BackendObjects.Add("App", source);
        Check(!page.Compile("<MUIPage><TextBox Text='{App.Text}'><TextBox.Text>unexpected write</TextBox.Text></TextBox></MUIPage>"));
        Equal("initial", source.Text); Check(page.CompilerMessages.Any(m => m.Code == "MXML023"));
        page.CompilerOptions.Factories.Add(typeof(TextBox), () =>
        {
            var input = new TextBox(); input.SetBinding(nameof(TextBox.Text), Binding.CreatePropertyBinding(source, nameof(source.Text))); return input;
        });
        Check(!page.Compile("<MUIPage><TextBox Text='draft'/><Unknown/></MUIPage>")); Equal("initial", source.Text);
        return Task.CompletedTask;
    }
}

namespace MxmlFixtures
{
    public class TestBackend : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? _changed; private EventHandler? _tick; private string _text = "initial";
        private TestBackend? _current;
        public TestBackend? Current { get => _current; set { _current = value; _changed?.Invoke(this, new(nameof(Current))); } }
        public event PropertyChangedEventHandler? PropertyChanged { add => _changed += value; remove => _changed -= value; }
        public event EventHandler? Tick { add => _tick += value; remove => _tick -= value; }
        public int Subscribers => _changed?.GetInvocationList().Length ?? 0;
        public int EventSubscribers => _tick?.GetInvocationList().Length ?? 0;
        public string Text { get => _text; set { _text = value; _changed?.Invoke(this, new(nameof(Text))); } }
        public int Calls { get; private set; }
        public string Submitted { get; private set; } = "";
        public void Call() => Calls++;
        public void Submit(string text, TextBox box) => Submitted = text;
        public void AcceptColor(Color color) => Calls++;
        public void Raise() => _tick?.Invoke(this, EventArgs.Empty);
    }
    public sealed class OwnedBackend : IDisposable
    {
        public static int Created, Disposed;
        public string Text => "owned";
        public OwnedBackend() => Created++;
        public void Dispose() => Disposed++;
    }
    public sealed class TickElement : UIElement
    {
        public int Ticks { get; private set; }
        public void OnTick(object? sender, EventArgs e) => Ticks++;
        protected override void RenderContent(ConsoleBuffer buffer) { }
    }
    public sealed class KeyboardHolder : UIElement
    {
        public ConsoleKey? Optional { get; set; }
        protected override void RenderContent(ConsoleBuffer buffer) { }
    }
    public sealed class CountingElement(int number) : UIElement
    {
        public static int Disposals;
        public int Number { get; } = number;
        protected override void RenderContent(ConsoleBuffer buffer) { }
        protected override void DisposeCore() => Disposals++;
    }
}
