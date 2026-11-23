using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using MelonUI.Base;

public class UnixRenderer
{
    [DllImport("libc", SetLastError = true)]
    private static extern nint write(int fd, IntPtr buffer, nuint count);
    private byte[] _output = [];
    public int Width { get; private set; }
    public int Height { get; private set; }
    public static bool IsSupported = OperatingSystem.IsLinux();
    public UnixRenderer(int width, int height) => SetSize(width, height);
    public void SetSize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        _output = new byte[checked(width * height * 50 + 64)];
        Width = width; Height = height;
    }

    // Test the exact Linux frame encoding without requiring a native terminal.
    internal ReadOnlyMemory<byte> BuildFrame(ConsolePixel[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (buffer.Length < checked(Width * Height)) throw new ArgumentException("Pixel buffer is smaller than the viewport.", nameof(buffer));
        if (Width == 0 || Height == 0) return ReadOnlyMemory<byte>.Empty;
        int offset = 0, lastFg = int.MinValue, lastBg = int.MinValue;
        void Append(string text) => offset += Encoding.UTF8.GetBytes(text, _output.AsSpan(offset));
        Append("\x1b[H");
        Span<char> glyph = stackalloc char[2];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var pixel = buffer[y * Width + x];
                bool raw = pixel.Character == '\uffff';
                var foreground = raw ? Color.Black : pixel.Foreground;
                var background = raw ? Color.FromArgb(255, (int)pixel.R, (int)pixel.G, (int)pixel.B) : pixel.Background;
                if (foreground.ToArgb() != lastFg)
                {
                    Append(foreground.A == 0 ? "\x1b[39m" : $"\x1b[38;2;{foreground.R};{foreground.G};{foreground.B}m");
                    lastFg = foreground.ToArgb();
                }
                if (background.ToArgb() != lastBg)
                {
                    Append(background.A == 0 ? "\x1b[49m" : $"\x1b[48;2;{background.R};{background.G};{background.B}m");
                    lastBg = background.ToArgb();
                }
                char ch = raw || pixel.Character == '\0' || char.IsControl(pixel.Character) ? ' ' : pixel.Character;
                glyph[0] = ch;
                int length = 1;
                bool nextCell = x + 1 < Width;
                if (char.IsHighSurrogate(ch) && nextCell && char.IsLowSurrogate(buffer[y * Width + x + 1].Character))
                { glyph[1] = buffer[y * Width + x + 1].Character; length = 2; }
                else if (char.IsSurrogate(ch)) glyph[0] = '\ufffd';
                offset += Encoding.UTF8.GetBytes(glyph[..length], _output.AsSpan(offset));
                if ((pixel.IsWide || length == 2) && nextCell) x++;
            }
            if (y + 1 < Height) Append("\r\n");
        }
        Append("\x1b[0m");
        return _output.AsMemory(0, offset);
    }

    public unsafe void RenderToConsole(ConsolePixel[] buffer)
    {
        var frame = BuildFrame(buffer);
        fixed (byte* start = frame.Span)
        {
            int offset = 0;
            while (offset < frame.Length)
            {
                nint count = write(1, (IntPtr)(start + offset), (nuint)(frame.Length - offset));
                if (count < 0)
                {
                    int error = Marshal.GetLastPInvokeError();
                    if (error == 4) continue; // EINTR: retry an interrupted write.
                    throw new Win32Exception(error);
                }
                if (count == 0) throw new IOException("Terminal output made no progress.");
                offset += checked((int)count);
            }
        }
    }
}
