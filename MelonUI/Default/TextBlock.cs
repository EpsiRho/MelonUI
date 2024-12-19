using MelonUI.Base;
using System;

namespace MelonUI.Default
{
    public class TextBlock : UIElement
    {
        public string Text { get; set; } = string.Empty;

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var fg = IsFocused ? FocusedForeground : Foreground;
            var bg = IsFocused ? FocusedBackground : Background;

            int maxWidth = ActualWidth - (ShowBorder ? 2 : 0);
            int maxHeight = ActualHeight - (ShowBorder ? 1 : 0);
            int startX = ShowBorder ? 1 : 0;
            int startY = ShowBorder ? 1 : 0;

            var lines = WrapText(Text, maxWidth);

            for (int i = 0; i < lines.Count && i + startY < maxHeight; i++)
            {
                buffer.WriteString(startX, startY + i, lines[i], fg, bg);
            }
        }

        private List<string> WrapText(string text, int maxWidth)
        {
            var wrappedLines = new List<string>();
            var paragraphs = text.Split('\n');

            foreach (var paragraph in paragraphs)
            {
                int start = 0;
                while (start < paragraph.Length)
                {
                    int length = Math.Min(maxWidth, paragraph.Length - start);
                    string line = paragraph.Substring(start, length);

                    // Ensure words are not split across lines
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
            }

            return wrappedLines;
        }

    }
}