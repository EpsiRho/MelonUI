using OpenTK.Graphics.OpenGL4;

namespace MelonUI.Shaders;

/// <summary>A linked OpenGL program. Create, use and dispose on the owning current context.</summary>
public sealed class Shader : IDisposable
{
    public int Handle { get; private set; }
    public Shader(string vertexShaderSource, string fragmentShaderSource)
    {
        int vertex = 0, fragment = 0, program = 0;
        try
        {
            vertex = Compile(ShaderType.VertexShader, vertexShaderSource);
            fragment = Compile(ShaderType.FragmentShader, fragmentShaderSource);
            program = GL.CreateProgram(); GL.AttachShader(program, vertex); GL.AttachShader(program, fragment); GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0) throw new InvalidOperationException($"Shader link failed: {GL.GetProgramInfoLog(program)}");
            GL.DetachShader(program, vertex); GL.DetachShader(program, fragment);
            Handle = program; program = 0;
        }
        finally { if (program != 0) GL.DeleteProgram(program); if (vertex != 0) GL.DeleteShader(vertex); if (fragment != 0) GL.DeleteShader(fragment); }
    }
    private static int Compile(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        try
        {
            GL.ShaderSource(shader, source); GL.CompileShader(shader); GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0) throw new InvalidOperationException($"{type} compilation failed: {GL.GetShaderInfoLog(shader)}");
            return shader;
        }
        catch { GL.DeleteShader(shader); throw; }
    }
    public void Use() { ObjectDisposedException.ThrowIf(Handle == 0, this); GL.UseProgram(Handle); }
    public void Dispose() { if (Handle != 0) { GL.DeleteProgram(Handle); Handle = 0; } }
}
