namespace MelonUI.Base;

internal static class ElementTree
{
    internal static IEnumerable<UIElement> Descendants(IEnumerable<UIElement> elements)
    {
        foreach (var element in elements)
        {
            yield return element;
            foreach (var child in Descendants(element.Children)) yield return child;
        }
    }

    internal static T? Find<T>(IEnumerable<UIElement> elements, string name) where T : UIElement
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var matches = Descendants(elements).Where(x => x.Name == name).Take(2).ToArray();
        if (matches.Length > 1)
            throw new InvalidOperationException($"More than one element is named '{name}'. Search within its page or container.");
        if (matches.Length == 0) return null;
        return matches[0] as T ?? throw new InvalidOperationException($"Element '{name}' is not a {typeof(T).Name}.");
    }
}
