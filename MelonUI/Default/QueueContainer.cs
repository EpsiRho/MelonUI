using MelonUI.Base;
using MelonUI.Enums;
using MelonUI.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.ConstrainedExecution;
namespace MelonUI.Default
{
    public class QueueContainer : UIElement
    {
        public Direction StackDirection { get; set; } = Direction.Right;
        public int MaxStackDisplaySize = 5;
        public ConsoleWindowManager _parentWindow;
        public override bool ShowBorder { get; set; } = false;
        public override ConsoleWindowManager ParentWindow
        {
            get
            {
                return _parentWindow;
            }
            set
            {
                if (value != _parentWindow)
                {
                    SetParentWindows(value);
                }
            }
        }

        public QueueContainer()
        {
        }
        public void SetParentWindows(ConsoleWindowManager parent)
        {
            this._parentWindow = parent;
            foreach(var pos in Children)
            {
                pos.ParentWindow = parent;
            }
        }

        public void QueueElement(UIElement element)
        {
            element.Parent = this;
            element.ParentWindow = this.ParentWindow;
            Children.Add(element);

            foreach (var control in element.GetKeyboardControls())
            {
                if(!control.Key.HasValue)
                {
                    control.Key = ConsoleKey.None;
                }
            }
            NeedsRecalculation = true;
        }
        public void InsertElementAt(UIElement element, int idx)
        {
            element.Parent = this;
            element.ParentWindow = this.ParentWindow;
            Children.Insert(idx, element);

            foreach (var control in element.GetKeyboardControls())
            {
                if(!control.Key.HasValue)
                {
                    control.Key = ConsoleKey.None;
                }
            }
            NeedsRecalculation = true;
        }
        public void PopElement()
        {
            RemoveElementAt(0);
        }
        public void RemoveElementAt(int idx)
        {
            Children.RemoveAt(idx);
            NeedsRecalculation = true;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            // Initial Size
            int fullInnerWidth = buffer.Width - (ShowBorder ? 2 : 0);
            int fullInnerHeight = buffer.Height - (ShowBorder ? 2 : 0);
            int innerWidth = buffer.Width - (ShowBorder ? 2 : 0);
            int innerHeight = buffer.Height - (ShowBorder ? 2 : 0);

            // Adjust size to account for stack frames
            switch (StackDirection)
            {
                case Direction.Right:
                    innerWidth -= Children.Count - 1 > MaxStackDisplaySize ? MaxStackDisplaySize : Children.Count - 1;
                    break;
            }

            var borderClrF = IsFocused ? FocusedForeground : Foreground;
            var borderClrB = IsFocused ? FocusedBackground : Background;

            // Render Preview Frames
            switch (StackDirection)
            {
                case Direction.Right:
                    for(int y = 1; y < innerHeight - 1; y++)
                    {
                        for(int x = innerWidth; x < fullInnerWidth; x++)
                        {
                            int modifier = (30 * (MaxStackDisplaySize - (fullInnerWidth - (x))));
                            var clr = Color.FromArgb(255, 
                                borderClrF.R - modifier > 20 ? borderClrF.R - modifier : 20,
                                borderClrF.B - modifier > 20 ? borderClrF.B - modifier : 20,
                                borderClrF.G - modifier > 20 ? borderClrF.G - modifier : 20);
                            buffer.SetPixel(x, y, BoxVertical, clr, borderClrB);
                        }
                    }
                    for (int x = innerWidth; x < fullInnerWidth; x++)
                    {
                        int modifier = (30 * (MaxStackDisplaySize - (fullInnerWidth - (x))));
                        var clr = Color.FromArgb(255,
                            borderClrF.R - modifier > 20 ? borderClrF.R - modifier : 20,
                            borderClrF.B - modifier > 20 ? borderClrF.B - modifier : 20,
                            borderClrF.G - modifier > 20 ? borderClrF.G - modifier : 20);
                        buffer.SetPixel(x, 0, BoxTopRight, clr, borderClrB);
                        buffer.SetPixel(x, innerHeight - 1, BoxBottomRight, clr, borderClrB);
                    }
                    break;
            }

            // Render inner content
            var curElm = Children.FirstOrDefault();
            if(curElm != null)
            {
                curElm.Width = innerWidth.ToString();
                curElm.Height = innerHeight.ToString();
                curElm.IsFocused = true;

                curElm.CalculateLayout(0, 0, innerWidth, innerHeight);
                var elementBuffer = curElm.Render();
                buffer.Write(curElm.ActualX, curElm.ActualY, elementBuffer);
            }
        }

        public override void HandleKey(ConsoleKeyInfo keyInfo)
        {
            var currentElm = Children.FirstOrDefault();
            if (currentElm != null)
            {
                currentElm.HandleKey(keyInfo);
            }
        }
    }
}
