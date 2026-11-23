namespace MelonUI.Base;

public sealed class KeyboardControl : IBindingHost
{
    private readonly BindingStore _bindings = new();
    private ConsoleKey? _key;
    private bool _shift, _control, _alt;
    private Func<ConsoleKeyInfo, bool>? _wildcard;
    private Action? _action;
    private string? _description = "";
    public ConsoleKey? Key { get => (ConsoleKey?)_bindings.GetValue(nameof(Key), _key); set => _bindings.SetValue(nameof(Key), value, ref _key); }
    public ConsoleKeyInfo? KeyInfo { get; set; }
    public bool RequireShift { get => (bool)_bindings.GetValue(nameof(RequireShift), _shift)!; set => _bindings.SetValue(nameof(RequireShift), value, ref _shift); }
    public bool RequireControl { get => (bool)_bindings.GetValue(nameof(RequireControl), _control)!; set => _bindings.SetValue(nameof(RequireControl), value, ref _control); }
    public bool RequireAlt { get => (bool)_bindings.GetValue(nameof(RequireAlt), _alt)!; set => _bindings.SetValue(nameof(RequireAlt), value, ref _alt); }
    public Func<ConsoleKeyInfo, bool>? Wildcard { get => (Func<ConsoleKeyInfo, bool>?)_bindings.GetValue(nameof(Wildcard), _wildcard); set => _bindings.SetValue(nameof(Wildcard), value, ref _wildcard); }
    public Action? Action { get => (Action?)_bindings.GetValue(nameof(Action), _action); set => _bindings.SetValue(nameof(Action), value, ref _action); }
    public string? Description { get => (string?)_bindings.GetValue(nameof(Description), _description); set => _bindings.SetValue(nameof(Description), value, ref _description); }
    public void SetBinding(string name, Binding binding) => _bindings.SetBinding(name, binding);
    public bool RemoveBinding(string name) => _bindings.RemoveBinding(name);
    public bool Matches(ConsoleKeyInfo keyInfo)
    {
        KeyInfo = keyInfo;
        if (Wildcard is { } wildcard) return wildcard(keyInfo);
        return keyInfo.Key == Key && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) == RequireShift &&
            keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) == RequireControl && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt) == RequireAlt;
    }
}
