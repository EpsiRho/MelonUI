namespace MelonUI.Base;

/// <summary>Receives a property binding. The UI owner controls attachment and subscriptions.</summary>
public interface IBindingHost
{
    void SetBinding(string propertyName, Binding binding);
    bool RemoveBinding(string propertyName);
}

/// <summary>Storage used by generated properties on objects that are not UI elements.</summary>
public sealed class BindingStore
{
    private readonly Dictionary<string, Binding> _bindings = new(StringComparer.Ordinal);
    public void SetBinding(string name, Binding binding)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name); ArgumentNullException.ThrowIfNull(binding);
        if (_bindings.TryGetValue(name, out var old) && !ReferenceEquals(old, binding)) old.Dispose();
        _bindings[name] = binding;
    }
    public bool RemoveBinding(string name)
    { if (!_bindings.Remove(name, out var binding)) return false; binding.Dispose(); return true; }
    public object? GetValue(string name, object? local) => _bindings.TryGetValue(name, out var binding) ? binding.GetValue() : local;
    public void SetValue<T>(string name, T value, ref T local)
    { if (_bindings.TryGetValue(name, out var binding)) binding.SetValue(value); else local = value; }
}
