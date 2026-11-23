using MelonUI.Components;

internal static class HardwareTests
{
    internal static Task Compute()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 2)) throw new PlatformNotSupportedException("--gpu requires Windows/Direct3D.");
        using var display = new ShaderDisplay();
        display.UpdateShader(CreateGradient);
        foreach (var size in new[] { (20, 10), (1, 1), (7, 3), (40, 12) })
        {
            display.CalculateLayout(0, 0, size.Item1, size.Item2);
            var frame = display.Render();
            if (display.ErrorMessage is { } error) throw new InvalidOperationException(error);
            var first = frame[0, 0].Foreground;
            var last = frame[size.Item2 - 1, size.Item1 - 1].Foreground;
            if (first.B is < 120 or > 130 || (size.Item1 > 1 && last.R <= first.R))
                throw new InvalidOperationException("Compute shader did not produce the expected gradient.");
        }
        return Task.CompletedTask;
    }
    [System.Runtime.Versioning.SupportedOSPlatform("windows6.2")]
    private static GradientShader CreateGradient(ComputeSharp.ReadWriteTexture2D<ComputeSharp.Float4> texture) => new(texture);
}
