using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using MelonUI.Managers;
using MelonUI.Shaders;

namespace MelonUIDemo;

// Draws Pages/shader.frag.glsl into the terminal with OpenGL. Needs a display and OpenGL 3.3.
internal static class ShaderDemo
{
    public static async Task RunAsync()
    {
        await using var window = new ConsoleWindowManager { Title = "MelonUI Shader Demo" };

        var shader = new OpenGLShaderElement
        {
            ShaderPath = Path.Combine(AppContext.BaseDirectory, "Pages", "shader.frag.glsl"),
            Width = "100%",
            Height = "100%",
            ShowBorder = false,
        };
        window.Elements.Add(shader);
        window.Elements.Add(new TextBlock
        {
            Text = "Esc quits.",
            Height = "1",
            ShowBorder = false,
            XYAlignment = Alignment.BottomLeft,
            ConsiderForFocus = false,
        });

        // Init starts the OpenGL window, so call it after the element is on screen.
        shader.Init();

        window.RegisterKeyboardControl(ConsoleKey.Escape, window.Stop, "Quit");
        await window.RunAsync();
    }
}
