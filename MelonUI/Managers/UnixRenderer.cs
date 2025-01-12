using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using MelonUI.Base;

public class UnixRenderer
{
    private const int STDOUT_FILENO = 1;
    private const int TIOCGWINSZ = 0x5413;
    private const int TCGETS = 0x5401;
    private const int TCSETS = 0x5402;

    [StructLayout(LayoutKind.Sequential)]
    private struct termios
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] c_cc;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct winsize
    {
        public ushort ws_row;
        public ushort ws_col;
        public ushort ws_xpixel;
        public ushort ws_ypixel;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int write(int fd, byte[] buf, int count);

    [DllImport("libc", SetLastError = true)]
    private static extern int ioctl(int fd, int cmd, IntPtr arg);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, out termios termios_p);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int optional_actions, ref termios termios_p);

    private byte[] writeBuffer;
    private readonly bool vtProcessingEnabled;

    // Conservative estimate of max bytes needed per pixel
    private const int BYTES_PER_PIXEL = 50; // Color sequences + char + safety margin
    private const int MAX_COLOR_SEQUENCE_LENGTH = 38; // Max length of a color sequence

    public int Width { get; private set; }
    public int Height { get; private set; }

    public static bool IsSupported = OperatingSystem.IsLinux();

    public UnixRenderer(int width, int height)
    {
        // Check if we can enable VT processing
        SetSize(width, height);
    }

    public void SetSize(int width, int height)
    {
        Width = width;
        Height = height;
        // Allocate buffer with enough space for all possible characters and sequences
        int bufferSize = (width * height * BYTES_PER_PIXEL) + MAX_COLOR_SEQUENCE_LENGTH;
        writeBuffer = new byte[bufferSize];
    }

    public unsafe void RenderToConsole(ConsolePixel[] buffer)
    {
        if (Width <= 0 || Height <= 0 || buffer == null) return;

        try
        {
            int writeOffset = 0;
            int maxLength = writeBuffer.Length;

            // Move cursor home - direct assignment
            writeBuffer[writeOffset++] = 0x1b;
            writeBuffer[writeOffset++] = (byte)'[';
            writeBuffer[writeOffset++] = (byte)'H';

            int lastFg = default;
            int lastBg = default;
            int bufferIndex = 0;
            int widthMinusOne = Width - 1;
            int heightMinusOne = Height - 1;

            // Preallocate common sequences
            byte[] resetFg = new byte[] { 0x1b, (byte)'[', (byte)'3', (byte)'9', (byte)'m' };
            byte[] resetBg = new byte[] { 0x1b, (byte)'[', (byte)'4', (byte)'9', (byte)'m' };
            byte[] resetSequence = new byte[] { 0x1b, (byte)'[', (byte)'0', (byte)'m' };

            for (int y = 0; y < heightMinusOne; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    ref ConsolePixel pixel = ref buffer[bufferIndex++];

                    // Check if we need to flush
                    if (maxLength - writeOffset < BYTES_PER_PIXEL)
                    {
                        write(STDOUT_FILENO, writeBuffer, writeOffset);
                        writeOffset = 0;
                    }

                    // Character is a raw pixel from OpenGL
                    if (pixel.Character == '\xFFFF')
                    {
                        // Blank Foreground
                        writeOffset += FormatColorSequence(writeBuffer, writeOffset, true, Color.Black);
                        lastFg = 0x0;

                        uint finalPixelColor = ((pixel.A & 0xFF) << 24) | ((pixel.R & 0xFF) << 16) | ((pixel.G & 0xFF) << 8) | (pixel.B & 0xFF);
                        writeOffset += FormatColorSequence(writeBuffer, writeOffset, false, Color.FromArgb((int)finalPixelColor));

                        lastBg = (int)finalPixelColor;

                        writeBuffer[writeOffset++] = 0x20;

                        continue;
                    }

                    // Handle foreground color
                    if (pixel.ForegroundARGB != lastFg)
                    {
                        if (pixel.Foreground.A == 0)
                        {
                            Buffer.BlockCopy(resetFg, 0, writeBuffer, writeOffset, 5);
                            writeOffset += 5;
                        }
                        else
                        {
                            writeOffset += FormatColorSequence(writeBuffer, writeOffset, true, pixel.Foreground);
                        }
                        lastFg = pixel.ForegroundARGB;
                    }

                    // Handle background color
                    if (pixel.BackgroundARGB != lastBg)
                    {
                        if (pixel.Background.A == 0)
                        {
                            Buffer.BlockCopy(resetBg, 0, writeBuffer, writeOffset, 5);
                            writeOffset += 5;
                        }
                        else
                        {
                            writeOffset += FormatColorSequence(writeBuffer, writeOffset, false, pixel.Background);
                        }
                        lastBg = pixel.BackgroundARGB;
                    }

                    // Write character - optimize for common case of space
                    if (pixel.Character == '\0' || pixel.Character == ' ')
                    {
                        writeBuffer[writeOffset++] = (byte)' ';
                    }
                    else
                    {
                        // Only encode non-ASCII characters
                        if (pixel.Character < 128)
                        {
                            writeBuffer[writeOffset++] = (byte)pixel.Character;
                        }
                        else
                        {
                            byte[] charBytes = System.Text.Encoding.UTF8.GetBytes(new[] { pixel.Character });
                            Buffer.BlockCopy(charBytes, 0, writeBuffer, writeOffset, charBytes.Length);
                            writeOffset += charBytes.Length;
                        }
                    }

                    if (pixel.IsWide) x++;
                }

                // Write newline - direct assignment
                writeBuffer[writeOffset++] = (byte)'\r';
                writeBuffer[writeOffset++] = (byte)'\n';
            }

            // Reset colors at end
            Buffer.BlockCopy(resetSequence, 0, writeBuffer, writeOffset, 4);
            writeOffset += 4;

            // Final write if we have content
            if (writeOffset > 0)
            {
                write(STDOUT_FILENO, writeBuffer, writeOffset);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Render error: {ex}");
        }
    }

    private int FormatColorSequence(byte[] buffer, int offset, bool isForeground, Color color)
    {
        // Format: \x1b[38;2;R;G;Bm or \x1b[48;2;R;G;Bm
        byte[] prefix = new byte[] { 0x1b, (byte)'[', (byte)(isForeground ? '3' : '4'), (byte)'8', (byte)';', (byte)'2', (byte)';' };
        Array.Copy(prefix, 0, buffer, offset, prefix.Length);
        int currentOffset = offset + prefix.Length;

        // Write RGB values
        currentOffset += WriteNumber(buffer, currentOffset, color.R);
        buffer[currentOffset++] = (byte)';';
        currentOffset += WriteNumber(buffer, currentOffset, color.G);
        buffer[currentOffset++] = (byte)';';
        currentOffset += WriteNumber(buffer, currentOffset, color.B);
        buffer[currentOffset++] = (byte)'m';

        return currentOffset - offset;
    }

    private int WriteNumber(byte[] buffer, int offset, int value)
    {
        if (value >= 100)
        {
            buffer[offset] = (byte)('0' + value / 100);
            value %= 100;
            buffer[offset + 1] = (byte)('0' + value / 10);
            buffer[offset + 2] = (byte)('0' + value % 10);
            return 3;
        }
        else if (value >= 10)
        {
            buffer[offset] = (byte)('0' + value / 10);
            buffer[offset + 1] = (byte)('0' + value % 10);
            return 2;
        }
        else
        {
            buffer[offset] = (byte)('0' + value);
            return 1;
        }
    }
}