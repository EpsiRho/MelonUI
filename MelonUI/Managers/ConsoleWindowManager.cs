﻿using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Managers
{
    public class ConsoleWindowManager
    {
        private List<UIElement> RootElements = new List<UIElement>();
        public UIElement FocusedElement;
        private ConsoleBuffer MainBuffer;
        private Dictionary<UIElement, ConsoleBuffer> BufferCache = new();
        public string Title = "";
        public string Status = "";
        private bool IsAltHeld = false;
        public int HighestZ = 0;
        public Color TitleForeground { get; set; } = Color.White;
        public Color TitleBackground { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public Color StatusForeground { get; set; } = Color.White;
        public Color StatusBackground { get; set; } = Color.FromArgb(0, 0, 0, 0);
        StreamWriter output { get; set; }

        public ConsoleWindowManager()
        {
            Console.CursorVisible = false;
            Console.OutputEncoding = Encoding.UTF8;
            int bufferSize = 65535;
            output = new StreamWriter(
              Console.OpenStandardOutput(),
              bufferSize: bufferSize);
            UpdateBufferSize();
        }

        private UIElement FindNearestElement(UIElement current, Direction direction)
        {
            if (current == null || RootElements.Count <= 1) return null;

            // Get the center point of the current element
            int currentCenterX = current.ActualX + (current.ActualWidth / 2);
            int currentCenterY = current.ActualY + (current.ActualHeight / 2);

            UIElement nearest = null;
            double nearestDistance = double.MaxValue;
            double bestDirectionalDistance = double.MaxValue;

            foreach (var element in RootElements)
            {
                if (element == current) continue;

                // Get the center point of the candidate element
                int elementCenterX = element.ActualX + (element.ActualWidth / 2);
                int elementCenterY = element.ActualY + (element.ActualHeight / 2);

                // Calculate the distance and angle to this element
                double distance = Math.Sqrt(
                    Math.Pow(elementCenterX - currentCenterX, 2) +
                    Math.Pow(elementCenterY - currentCenterY, 2)
                );

                // Check if this element is in the correct direction
                bool isInDirection = false;
                double directionDistance = 0;

                switch (direction)
                {
                    case Direction.Up:
                        if (elementCenterY < currentCenterY)
                        {
                            isInDirection = true;
                            directionDistance = currentCenterY - elementCenterY;
                        }
                        break;
                    case Direction.Down:
                        if (elementCenterY > currentCenterY)
                        {
                            isInDirection = true;
                            directionDistance = elementCenterY - currentCenterY;
                        }
                        break;
                    case Direction.Left:
                        if (elementCenterX < currentCenterX)
                        {
                            isInDirection = true;
                            directionDistance = currentCenterX - elementCenterX;
                        }
                        break;
                    case Direction.Right:
                        if (elementCenterX > currentCenterX)
                        {
                            isInDirection = true;
                            directionDistance = elementCenterX - currentCenterX;
                        }
                        break;
                }

                // If this element is in the correct direction, consider it as a candidate
                if (isInDirection)
                {
                    // We prioritize elements that are more directly in the desired direction
                    // by using a weighted combination of distance and directional alignment
                    double weightedDistance = distance + (Math.Abs(directionDistance) * 0.8);

                    if (weightedDistance < nearestDistance)
                    {
                        nearestDistance = weightedDistance;
                        nearest = element;
                    }
                }
            }

            return nearest;
        }


        private void UpdateBufferSize()
        {
            try
            {
                int width = Math.Max(1, Console.WindowWidth);
                int height = Math.Max(1, Console.WindowHeight);

                if (MainBuffer == null)
                {
                    MainBuffer = new ConsoleBuffer(width, height);
                }
                else
                {
                    MainBuffer.Resize(width, height);
                }
            }
            catch (Exception)
            {
                // If we can't get console dimensions, create a minimal buffer
                MainBuffer = new ConsoleBuffer(1, 1);
            }
        }

        public void SetTitle(string title) => Title = title;
        public void SetStatus(string status) => Status = status;

        public void AddElement(UIElement element, bool forceFocus = true)
        {
            element.ParentWindow = this;
            if (FocusedElement == null)
            {
                FocusedElement = element;
                element.IsFocused = true;
            }

            if (forceFocus)
            {
                FocusedElement.IsFocused = false;
                FocusedElement = element;
                element.IsFocused = true;
                int count = 0;
                foreach (var elm in RootElements.OrderBy(x => x.Z))
                {
                    elm.Z = count;
                    count++;
                }
                element.Z = count;
            }

            RootElements.Add(element);
            if(HighestZ < element.Z)
            {
                HighestZ = element.Z;
            }
        }
        public void RemoveElement(UIElement element)
        {
            int idx = RootElements.IndexOf(element);
            RootElements.RemoveAt(idx);

            if (FocusedElement.Equals(element) && RootElements.Count() >= 1)
            {
                FocusedElement = RootElements.OrderByDescending(x=>x.Z).FirstOrDefault();
                FocusedElement.IsFocused = true;
            }

            int count = 0;
            foreach (var elm in RootElements.OrderBy(x => x.Z))
            {
                elm.Z = count;
                count++;
            }

            if (HighestZ == element.Z)
            {
                var item = RootElements.OrderByDescending(e => e.Z).FirstOrDefault();
                if (item == null)
                {
                    HighestZ = 0;
                }
            }

        }

        public void Render()
        {
            try
            {
                if (MainBuffer.Width != Console.WindowWidth || MainBuffer.Height != Console.WindowHeight)
                {
                    Parallel.ForEach(RootElements, (element) =>
                    {
                        element.NeedsRecalculation = true;
                    });
                }
                UpdateBufferSize();
                MainBuffer.Clear(Color.FromArgb(0,0,0,0));

                // Draw title and status
                for (int i = 0; i < Title.Length && i < MainBuffer.Width; i++)
                    MainBuffer.SetPixel(i, 0, Title[i], TitleForeground, TitleBackground);
                for (int i = 0; i < Status.Length && i < MainBuffer.Width; i++)
                    MainBuffer.SetPixel(i, 1, Status[i], StatusForeground, StatusBackground);

                // Calculate layouts and render elements
                var objectBuffers = new ConcurrentBag<(ConsoleBuffer buffer, UIElement element)>();
                var delElms = RootElements.Where(e => e.RenderThreadDeleteMe);
                foreach (var item in delElms)
                {
                    RemoveElement(item);
                }
                var visibleElms = RootElements.Where(x => x.IsVisible).ToList();
                Parallel.ForEach(visibleElms, (element) =>
                {
                    ConsoleBuffer elementBuffer = null;

                    if (element.NeedsRecalculation)
                    {
                        element.CalculateLayout(0, 2, MainBuffer.Width, MainBuffer.Height - 2);
                        elementBuffer = element.Render();

                        if (element.EnableCaching)
                        {
                            lock (BufferCache)
                            {
                                BufferCache[element] = elementBuffer;
                            }
                        }
                    }
                    else
                    {
                        bool bufferFound = false;
                        if (element.EnableCaching)
                        {
                            lock (BufferCache)
                            {
                                if (BufferCache.TryGetValue(element, out elementBuffer))
                                {
                                    bufferFound = true;
                                }
                            }
                        }

                        if (!bufferFound)
                        {
                            element.CalculateLayout(0, 2, MainBuffer.Width, MainBuffer.Height - 2);
                            elementBuffer = element.Render();

                            if (element.EnableCaching)
                            {
                                lock (BufferCache)
                                {
                                    BufferCache[element] = elementBuffer;
                                }
                            }
                        }
                    }

                    objectBuffers.Add((elementBuffer, element));
                });

                var lst = objectBuffers.OrderBy(e => e.element.Z).ToList();
                foreach (var element in lst)
                {
                    MainBuffer.WriteBuffer(element.element.ActualX, element.element.ActualY, element.buffer);
                };


                // Render the main buffer to console
                MainBuffer.RenderToConsole(output);
            }
            catch (Exception)
            {

            }
        }

        private void MoveFocus(Direction direction)
        {
            var nextElement = FindNearestElement(FocusedElement, direction);
            if (nextElement != null)
            {
                Parallel.ForEach(RootElements, (element) =>
                {
                    element.NeedsRecalculation = true;
                });
                // Unfocus the currently focused element
                FocusedElement.IsFocused = false;

                // Update Z-index for all elements
                nextElement.Z = HighestZ + 1;
                int count = 0;
                foreach (var elm in RootElements.OrderBy(x => x.Z))
                {
                    elm.Z = count;
                    count++;
                }

                // Focus the new element
                FocusedElement = nextElement;
                FocusedElement.IsFocused = true;

                // Assign the highest Z-index to the newly focused element
                //FocusedElement.Z = HighestZ;

                // Normalize Z-indexes to prevent gaps or inconsistencies
                //NormalizeZIndexes();

                // Update status to show the current selection
                SetStatus($"Selected: {FocusedElement.GetType().Name} at {FocusedElement.ActualX},{FocusedElement.ActualY}");
            }
        }
        private void NormalizeZIndexes()
        {
            // Sort elements by their current Z-index
            var sortedElements = RootElements.OrderBy(e => e.Z).ToList();

            // Assign sequential Z-index values
            for (int i = 0; i < sortedElements.Count; i++)
            {
                sortedElements[i].Z = i + 1;
            }

            // Update the highest Z-index
            HighestZ = sortedElements.Count;
        }


        public void HandleInput()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);


                if (key.Modifiers == ConsoleModifiers.Alt)
                {
                    IsAltHeld = true;
                }
                else
                {
                    IsAltHeld = false;
                }

                if (IsAltHeld)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            MoveFocus(Direction.Up);
                            break;
                        case ConsoleKey.DownArrow:
                            MoveFocus(Direction.Down);
                            break;
                        case ConsoleKey.LeftArrow:
                            MoveFocus(Direction.Left);
                            break;
                        case ConsoleKey.RightArrow:
                            MoveFocus(Direction.Right);
                            break;
                        case ConsoleKey.Tab:
                            FocusedElement.IsFocused = false;
                            FocusedElement = RootElements[RootElements.IndexOf(FocusedElement) + 1];
                            FocusedElement.IsFocused = true;
                            SetStatus($"Selected: {FocusedElement.GetType().Name} at {FocusedElement.ActualX},{FocusedElement.ActualY}");

                            break;
                    }
                }
                else
                {
                    FocusedElement?.HandleKey(key);
                }
            }
            else
            {

            }
        }

        public async Task ManageConsole(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    while (true)
                    {
                        HandleInput();
                        Render();
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
        }

    }
}
