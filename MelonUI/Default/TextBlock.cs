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
            Color defaultFg = IsFocused ? FocusedForeground : Foreground;
            Color defaultBg = IsFocused ? FocusedBackground : Background;

            // Internal area shrinks if we have borders:
            int maxWidth = ActualWidth - (ShowBorder ? 2 : 0);
            int maxHeight = ActualHeight - (ShowBorder ? 2 : 0);

            // Step 1: Convert [Color(r,g,b)] + [ColorReset] => ANSI codes
            string processedText = ConvertMarkersToAnsi(Text);

            // Step 2: Wrap text, using an approach that never breaks in the middle of words
            List<string> wrappedLines = ContainsAnsiCodes(processedText)
                ? WrapAnsiText(processedText, maxWidth)
                : WrapPlainText(processedText, maxWidth);

            // The total line count
            int contentHeight = wrappedLines.Count;

            // Compute vertical alignment offset
            int startY = CalculateStartY(contentHeight, maxHeight);

            // Render each line
            for (int i = 0; i < wrappedLines.Count && (i + startY) < (maxHeight + (ShowBorder ? 1 : 0)); i++)
            {
                string line = wrappedLines[i];

                // For alignment, measure visible width ignoring ANSI
                int lineWidth = ParamParser.GetVisibleLength(line);

                // Compute horizontal alignment offset
                int startX = CalculateStartX(lineWidth, maxWidth);

                // Render at (startX, startY + i)
                RenderColoredLine(buffer, startX, startY + i, line, defaultFg, defaultBg);
            }
        }

        // Replace [Color(r,g,b)] => ESC[38;2;r;g;bm, and [ColorReset] => ESC[0m
        private string ConvertMarkersToAnsi(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Replace [ColorReset] => ESC[0m
            input = Regex.Replace(input, @"\[ColorReset\]", "\x1b[0m");

            // Parse each [Color(r,g,b)] block
            var colorRanges = ParseColors(input);
            if (colorRanges.Count == 0)
                return input;

            var sb = new StringBuilder();
            int currentIndex = 0;

            foreach (var (color, startIndex, endIndex) in colorRanges)
            {
                // Copy everything prior to marker
                if (startIndex > currentIndex)
                    sb.Append(input, currentIndex, startIndex - currentIndex);

                // Insert ANSI color code
                sb.Append($"\x1b[38;2;{color.R};{color.G};{color.B}m");

                // Advance index
                currentIndex = endIndex + 1;
            }
            // Append trailing text
            if (currentIndex < input.Length)
                sb.Append(input, currentIndex, input.Length - currentIndex);

            return sb.ToString();
        }

        // Finds [Color(r,g,b)] markers and returns the color & [start..end] coverage.
        public static List<(Color color, int startIndex, int endIndex)> ParseColors(string input)
        {
            const string markerStart = "[Color(";
            const string markerEnd = ")]";

            var results = new List<(Color, int, int)>();
            int idx = 0;
            while (idx < input.Length)
            {
                int startIndex = input.IndexOf(markerStart, idx);
                if (startIndex < 0) break;
                int endIndex = input.IndexOf(markerEnd, startIndex);
                if (endIndex < 0) break;

                int innerStart = startIndex + markerStart.Length;
                int innerLen = endIndex - innerStart;
                string inside = input.Substring(innerStart, innerLen);

                var parts = inside.Split(',');
                if (parts.Length == 3)
                {
                    try
                    {
                        int r = int.Parse(parts[0].Trim());
                        int g = int.Parse(parts[1].Trim());
                        int b = int.Parse(parts[2].Trim());
                        var color = Color.FromArgb(r, g, b);

                        // The entire marker is "[Color(r,g,b)]"
                        int markerFullEnd = endIndex + markerEnd.Length - 1;
                        results.Add((color, startIndex, markerFullEnd));
                    }
                    catch
                    {
                        // ignore parse errors
                    }
                }
                idx = endIndex + markerEnd.Length;
            }
            return results;
        }

        // Check for any ANSI ESC + '[' + ??? + 'm' codes
        public static bool ContainsAnsiCodes(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            for (int i = 0; i < input.Length - 1; i++)
            {
                if (input[i] == '\x1b' && i + 1 < input.Length && input[i + 1] == '[')
                {
                    int j = i + 2;
                    while (j < input.Length && (char.IsDigit(input[j]) || input[j] == ';'))
                        j++;
                    if (j < input.Length && input[j] == 'm')
                        return true;
                }
            }
            return false;
        }

        private List<string> WrapPlainText(string text, int maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
                return lines;

            // Break on actual newlines first
            string[] paragraphs = text.Split('\n');
            foreach (string paragraph in paragraphs)
            {
                // Split the paragraph into words
                string[] words = paragraph.Split(' ');
                var sb = new StringBuilder();
                foreach (var word in words)
                {
                    if (word.Length == 0)
                    {
                        // skip empty
                        continue;
                    }

                    // If adding this word (plus a space if needed) exceeds maxWidth, we start a new line
                    if (sb.Length > 0 && (sb.Length + 1 + word.Length) > maxWidth)
                    {
                        lines.Add(sb.ToString());
                        sb.Clear();
                    }

                    // Append a space if it's not the first word in the line
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(word);
                }

                // Add whatever is left in the line buffer
                if (sb.Length > 0)
                {
                    lines.Add(sb.ToString());
                }
            }
            return lines;
        }

        private List<string> WrapAnsiText(string text, int maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
                return lines;

            // Split on newlines first
            string[] paragraphs = text.Split('\n');
            foreach (var paragraph in paragraphs)
            {
                // We'll treat "words" as chunks separated by visible spaces. 
                // Because we have ANSI codes, we can't just do `paragraph.Split(' ')`.
                // Instead, let's parse a "visible" word approach:
                List<string> words = SplitAnsiParagraphIntoWords(paragraph);

                var sb = new StringBuilder();
                int currentVisLen = 0; // visible length so far in this line

                foreach (var word in words)
                {
                    int wLen = ParamParser.GetVisibleLength(word);
                    // if adding this word (plus a space) would exceed line
                    if (currentVisLen > 0 && (currentVisLen + 1 + wLen) > maxWidth)
                    {
                        // finalize previous line
                        lines.Add(sb.ToString());
                        sb.Clear();
                        currentVisLen = 0;
                    }

                    // add space if needed
                    if (currentVisLen > 0)
                    {
                        sb.Append(' ');
                        currentVisLen++;
                    }

                    sb.Append(word);
                    currentVisLen += wLen;
                }

                // leftover line buffer
                if (sb.Length > 0)
                {
                    lines.Add(sb.ToString());
                }
            }
            return lines;
        }

        private List<string> SplitAnsiParagraphIntoWords(string paragraph)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            int i = 0;
            while (i < paragraph.Length)
            {
                if (paragraph[i] == '\x1b')
                {
                    // Copy the entire escape code verbatim
                    int escStart = i;
                    i++;
                    while (i < paragraph.Length && !char.IsLetter(paragraph[i]))
                        i++;
                    if (i < paragraph.Length) i++;

                    // Append that entire escape code to whatever we're building
                    sb.Append(paragraph, escStart, i - escStart);
                }
                else if (paragraph[i] == ' ')
                {
                    // We found a visible space => finish the current word
                    if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    i++;
                }
                else
                {
                    // a normal character
                    sb.Append(paragraph[i]);
                    i++;
                }
            }

            // leftover partial word
            if (sb.Length > 0)
            {
                result.Add(sb.ToString());
            }

            return result;
        }

        private int CalculateStartX(int contentWidth, int maxWidth)
        {
            int borderOffset = ShowBorder ? 1 : 0;
            int x;

            switch (TextAlignment)
            {
                case Alignment.TopCenter:
                case Alignment.Centered:
                case Alignment.BottomCenter:
                    x = (maxWidth - contentWidth) / 2 + borderOffset;
                    break;
                case Alignment.TopRight:
                case Alignment.CenterRight:
                case Alignment.BottomRight:
                    x = maxWidth - contentWidth + borderOffset;
                    break;
                default:
                    x = borderOffset; // left
                    break;
            }

            // Clamp so it doesn't go off left
            if (x < borderOffset)
                x = borderOffset;

            return x;
        }

        private int CalculateStartY(int contentHeight, int maxHeight)
        {
            int borderOffset = ShowBorder ? 1 : 0;
            int y;

            switch (TextAlignment)
            {
                case Alignment.CenterLeft:
                case Alignment.Centered:
                case Alignment.CenterRight:
                    y = (maxHeight - contentHeight) / 2 + borderOffset;
                    break;
                case Alignment.BottomLeft:
                case Alignment.BottomCenter:
                case Alignment.BottomRight:
                    y = maxHeight - contentHeight + borderOffset;
                    break;
                default:
                    y = borderOffset; // top
                    break;
            }

            if (y < borderOffset)
                y = borderOffset;

            return y;
        }

        private void RenderColoredLine(ConsoleBuffer buffer, int startX, int startY, string line, Color defaultFg, Color defaultBg)
        {
            // parse ANSI into segments
            var segments = ColoredStringParser.ParseColoredString(line);
            // pass to buffer
            buffer.WriteBuffer(startX, startY, segments);
        }
    }
}
