﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using MelonUI.Base;
using static System.Net.Mime.MediaTypeNames;
using MelonUI.Managers;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Numerics;

public class Win32Renderer
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_PROCESSED_OUTPUT = 0x0001;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool WriteConsoleW(
        IntPtr hConsoleOutput,
        char[] lpBuffer,
        int nNumberOfCharsToWrite,
        out int lpNumberOfCharsWritten,
        IntPtr lpReserved);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetConsoleScreenBufferInfo(
        IntPtr hConsoleOutput,
        out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    private readonly IntPtr consoleHandle;
    private readonly bool vtProcessingEnabled;
    private char[] writeBuffer;

    // Conservative estimate of max chars needed per pixel
    private const int CHARS_PER_PIXEL = 50; // Color sequences + char + safety margin
    private const int MAX_COLOR_SEQUENCE_LENGTH = 38; // Max length of a color sequence

    public int Width { get; private set; }
    public int Height { get; private set; }

    public static bool IsSupported = OperatingSystem.IsWindows();

    public Win32Renderer(int width, int height)
    {
        consoleHandle = GetStdHandle(STD_OUTPUT_HANDLE);

        // Check if we can enable VT processing
        if (GetConsoleMode(consoleHandle, out uint mode))
        {
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN | ENABLE_PROCESSED_OUTPUT;
            vtProcessingEnabled = SetConsoleMode(consoleHandle, mode);
        }

        SetSize(width, height);
    }

    public void SetSize(int width, int height)
    {
        Width = width;
        Height = height;
        // Allocate buffer with enough space for all possible characters and sequences
        int bufferSize = (width * height * CHARS_PER_PIXEL) + MAX_COLOR_SEQUENCE_LENGTH;
        writeBuffer = new char[bufferSize];
    }




    // Structure to hold rectangle coordinates
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWINFO
    {
        public uint cbSize;
        public RECT rcWindow;
        public RECT rcClient;
        public uint dwStyle;
        public uint dwExStyle;
        public uint dwWindowStatus;
        public uint cxWindowBorders;
        public uint cyWindowBorders;
        public ushort atomWindowType;
        public ushort wCreatorVersion;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public short wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    public static CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo()
    {
        // Get the handle to the standard output
        IntPtr hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE);

        if (hConsoleOutput == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to get standard output handle.");
        }

        // Retrieve the console screen buffer info
        if (!GetConsoleScreenBufferInfo(hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO bufferInfo))
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new System.ComponentModel.Win32Exception(errorCode, "Failed to retrieve console screen buffer info.");
        }

        return bufferInfo;
    }

    public static (int width, int height) GetConsoleWindowSize()
    {
        //var info = GetBufferInfo();
        //return (info.dwSize.X, info.dwSize.Y);
        return (Console.WindowWidth, Console.WindowHeight);
    }

    public unsafe void RenderToConsole(ConsolePixel[] buffer)
    {
        if (Width <= 0 || Height <= 0 || buffer == null) return;

        try
        {
            int writeOffset = 0;
            int maxLength = writeBuffer.Length;

            // Move cursor home - direct assignment
            writeBuffer[writeOffset++] = '\x1b';
            writeBuffer[writeOffset++] = '[';
            writeBuffer[writeOffset++] = 'H';

            int lastFg = default;
            int lastBg = default;
            int bufferIndex = 0;
            int widthMinusOne = Width - 1;
            int heightMinusOne = Height - 1;

            // Preallocate common sequences
            char[] resetFg = new[] { '\x1b', '[', '3', '9', 'm' };
            char[] resetBg = new[] { '\x1b', '[', '4', '9', 'm' };
            char[] resetSequence = new[] { '\x1b', '[', '0', 'm' };

            for (int y = 0; y < heightMinusOne; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    ref ConsolePixel pixel = ref buffer[bufferIndex++];

                    // Check if we need to flush
                    //if (maxLength - writeOffset < CHARS_PER_PIXEL)
                    //{
                    //    WriteConsoleW(consoleHandle, writeBuffer, writeOffset, out _, IntPtr.Zero);
                    //    writeOffset = 0;
                    //}

                    // Character is a raw pixel from OpenGL
                    if (pixel.Character == '\xFFFF')
                    {
                        var ole = pixel.ToOleColor();
                        writeOffset += FormatColorSequence(writeBuffer, writeOffset, false, (byte)pixel.R , (byte)pixel.G, (byte)pixel.B);

                        lastBg = ole;

                        writeBuffer[writeOffset++] = ' ';

                        continue;
                    }

                    // Handle foreground color
                    if (pixel.ForegroundARGB != lastFg)
                    {
                        if (pixel.Foreground.A == 0)
                        {
                            Buffer.BlockCopy(resetFg, 0, writeBuffer, writeOffset * sizeof(char), resetFg.Length * sizeof(char));
                            writeOffset += resetFg.Length;
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
                            Buffer.BlockCopy(resetBg, 0, writeBuffer, writeOffset * sizeof(char), resetBg.Length * sizeof(char));
                            writeOffset += resetBg.Length;
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
                        writeBuffer[writeOffset++] = ' ';
                    }
                    else
                    {
                        writeBuffer[writeOffset++] = pixel.Character;
                    }

                    if (pixel.IsWide) x++;
                }

                // Write newline - direct assignment
                writeBuffer[writeOffset++] = '\r';
                writeBuffer[writeOffset++] = '\n';
            }

            // Reset colors at end
            Buffer.BlockCopy(resetSequence, 0, writeBuffer, writeOffset * sizeof(char), resetSequence.Length * sizeof(char));
            writeOffset += resetSequence.Length;

            // Final write if we have content
            if (writeOffset > 0)
            {
                WriteConsoleW(consoleHandle, writeBuffer, writeOffset, out _, IntPtr.Zero);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Render error: {ex}");
        }
    }

    static readonly char[] prefixForeground = new[] { '\x1b', '[', '3', '8', ';', '2', ';' };
    static readonly char[] prefixBackground = new[] { '\x1b', '[', '4', '8', ';', '2', ';' };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FormatColorSequence(char[] buffer, int offset, bool isForeground, Color color)
    {
        // Format: \x1b[38;2;R;G;Bm or \x1b[48;2;R;G;Bm
        char[] prefix = isForeground ? prefixForeground : prefixBackground;

        //Buffer.BlockCopy(prefix, 0, buffer, offset * sizeof(char), prefix.Length * sizeof(char));
        CopyWithSIMD(prefix, buffer, offset);

        int currentOffset = offset + prefix.Length;

        // Write RGB values
        currentOffset += WriteNumber(buffer, currentOffset, color.R);
        buffer[currentOffset++] = ';';
        currentOffset += WriteNumber(buffer, currentOffset, color.G);
        buffer[currentOffset++] = ';';
        currentOffset += WriteNumber(buffer, currentOffset, color.B);
        buffer[currentOffset++] = 'm';

        return currentOffset - offset;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FormatColorSequence(char[] buffer, int offset, bool isForeground, byte r, byte g, byte b)
    {
        // Format: \x1b[38;2;R;G;Bm or \x1b[48;2;R;G;Bm
        char[] prefix = isForeground ? prefixForeground : prefixBackground;

        //Buffer.BlockCopy(prefix, 0, buffer, offset * sizeof(char), prefix.Length * sizeof(char));
        CopyWithSIMD(prefix, buffer, offset);

        int currentOffset = offset + prefix.Length;

        // Write RGB values
        currentOffset += WriteNumber(buffer, currentOffset, r);
        buffer[currentOffset++] = ';';
        currentOffset += WriteNumber(buffer, currentOffset, g);
        buffer[currentOffset++] = ';';
        currentOffset += WriteNumber(buffer, currentOffset, b);
        buffer[currentOffset++] = 'm';

        return currentOffset - offset;
    }

    private void CopyWithSIMD(char[] source, char[] destination, int destinationOffset)
    {
        int vectorSize = Vector<ushort>.Count; // SIMD can process multiple characters in parallel
        int i = 0;

        // Copy in chunks of `vectorSize` characters
        for (; i <= source.Length - vectorSize; i += vectorSize)
        {
            var sourceVector = new Vector<char>(source, i);
            sourceVector.CopyTo(destination, destinationOffset + i);
        }

        // Copy remaining elements
        for (; i < source.Length; i++)
        {
            destination[destinationOffset + i] = source[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WriteNumber(char[] buffer, int offset, int value)
    {
        if (value >= 100)
        {
            buffer[offset] = (char)('0' + value / 100);
            value %= 100;
            buffer[offset + 1] = (char)('0' + value / 10);
            buffer[offset + 2] = (char)('0' + value % 10);
            return 3;
        }
        else if (value >= 10)
        {
            buffer[offset] = (char)('0' + value / 10);
            buffer[offset + 1] = (char)('0' + value % 10);
            return 2;
        }
        else
        {
            buffer[offset] = (char)('0' + value);
            return 1;
        }
    }
}