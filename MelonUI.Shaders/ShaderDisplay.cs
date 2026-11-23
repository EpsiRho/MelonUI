using ComputeSharp;
using ComputeSharp.Descriptors;
using System.Drawing;
using System.Runtime.Versioning;
using MelonUI.Base;

namespace MelonUI.Components;

[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
[SupportedOSPlatform("windows6.2")]
internal readonly partial struct GradientShader(ReadWriteTexture2D<float4> texture) : IComputeShader
{
    private readonly ReadWriteTexture2D<float4> texture = texture;
    public void Execute()
    {
        float2 uv = ThreadIds.XY / new float2(texture.Width, texture.Height);
        texture[ThreadIds.XY] = new float4(uv.X, uv.Y, .5f, 1);
    }
}

/// <summary>Optional Windows Direct3D compute display. Construction does not allocate GPU resources.</summary>
public class ShaderDisplay : UIElement
{
    private ReadWriteTexture2D<float4>? _texture;
    private Action<ReadWriteTexture2D<float4>, int, int>? _draw;
    public string? ErrorMessage { get; private set; }
    public ShaderDisplay() { ConsiderForFocus = false; ShowBorder = false; EnableCaching = false; }

    [SupportedOSPlatform("windows6.2")]
    public void UpdateShader<T>(Func<ReadWriteTexture2D<float4>, T> factory) where T : struct, IComputeShader, IComputeShaderDescriptor<T>
    {
        VerifyAccess(); ArgumentNullException.ThrowIfNull(factory);
        _draw = (texture, width, height) => GraphicsDevice.GetDefault().For(width, height, factory(texture));
        ErrorMessage = null; Invalidate();
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        if (a.Width == 0 || a.Height == 0) return;
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 2)) ErrorMessage = "Compute shaders require Windows/Direct3D.";
        else if (ErrorMessage is null)
        {
            try { RenderGpu(buffer, a.X, a.Y, a.Width, a.Height); return; }
            catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or System.Runtime.InteropServices.COMException)
            { ErrorMessage = ex.Message; }
        }
        ControlDrawing.Text(buffer, a.X, a.Y, ErrorMessage, a.Width, Foreground, Background);
    }
    [SupportedOSPlatform("windows6.2")]
    private void RenderGpu(ConsoleBuffer buffer, int left, int top, int width, int height)
    {
        if (_texture is null || _texture.Width != width || _texture.Height != height)
        {
            _texture?.Dispose();
            _texture = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<float4>(width, height);
        }
        if (_draw is { } draw) draw(_texture, width, height);
        else GraphicsDevice.GetDefault().For(width, height, new GradientShader(_texture));
        var pixels = new float4[width * height];
        _texture.CopyTo(pixels);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                static int Channel(float value) => (int)(ControlDrawing.Unit(value) * 255);
                buffer.SetPixel(left + x, top + y, '█', Color.FromArgb(Channel(pixel.X), Channel(pixel.Y), Channel(pixel.Z)), Background);
            }
    }
    protected override void DisposeCore()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 2)) _texture?.Dispose();
        _texture = null; _draw = null; base.DisposeCore();
    }
}
