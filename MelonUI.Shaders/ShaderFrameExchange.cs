using MelonUI.Base;

namespace MelonUI.Shaders;

/// <summary>Single-producer frame handoff. Dimensions and pixels are published together after readback.</summary>
internal sealed class ShaderFrameExchange
{
    private sealed class Frame(int width, int height)
    {
        internal readonly int Width = width;
        internal readonly int Height = height;
        internal readonly ConsolePixel[] Pixels = new ConsolePixel[checked(width * height)];
    }

    private readonly object _sync = new();
    private (int Width, int Height) _requestedSize;
    private Frame? _published;
    private Frame? _back;

    internal ShaderFrameExchange(int width, int height) => RequestSize(width, height);

    internal void RequestSize(int width, int height)
    {
        ValidateSize(width, height);
        lock (_sync) _requestedSize = (width, height);
    }

    internal (int Width, int Height) RequestedSize { get { lock (_sync) return _requestedSize; } }
    internal bool HasFrame { get { lock (_sync) return _published is not null; } }

    // Only the GPU producer calls this. Slow readback never holds the UI-copy lock.
    internal void PublishFrame(int width, int height, Action<ConsolePixel[]> readPixels)
    {
        ValidateSize(width, height);
        ArgumentNullException.ThrowIfNull(readPixels);
        var frame = _back;
        if (frame is null || frame.Width != width || frame.Height != height)
            frame = _back = new Frame(width, height);
        readPixels(frame.Pixels);
        lock (_sync)
        {
            _back = _published;
            _published = frame;
        }
    }

    internal bool CopyLatestTo(ConsoleBuffer destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        lock (_sync)
        {
            if (_published is not { } frame) return false;
            int width = Math.Min(frame.Width, destination.Width);
            int height = Math.Min(frame.Height, destination.Height);
            for (int y = 0; y < height; y++)
            {
                // OpenGL rows run bottom-to-top. Use the published frame's stride and height.
                Array.Copy(frame.Pixels, (frame.Height - 1 - y) * frame.Width,
                    destination.buffer, y * destination.Width, width);
            }
            return true;
        }
    }

    private static void ValidateSize(int width, int height)
    {
        if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
        _ = checked(width * height);
    }
}
