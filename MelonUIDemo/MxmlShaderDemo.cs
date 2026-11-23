using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Backends;
using MelonUI.Shaders;
using System.Diagnostics;

namespace MelonUIDemo;

// Preserves the experimental MXML/shader demo. Compiler and control polish are later sprints.
internal static class MxmlShaderDemo
{
    internal static async Task RunAsync(bool smoke = false)
    {
        await using var window = new ConsoleWindowManager { Title = "MelonUI — experimental MXML/shader demo" };
        DemoWelcomeBackend.CWM = window;
        DemoWelcomeBackend.ShaderSource = DemoSources.Shader;
        var page = new MUIPage();
        page.CompilerOptions.BaseDirectory = AppContext.BaseDirectory;
        if (!page.Compile(DemoSources.Markup))
        {
            Console.WriteLine(page.GetSimpleCompilerDisplay());
            page.Dispose();
            return;
        }
        window.Elements.Add(page);
        // GLFW context creation belongs to the app main thread; parsing remains side-effect free.
        page.Children.OfType<OpenGLShaderElement>().Single().Init();
        window.Elements.Add(new TextBlock
        {
            Name = "Status", 
            Text = "F1: renderer   Esc: quit",
            Width = "100%", 
            Height = "1",
            XYAlignment = MelonUI.Enums.Alignment.BottomLeft, 
            ShowBorder = false, 
            ConsiderForFocus = false
        });
        window.RegisterKeyboardControl(ConsoleKey.F1, () =>
            window.UsePlatformSpecificRenderer = !window.UsePlatformSpecificRenderer, "Toggle renderer");
        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
        window.RegisterKeyboardControl(ConsoleKey.F12, () =>
        {
            Directory.CreateDirectory("Screenshots");
            File.WriteAllText(Path.Combine("Screenshots", $"{DateTime.Now:yyyyMMdd-HHmmss}.txt"), window.CaptureText());
        }, "Capture text");
        window.FrameRendered += (_, _) =>
        {
            var now = DateTime.Now;
            DemoWelcomeBackend.FrameTimes.Insert(0, (now - DemoWelcomeBackend.LastFrameTime).TotalMilliseconds);
            DemoWelcomeBackend.FrameTimes = DemoWelcomeBackend.FrameTimes.Take(100).ToList();
            DemoWelcomeBackend.FrameTimeDisplay = $"LFT: {DemoWelcomeBackend.FrameTimes.Average():0000.0000}ms";
            DemoWelcomeBackend.LastFrameTime = now;
        };
        if (smoke)
        {
            var shader = page.Children.OfType<OpenGLShaderElement>().Single();
            var sizes = new[] { ("40", "10"), ("1", "1"), ("100%", "100%"), ("9", "25"), ("70", "18") };
            var clock = Stopwatch.StartNew();
            var observedFrame = false;
            var resize = 0;
            var nextResize = TimeSpan.Zero;
            window.FrameRendered += (_, _) =>
            {
                if (clock.Elapsed > TimeSpan.FromSeconds(10))
                    throw new TimeoutException("The shader did not complete its startup/resize smoke check.");
                // Raw OpenGL pixels carry the opaque-alpha marker in their Character field.
                observedFrame |= window.CaptureText().Contains('\uffff');
                if (!observedFrame || clock.Elapsed < nextResize) return;
                if (resize == sizes.Length) { window.Stop(); return; }
                (shader.Width, shader.Height) = sizes[resize++];
                nextResize = clock.Elapsed + TimeSpan.FromMilliseconds(150);
            };
        }
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; window.Stop(); };
        Console.CancelKeyPress += cancel;
        try { await window.RunAsync(); }
        finally
        {
            Console.CancelKeyPress -= cancel;
            await DemoWelcomeBackend.StopWatcherAsync();
        }
        if (smoke)
        {
            await window.DisposeAsync();
            Console.WriteLine("PASS: MXML shader startup, repeated resize, and shutdown.");
        }
    }
}
