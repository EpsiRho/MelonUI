using System.Drawing;
using MelonUI.Attributes;
using MelonUI.Base;
using MelonUI.Enums;

namespace MelonUI.Default;

public partial class QueueContainer : UIElement
{
    [Binding] private Direction stackDirection = Direction.Right;
    [Binding] private int maxStackDisplaySize = 5;
    public UIElement? CurrentElement => Children.FirstOrDefault(c => c.IsVisible);
    protected internal override IEnumerable<UIElement> FocusChildren => CurrentElement is { } current ? [current] : [];
    public QueueContainer() { ConsiderForFocus = false; EnableCaching = false; }
    public void QueueElement(UIElement element) => Children.Add(element);
    public void InsertElementAt(UIElement element, int index) => Children.Insert(index, element);
    public UIElement? PopElement() { VerifyAccess(); var element = Children.FirstOrDefault(); element?.Remove(); return element; }
    public void RemoveElementAt(int index) => Children.RemoveAt(index);
    protected override void LayoutChildren()
    {
        var a = ContentRect;
        int count = Children.Count(c => c.IsVisible);
        int extent = StackDirection is Direction.Up or Direction.Down ? a.Height : a.Width;
        int depth = Math.Clamp(count - 1, 0, Math.Max(0, Math.Min(MaxStackDisplaySize, extent - 1)));
        int x = a.X + (StackDirection == Direction.Left ? depth : 0);
        int y = a.Y + (StackDirection == Direction.Up ? depth : 0);
        foreach (var child in Children.ToArray())
            if (child == CurrentElement) child.CalculateLayout(x, y, Math.Max(0, a.Width - (StackDirection is Direction.Left or Direction.Right ? depth : 0)), Math.Max(0, a.Height - (StackDirection is Direction.Up or Direction.Down ? depth : 0)));
            else child.CalculateLayout(a.X, a.Y, 0, 0);
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        int count = Children.Count(c => c.IsVisible);
        int extent = StackDirection is Direction.Up or Direction.Down ? a.Height : a.Width;
        int depth = Math.Clamp(count - 1, 0, Math.Max(0, Math.Min(MaxStackDisplaySize, extent - 1)));
        for (int i = 0; i < depth; i++)
        {
            var tint = Color.FromArgb(Foreground.A, Math.Max(20, Foreground.R - i * 20), Math.Max(20, Foreground.G - i * 20), Math.Max(20, Foreground.B - i * 20));
            if (StackDirection is Direction.Up or Direction.Down)
            {
                int y = StackDirection == Direction.Up ? a.Y + i : a.Y + a.Height - 1 - i;
                for (int x = a.X; x < a.X + a.Width; x++) buffer.SetPixel(x, y, '─', tint, Background);
            }
            else
            {
                int x = StackDirection == Direction.Left ? a.X + i : a.X + a.Width - 1 - i;
                for (int y = a.Y; y < a.Y + a.Height; y++) buffer.SetPixel(x, y, '│', tint, Background);
            }
        }
        if (CurrentElement is { ActualWidth: > 0, ActualHeight: > 0 } child) buffer.WriteBuffer(child.ActualX, child.ActualY, child.Render(), child.RespectBackgroundOnDraw);
    }
}
