using MelonUI.Attributes;
using MelonUI.Base;
using MelonUI.Enums;
using MelonUI.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace MelonUI.Default
{
    public partial class TextBlock : UIElement
    {
        [Binding]
        private string text;
        [Binding]

        private Alignment textAlignment = Alignment.TopLeft;
        [Binding]
        private bool sizeBasedOnText = false;

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var defaultFg = IsFocused ? FocusedForeground : Foreground;
            var defaultBg = IsFocused ? FocusedBackground : Background;

            int maxWidth = ActualWidth - (ShowBorder ? 2 : 0);
            int maxHeight = ActualHeight - (ShowBorder ? 1 : 0);

            // Parse the text with inline colors
            //var (plainText, colors) = ParseColorMarkup(Text);

            // Wrap the text and colors together
            List<string> wrappedLines = new();
            if (ContainsAnsiCodes(Text))
            {
                wrappedLines = WrapAnsiText(Text, maxWidth);
            }
            else
            {
                wrappedLines = WrapText(Text, maxWidth);
            }

            // Calculate the vertical starting position for the entire block
            int startY = CalculateStartY(wrappedLines.Count, maxHeight);

            for (int i = 0; i < wrappedLines.Count && i + startY < maxHeight; i++)
            {
                var line = wrappedLines[i];
                int lineWidth = GetStringWidth(line);
                int startX = CalculateStartX(lineWidth, maxWidth);

                RenderColoredLine(buffer, startX, startY + i, line, defaultFg, defaultBg);
            }
        }

        public static bool ContainsAnsiCodes(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            for (int i = 0; i < input.Length - 1; i++)
            {
                if (input[i] == '\x1B' && input[i + 1] == '[')
                {
                    int j = i + 2;
                    while (j < input.Length && ((input[j] >= '0' && input[j] <= '9') || input[j] == ';'))
                        j++;

                    if (j < input.Length && input[j] == 'm')
                        return true;
                }
            }
            return false;
        }
        public static List<(Color color, int startIndex, int endIndex)> ParseColors(string input)
        {
            const string markerStart = "[Color(";
            const string markerEnd = ")]";
            var results = new List<(Color, int, int)>();

            int currentIndex = 0;

            while (currentIndex < input.Length)
            {
                // Find the start index of the next marker
                int startIndex = input.IndexOf(markerStart, currentIndex);
                if (startIndex == -1) break; // No more markers

                // Find the end index of the marker
                int endIndex = input.IndexOf(markerEnd, startIndex);
                if (endIndex == -1) break; // Closing marker not found


                // Calculate the indices for extracting the inner content
                int innerStart = startIndex + markerStart.Length;
                int innerEnd = endIndex;

                // Extract the part inside the brackets
                string inner = input.Substring(innerStart, innerEnd - innerStart);

                // Split the values by comma
                string[] parts = inner.Split(',');

                // Ensure there are exactly three parts
                if (parts.Length != 3)
                {
                    currentIndex = endIndex + markerEnd.Length; // Skip to next marker
                    continue;
                }

                try
                {
                    int red = int.Parse(parts[0]);
                    int green = int.Parse(parts[1]);
                    int blue = int.Parse(parts[2]);

                    results.Add((Color.FromArgb(255,red, green, blue), startIndex, endIndex + markerEnd.Length - 1));
                }
                catch
                {

                }

                currentIndex = endIndex + markerEnd.Length;
            }

            return results;
        }
        private List<string> WrapText(string text, int maxWidth)
        {
            List<string> wrappedLines = new();
            if (string.IsNullOrEmpty(text))
            {
                return wrappedLines;
            }
            var paragraphs = text.Split('\n');

            int last = 0;
            foreach (var paragraph in paragraphs)
            {
                int start = 0;
                while (start < paragraph.Length)
                {
                    int length = Math.Min(maxWidth, paragraph.Length - start);
                    string line = paragraph.Substring(start, length);

                    if (start + length < paragraph.Length && paragraph[start + length] != ' ')
                    {
                        int lastSpace = line.LastIndexOf(' ');
                        if (lastSpace >= 0)
                        {
                            line = line.Substring(0, lastSpace);
                            length = lastSpace + 1;
                        }
                    }

                    wrappedLines.Add(line);
                    start += length;
                }
                last += paragraph.Length + 1;
            }

            return wrappedLines;
        }
        private List<string> WrapAnsiText(string text, int maxWidth)
        {
            var wrappedLines = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return wrappedLines;
            }

            var paragraphs = text.Split('\n');
            int colorIndex = 0;

            foreach (var paragraph in paragraphs)
            {
                int visibleStart = 0;
                bool isWrappedLine = false;

                while (visibleStart < GetVisibleLength(paragraph))
                {
                    int visibleLength = Math.Min(maxWidth, GetVisibleLength(paragraph) - visibleStart);
                    (string line, int actualLength) = ExtractVisibleSubstring(paragraph, visibleStart, visibleLength);

                    if (visibleStart + visibleLength < GetVisibleLength(paragraph))
                    {
                        int lastSpace = FindLastVisibleSpace(line);
                        if (lastSpace >= 0)
                        {
                            line = line.Substring(0, lastSpace);
                            actualLength = GetActualLength(line);
                            visibleStart--; // Adjust for the space that will be trimmed
                        }
                    }

                    if (isWrappedLine)
                    {
                        (string trimmedLine, int trimCount) = TrimStartWithAnsi(line);
                        line = trimmedLine;
                        actualLength -= trimCount;
                    }

                    wrappedLines.Add(line);
                    visibleStart += GetVisibleLength(line);
                    colorIndex += actualLength;
                    isWrappedLine = true;
                }
                colorIndex++; // Account for newline
            }
            return wrappedLines;
        }

        private (string text, int trimCount) TrimStartWithAnsi(string text)
        {
            int visibleTrimCount = 0;
            StringBuilder result = new StringBuilder();
            bool foundNonSpace = false;
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == '\x1b')
                {
                    int escStart = i;
                    while (i < text.Length && !char.IsLetter(text[i])) i++;
                    if (!foundNonSpace)
                    {
                        result.Append(text.Substring(escStart, i - escStart + 1));
                    }
                    i++;
                    continue;
                }

                if (!foundNonSpace)
                {
                    if (text[i] == ' ')
                    {
                        visibleTrimCount++;
                    }
                    else
                    {
                        foundNonSpace = true;
                        result.Append(text[i]);
                    }
                }
                else
                {
                    result.Append(text[i]);
                }
                i++;
            }

            return (result.ToString(), visibleTrimCount);
        }

        private int GetVisibleLength(string text)
        {
            int length = 0;
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '\x1b')
                {
                    while (i < text.Length && !char.IsLetter(text[i])) i++;
                    i++; // Skip the terminating letter
                    continue;
                }
                length++;
                i++;
            }
            return length;
        }

        private (string text, int actualLength) ExtractVisibleSubstring(string text, int visibleStart, int visibleLength)
        {
            int currentVisible = 0;
            int start = 0;

            while (currentVisible < visibleStart && start < text.Length)
            {
                if (text[start] == '\x1b')
                {
                    while (start < text.Length && !char.IsLetter(text[start])) start++;
                    start++;
                    continue;
                }
                currentVisible++;
                start++;
            }

            int end = start;
            currentVisible = 0;
            StringBuilder result = new StringBuilder();

            while (currentVisible < visibleLength && end < text.Length)
            {
                if (text[end] == '\x1b')
                {
                    int escStart = end;
                    while (end < text.Length && !char.IsLetter(text[end])) end++;
                    result.Append(text.Substring(escStart, end - escStart + 1));
                    end++;
                    continue;
                }
                result.Append(text[end]);
                currentVisible++;
                end++;
            }

            return (result.ToString(), end - start);
        }

        private int FindLastVisibleSpace(string text)
        {
            int lastSpace = -1;
            int visiblePos = -1;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\x1b')
                {
                    while (i < text.Length && !char.IsLetter(text[i])) i++;
                    continue;
                }
                visiblePos++;
                if (text[i] == ' ')
                {
                    lastSpace = i;
                }
            }
            return lastSpace;
        }

        private int GetActualLength(string text)
        {
            int length = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\x1b')
                {
                    while (i < text.Length && !char.IsLetter(text[i])) i++;
                    continue;
                }
                length++;
            }
            return length;
        }
        private void RenderColoredLine(ConsoleBuffer buffer, int startX, int startY, string line, Color defaultFg, Color defaultBg)
        {
            var buf = ColoredStringParser.ParseColoredString(line);
            buffer.WriteBuffer(startX, startY, buf);
        }

        private int CalculateStartX(int contentWidth, int maxWidth)
        {
            return TextAlignment switch
            {
                Alignment.TopCenter or Alignment.Centered or Alignment.BottomCenter =>
                    (maxWidth - contentWidth) / 2 + (ShowBorder ? 1 : 0),
                Alignment.TopRight or Alignment.CenterRight or Alignment.BottomRight =>
                    maxWidth - contentWidth + (ShowBorder ? 1 : 0),
                _ => ShowBorder ? 1 : 0
            };
        }

        private int CalculateStartY(int contentHeight, int maxHeight)
        {
            return TextAlignment switch
            {
                Alignment.CenterLeft or Alignment.Centered or Alignment.CenterRight =>
                    (maxHeight - contentHeight) / 2 + (ShowBorder ? 1 : 0),
                Alignment.BottomLeft or Alignment.BottomCenter or Alignment.BottomRight =>
                    maxHeight - contentHeight + (ShowBorder ? 1 : 0),
                _ => ShowBorder ? 1 : 0
            };
        }

        private int GetStringWidth(string str)
        {
            return ConsoleBuffer.GetStringWidth(str);
        }
    }
}
