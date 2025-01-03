using MelonUI.Attributes;
using MelonUI.Base;
using MelonUI.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
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
            var (plainText, colors) = ParseColorMarkup(Text);

            // Wrap the text and colors together
            var wrappedLines = WrapText(plainText, colors, maxWidth);

            // Calculate the vertical starting position for the entire block
            int startY = CalculateStartY(wrappedLines.Count, maxHeight);

            for (int i = 0; i < wrappedLines.Count && i + startY < maxHeight; i++)
            {
                var (line, lineColors) = wrappedLines[i];
                int lineWidth = GetStringWidth(line);
                int startX = CalculateStartX(lineWidth, maxWidth);

                RenderColoredLine(buffer, startX, startY + i, line, lineColors, defaultFg, defaultBg);
            }
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

        private (string PlainText, List<Color?> Colors) ParseColorMarkup(string text)
        {
            var plainText = text;
            var colors = new Color?[text.Length];
            Color? transparent = Color.FromArgb(0, 0, 0, 0);

            int missing = 0;
            try
            {
                var markers = ParseColors(text);
                foreach (var match in markers)
                {
                    int tagIndex = match.startIndex;
                    int length = match.endIndex - match.startIndex + 1;

                    for (int i = tagIndex; i < tagIndex + length; i++)
                    {
                        colors[i] = Color.FromArgb(0, 0, 0, 0);
                    }
                    for (int i = tagIndex + length; i < colors.Length; i++)
                    {
                        colors[i] = match.color;
                    }

                    // Remove the processed tag
                    text = text.Remove(tagIndex - missing, length);
                    missing += length;
                    plainText = text;
                }

                var fuckOff = colors.ToList();
                fuckOff.RemoveAll(x => x == transparent);
                return (plainText, fuckOff);
            }
            catch (Exception)
            {
                return (null, null);
            }
        }

        private List<(string Line, List<Color?> Colors)> WrapText(string text, List<Color?> colors, int maxWidth)
        {
            var wrappedLines = new List<(string Line, List<Color?> Colors)>();
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

                    var lineColors = colors.GetRange(start + last, length);
                    wrappedLines.Add((line, lineColors));
                    start += length;
                }
                last += paragraph.Length + 1;
            }

            return wrappedLines;
        }

        private void RenderColoredLine(ConsoleBuffer buffer, int startX, int startY, string line, List<Color?> colors, Color defaultFg, Color defaultBg)
        {
            int currentX = startX;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                Color fg = colors[i] ?? defaultFg;
                buffer.SetPixel(currentX++, startY, c, fg, defaultBg);
            }
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
