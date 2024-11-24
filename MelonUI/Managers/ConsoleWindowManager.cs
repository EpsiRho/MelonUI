using MelonUI.Base;
using MelonUI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Managers
{
    public class ConsoleWindowManager
    {
        private readonly List<UIElement> RootElements = new List<UIElement>();
        private UIElement FocusedElement;
        private ConsoleBuffer MainBuffer;
        private string Title = "";
        private string Status = "";
        private bool IsAltHeld = false;

        public ConsoleWindowManager()
        {
            Console.CursorVisible = false;
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

        public void AddElement(UIElement element)
        {
            RootElements.Add(element);
            if (FocusedElement == null)
            {
                FocusedElement = element;
                element.IsFocused = true;
            }
        }

        public void Render()
        {
            UpdateBufferSize();
            MainBuffer.Clear();

            // Draw title and status
            for (int i = 0; i < Title.Length && i < MainBuffer.Width; i++)
                MainBuffer.SetPixel(i, 0, Title[i], ConsoleColor.White, ConsoleColor.Black);
            for (int i = 0; i < Status.Length && i < MainBuffer.Width; i++)
                MainBuffer.SetPixel(i, 1, Status[i], ConsoleColor.White, ConsoleColor.Black);

            // Calculate layouts and render elements
            foreach (var element in RootElements)
            {
                element.CalculateLayout(0, 2, MainBuffer.Width, MainBuffer.Height - 2);
                var elementBuffer = element.Render();
                MainBuffer.Write(element.ActualX, element.ActualY, elementBuffer);
            }

            // Render the main buffer to console
            MainBuffer.RenderToConsole();
        }

        private void MoveFocus(Direction direction)
        {
            var nextElement = FindNearestElement(FocusedElement, direction);
            if (nextElement != null)
            {
                FocusedElement.IsFocused = false;
                FocusedElement = nextElement;
                FocusedElement.IsFocused = true;

                // Update status to show current selection
                SetStatus($"Selected: {FocusedElement.GetType().Name} at {FocusedElement.ActualX},{FocusedElement.ActualY}");
            }
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

    }
}
