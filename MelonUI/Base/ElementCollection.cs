using System.Collections.ObjectModel;
using MelonUI.Managers;

namespace MelonUI.Base;

/// <summary>An owned UI tree. Removing an element detaches it without disposing it.</summary>
public sealed class ElementCollection : Collection<UIElement>
{
    private readonly UIElement? _parent;
    private readonly ConsoleWindowManager? _window;

    internal ElementCollection(UIElement parent) => _parent = parent;
    internal ElementCollection(ConsoleWindowManager window) => _window = window;
    private ConsoleWindowManager? Window => _window ?? _parent?.ParentWindow;

    private void VerifyAccess()
    {
        _parent?.ThrowIfDisposed();
        Window?.VerifyAccess();
    }

    private void Validate(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.ThrowIfDisposed();
        if (element.OwnerCollection is not null)
            throw new InvalidOperationException("Detach the element from its current owner before adding it again.");
        for (var ancestor = _parent; ancestor is not null; ancestor = ancestor.Parent)
            if (ReferenceEquals(ancestor, element))
                throw new InvalidOperationException("An element cannot contain itself or an ancestor.");
    }

    internal void ReverseOrder()
    {
        VerifyAccess();
        for (var left = 0; left < Count / 2; left++)
        {
            var right = Count - 1 - left;
            (Items[left], Items[right]) = (Items[right], Items[left]);
        }
        _parent?.Invalidate();
    }

    protected override void InsertItem(int index, UIElement item)
    {
        VerifyAccess();
        Validate(item);
        base.InsertItem(index, item);
        Attach(item);
    }

    protected override void SetItem(int index, UIElement item)
    {
        VerifyAccess();
        if (ReferenceEquals(this[index], item)) return;
        Validate(item); // Invalid replacement leaves the old tree intact.
        var previous = this[index];
        base.SetItem(index, item);
        Detach(previous);
        Attach(item);
    }

    protected override void RemoveItem(int index)
    {
        VerifyAccess();
        var item = this[index];
        base.RemoveItem(index);
        Detach(item);
    }

    protected override void ClearItems()
    {
        VerifyAccess();
        while (Count > 0) RemoveItem(Count - 1);
    }

    private void Attach(UIElement item)
    {
        item.OwnerCollection = this;
        item.AttachmentVersion++;
        item.Parent = _parent;
        item.SetWindow(Window);
        _parent?.ChildAdded(item);
        _parent?.Invalidate();
        Window?.RequestRender();
        Window?.TreeChanged();
    }

    private void Detach(UIElement item)
    {
        var window = Window;
        item.OwnerCollection = null;
        item.AttachmentVersion++;
        item.Parent = null;
        item.SetWindow(null);
        _parent?.ChildRemoved(item);
        _parent?.Invalidate();
        window?.RequestRender();
        window?.TreeRemoved(item);
    }
}
