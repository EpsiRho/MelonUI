using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MelonUI.Managers
{
    public class ConsoleWindowManager
    {
        public List<UIElement> RootElements = new();
        public UIElement FocusedElement;
        private ConsoleBuffer MainBuffer;
        public event EventHandler FrameRendered;
        private Dictionary<UIElement, ConsoleBuffer> BufferCache = new();
        private List<KeyboardControl> _KeyboardControls = new List<KeyboardControl>();
        public string Title = "";
        public string Status = "";
        private bool IsAltHeld = false;
        public bool EnableSystemFocusControls = true;
        public bool EnableGlobalControls = true;
        public bool EnableTitleBar = false;
        public bool IsManaged { get { return IsRendererActive && IsControllerActive; } }
        public bool IsRendererActive;
        public bool IsControllerActive;
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

        public string Screenshot(bool KeepColor)
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
                MainBuffer.Clear(Color.FromArgb(0, 0, 0, 0));

                // Draw title and status
                int bumpY = 0;
                if (EnableTitleBar)
                {
                    bumpY = 2;
                    for (int i = 0; i < Title.Length && i < MainBuffer.Width; i++)
                        MainBuffer.SetPixel(i, 0, Title[i], TitleForeground, TitleBackground);
                    for (int i = 0; i < Status.Length && i < MainBuffer.Width; i++)
                        MainBuffer.SetPixel(i, 1, Status[i], StatusForeground, StatusBackground);
                }

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
                        element.CalculateLayout(0, bumpY, MainBuffer.Width, MainBuffer.Height - bumpY);
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
                            element.CalculateLayout(0, bumpY, MainBuffer.Width, MainBuffer.Height - bumpY);
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
                    MainBuffer.WriteBuffer(element.element.ActualX, element.element.ActualY, element.buffer, element.element.RespectBackgroundOnDraw);
                };

                // Screenshot ;P
                var screenshot = MainBuffer.Screenshot(KeepColor);
                return screenshot;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private UIElement FindNearestElement(UIElement current, Direction direction)
        {
            var elms = GetAllFocusableChildren(RootElements);
            if (current == null || elms.Count <= 1) return null;

            // Get the center point of the current element
            int currentCenterX = current.ActualX + (current.ActualWidth / 2);
            int currentCenterY = current.ActualY + (current.ActualHeight / 2);

            UIElement nearest = null;
            double nearestDistance = double.MaxValue;
            double bestDirectionalDistance = double.MaxValue;

            foreach (var element in elms)
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
        public List<UIElement> GetAllFocusableChildren(List<UIElement> elms)
        {
            List<UIElement> children = new List<UIElement>();
            foreach(var elm in elms)
            {
                if (elm.ConsiderForFocus && elm.IsVisible)
                {
                    children.Add(elm);
                }
                children.AddRange(GetAllFocusableChildren(elm.Children));
            }

            return children;
        }
        public void ResetSubChildrenRecalculation(List<UIElement> elms)
        {
            foreach (var elm in elms)
            {
                if (elm.ConsiderForFocus && elm.IsVisible)
                {
                    elm.NeedsRecalculation = true;
                }
                ResetSubChildrenRecalculation(elm.Children);
            }
        }
        public UIElement GetSubChildByGuid(string uid, List<UIElement> elms = null)
        {
            if(elms == null)
            {
                elms = RootElements;
            }
            foreach (var elm in elms)
            {
                if (elm.UID == uid)
                {
                    return elm;
                }
                var check = GetSubChildByGuid(uid, elm.Children);
                if (check != null)
                {
                    return check;
                }
            }

            return null;
        }
        public UIElement GetSubChildByName(string name, List<UIElement> elms = null)
        {
            if (elms == null)
            {
                elms = RootElements;
            }
            foreach (var elm in elms)
            {
                if (elm.Name == name)
                {
                    return elm;
                }
                var check = GetSubChildByName(name, elm.Children);
                if (check != null)
                {
                    return check;
                }
            }

            return null;
        }
        public void SetSubZByGuid(int z, string uid, List<UIElement> elms = null)
        {
            if(elms == null)
            {
                elms = RootElements;
            }
            foreach (var elm in elms)
            {
                if (elm.UID == uid)
                {
                    elm.Z = z;
                    return;
                }
                SetSubZByGuid(z, uid, elm.Children);
            }
        }
        public void AddElement(UIElement element, bool forceFocus = true)
        {
            forceFocus = EnableSystemFocusControls ? forceFocus : false;
            element.ParentWindow = this;
            if (FocusedElement == null)
            {
                FocusedElement = element;
                element.IsFocused = true;
            }

            var focuselms = GetAllFocusableChildren(RootElements);
            if (element.GetType() == typeof(MUIPage) && forceFocus)
            {
                
                int count = 0;
                focuselms.AddRange(GetAllFocusableChildren(element.Children));
                foreach (var elm in focuselms.OrderBy(x => x.Z))
                {
                    var focusedItem = GetSubChildByGuid(elm.UID, RootElements);
                    if(focusedItem == null)
                    {
                        focusedItem = GetSubChildByGuid(elm.UID, element.Children);
                        if(focusedItem == null)
                        {
                            continue;
                        }
                    }
                    focusedItem.Z = count;
                    count++;
                }
                element.Z = count;
                FocusedElement.IsFocused = false;
                var id = focuselms.OrderByDescending(x => x.Z).FirstOrDefault();
                if(id == null)
                {
                    return;
                }
                FocusedElement = GetSubChildByGuid(id.UID, element.Children);
                if(FocusedElement == null)
                {
                    return;
                }
                FocusedElement.IsFocused = true;
                Parallel.ForEach(focuselms, (element) =>
                {
                    element.NeedsRecalculation = true;
                });
                RootElements.Add(element);
                if (HighestZ < element.Z)
                {
                    HighestZ = focuselms.Count;
                }
                return;
            }
            else if (forceFocus)
            {
                FocusedElement.IsFocused = false;
                FocusedElement = element;
                element.IsFocused = true;
                int count = 0;
                foreach (var elm in focuselms.OrderBy(x => x.Z))
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
            if(idx == -1)
            {
                return;
            }
            RootElements.RemoveAt(idx);
            if (FocusedElement != null && FocusedElement.Equals(element) && RootElements.Count() >= 1)
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
                    ResetSubChildrenRecalculation(RootElements);
                }
                UpdateBufferSize();
                MainBuffer.Clear(Color.FromArgb(0,0,0,0));

                // Draw title and status
                int bumpY = 0;
                if (EnableTitleBar)
                {
                    bumpY = 2;
                    for (int i = 0; i < Title.Length && i < MainBuffer.Width; i++)
                        MainBuffer.SetPixel(i, 0, Title[i], TitleForeground, TitleBackground);
                    for (int i = 0; i < Status.Length && i < MainBuffer.Width; i++)
                        MainBuffer.SetPixel(i, 1, Status[i], StatusForeground, StatusBackground);
                }

                // Calculate layouts and render elements
                var objectBuffers = new ConcurrentBag<(ConsoleBuffer buffer, UIElement element)>();
                var delElms = RootElements.Where(e => e.RenderThreadDeleteMe);
                foreach (var item in delElms)
                {
                    RemoveElement(item);
                }
                var visibleElms = RootElements.Where(x => x.IsVisible).ToList();
                //Parallel.ForEach(visibleElms, (element) => (Parallel ForEach was causing Memory Access Violation Exceptions, and perf gain was minimal so.)
                foreach (var element in visibleElms)
                {
                    ConsoleBuffer elementBuffer = null;

                    if (element.NeedsRecalculation)
                    {
                        element.CalculateLayout(0, bumpY, MainBuffer.Width, MainBuffer.Height - bumpY);
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
                        element.CalculateLayout(0, bumpY, MainBuffer.Width, MainBuffer.Height - bumpY);
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
                };

                var lst = objectBuffers.OrderBy(e => e.element.Z).ToList();
                foreach (var element in lst)
                {
                    MainBuffer.WriteBuffer(element.element.ActualX, element.element.ActualY, element.buffer, element.element.RespectBackgroundOnDraw);
                };


                // Render the main buffer to console
                MainBuffer.RenderToConsole(output);
                FrameRendered?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                MainBuffer.WriteStringWrapped(0,0,e.Message, Console.WindowWidth - 2, Color.White, Color.Transparent);
                MainBuffer.RenderToConsole(output);
                FrameRendered?.Invoke(this, EventArgs.Empty);
            }
        }

        public void MoveFocus(UIElement elm)
        {
            if (elm != null)
            {
                var elms = GetAllFocusableChildren(RootElements).OrderBy(x => x.Z).ToList();
                int count = 0;

                var focusedItem = GetSubChildByGuid(elm.UID, RootElements);
                if(focusedItem == null)
                {
                    return;
                }
                focusedItem.Z = HighestZ + 1;
                foreach (var e in elms.OrderBy(x => x.Z))
                {
                    //var child = GetSubChildByGuid(RootElements, elm.UID);
                    //if (child == null)
                    //{
                    //    return;
                    //}
                   // child.Z = count;
                    SetSubZByGuid(count, e.UID);
                    count++;
                }
                if(FocusedElement != null)
                {
                    FocusedElement.IsFocused = false;
                }
                FocusedElement = focusedItem;
                FocusedElement.IsFocused = true;

            }
        }
        public void MoveFocus(Direction direction)
        {
            if(direction == Direction.Any)
            {
                var elms = GetAllFocusableChildren(RootElements).OrderBy(x => x.Z).ToList();
                int count = 0;

                int idx = elms.IndexOf(FocusedElement) + 1;
                idx = idx > elms.Count - 1 ? 0 : idx;

                var focusedItem = GetSubChildByGuid(elms[idx].UID, RootElements);
                MoveFocus(focusedItem);
                return;
            }

            var nextElement = FindNearestElement(FocusedElement, direction);
            MoveFocus(nextElement);


        }

        public void HandleInput()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                if (!EnableGlobalControls)
                {
                    return;
                }

                if (key.Modifiers == ConsoleModifiers.Alt)
                {
                    IsAltHeld = true;
                }
                else
                {
                    IsAltHeld = false;
                }

                if (IsAltHeld && EnableSystemFocusControls)
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
                        case ConsoleKey.Oem3:
                            MoveFocus(Direction.Any);
                            break;

                    }
                }
                else
                {
                    foreach (var control in _KeyboardControls)
                    {
                        if (control.Matches(key))
                        {
                            control.Action();
                        }
                    }
                    FocusedElement?.HandleKey(key);
                }
            }
            else
            {

            }
        }
        public void RegisterKeyboardControl(ConsoleKey key, Action action, string description,
            bool requireShift = false, bool requireControl = false, bool requireAlt = false)
        {
            _KeyboardControls.Add(new KeyboardControl
            {
                Key = key,
                Action = action,
                Description = description,
                RequireShift = requireShift,
                RequireControl = requireControl,
                RequireAlt = requireAlt
            });
        }
        public void RegisterKeyboardControl(KeyboardControl keyControl)
        {
            _KeyboardControls.Add(keyControl);
        }

        public virtual IEnumerable<KeyboardControl> GetKeyboardControls()
        {
            return _KeyboardControls;
        }

        public async Task ManageConsole(CancellationToken cancellationToken)
        {
            Thread ControllerThread = new Thread(() =>
            {
                IsControllerActive = true;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        HandleInput();
                    }
                    catch (Exception ex)
                    {
                        //TODO: Logging
                        Debug.WriteLine(ex.Message);
                        MainBuffer.WriteStringWrapped(0, 0, ex.Message, Console.WindowWidth - 2, Color.White, Color.Transparent);
                        MainBuffer.RenderToConsole(output);
                        FrameRendered?.Invoke(this, EventArgs.Empty);
                    }
                }
                IsControllerActive = false;
            });
            Thread DisplayThread = new Thread(() =>
            {
                IsRendererActive = true;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Render();
                    }
                    catch (Exception ex)
                    {
                        //TODO: Logging
                        Debug.WriteLine(ex.Message);
                        MainBuffer.WriteStringWrapped(0, 0, ex.Message, Console.WindowWidth - 2, Color.White, Color.Transparent);
                        MainBuffer.RenderToConsole(output);
                        FrameRendered?.Invoke(this, EventArgs.Empty);
                    }
                }
                IsRendererActive = false;
            });
            try
            {
                DisplayThread.Start();
                ControllerThread.Start();
            }
            catch (OperationCanceledException)
            {

            }
        }

    }
}
