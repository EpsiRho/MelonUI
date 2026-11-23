using System.Diagnostics;
using System.Runtime.CompilerServices;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;

internal static class SoakTests
{
    internal static Task Run()
    {
        var clock = Stopwatch.StartNew(); long allocationStart = GC.GetTotalAllocatedBytes();
        var references = Exercise();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        int alive = references.Count(reference => reference.TryGetTarget(out _));
        if (alive > 2) throw new Exception($"{alive} replaced UI subtrees survived collection after 500 reloads.");
        Console.WriteLine($"SOAK: cycles=500 oldSubtreesAlive={alive} elapsedMs={clock.ElapsedMilliseconds} allocatedBytes={GC.GetTotalAllocatedBytes() - allocationStart}");
        return Task.CompletedTask;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference<UIElement>> Exercise()
    {
        var source = new MxmlFixtures.TestBackend(); var window = new ConsoleWindowManager(new FakeTerminal());
        var page = new MUIPage(); page.CompilerOptions.Assemblies.Add(typeof(MxmlFixtures.TickElement).Assembly);
        page.CompilerOptions.Namespaces.Add("MxmlFixtures"); page.BackendObjects.Add("App", source); window.Elements.Add(page);
        var references = new List<WeakReference<UIElement>>();
        const string markup = """
            <MUIPage ShowBorder="false"><TickElement OnTick="{App.Tick}" Width="1" Height="1"/>
            <TextBlock Name="Out" Text="{App.Text}" Height="1" ShowBorder="false"/>
            <Button Name="Go" OnPressed="{App.Call}" Y="2" Height="3"><Button.OnPressed><Show Target="Out" Focus="false"/></Button.OnPressed></Button></MUIPage>
            """;
        for (int i = 0; i < 500; i++)
        {
            if (page.Children.Count > 0) foreach (var child in page.Children) references.Add(new(child));
            if (!page.Compile(markup)) throw new Exception(page.GetSimpleCompilerDisplay());
            if (i % 10 == 0 && page.Compile("<MUIPage><TickElement OnTick='{App.Tick}'/><Unknown/></MUIPage>")) throw new Exception("Invalid reload succeeded.");
            if (source.EventSubscribers != 1) throw new Exception("Reload accumulated event subscribers.");
            source.Raise();
            if (((MxmlFixtures.TickElement)page.Children[0]).Ticks != 1) throw new Exception("A stale event handler ran after reload.");
            page.Find<Button>("Go")!.PerformClick(); window.CaptureText(width: 40, height: 10);
        }
        page.Dispose();
        if (source.EventSubscribers != 0 || source.Subscribers != 0) throw new Exception("Disposal retained backend subscriptions.");
        window.DisposeAsync().AsTask().GetAwaiter().GetResult();
        return references;
    }
}
