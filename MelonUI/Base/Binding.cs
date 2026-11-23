using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace MelonUI.Base;

public enum BindingMode { OneWay, TwoWay, OneTime }

/// <summary>A public property, delegate or event binding, suspended when its UI owner detaches.</summary>
public sealed class Binding : IDisposable
{
    private readonly object _gate = new();
    private readonly object? _root;
    private readonly Type _rootType;
    private readonly string[]? _path;
    private readonly MemberInfo? _member;
    private readonly List<Action> _unsubscribe = new();
    private Delegate? _handler;
    private (object? Instance, EventInfo Event)? _eventSubscription;
    private Action? _changed;
    private bool _active, _disposed;
    internal Type? TargetType { get; set; }
    internal Func<object?, Type, object?>? Converter { get; set; }
    internal Func<object?, object?>? Transform { get; set; }
    public BindingMode Mode { get; set; } = BindingMode.TwoWay;

    public Binding(object? instance, PropertyInfo property) : this(instance, (MemberInfo)property) { }
    public Binding(object? instance, MethodInfo method) : this(instance, (MemberInfo)method) { }
    public Binding(object? instance, EventInfo eventInfo) : this(instance, (MemberInfo)eventInfo) { }
    private Binding(object? instance, MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);
        _root = instance; _rootType = member.DeclaringType!; _member = member;
        Validate(Resolve());
    }
    private Binding(object instanceOrType, string path, Type? targetType)
    {
        ArgumentNullException.ThrowIfNull(instanceOrType); ArgumentException.ThrowIfNullOrWhiteSpace(path);
        TargetType = targetType;
        _root = instanceOrType is Type ? null : instanceOrType;
        _rootType = instanceOrType as Type ?? instanceOrType.GetType();
        _path = path.Split('.');
        if (_path.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Binding paths cannot contain empty segments.", nameof(path));
        Validate(Resolve());
    }
    public static Binding CreatePath(object instanceOrType, string memberPath, Type? targetType = null) => new(instanceOrType, memberPath, targetType);
    private static void Validate((object? Instance, MemberInfo Member) source)
    {
        bool isStatic = source.Member switch { PropertyInfo p => (p.GetMethod ?? p.SetMethod)?.IsStatic == true,
            MethodInfo m => m.IsStatic, EventInfo e => e.AddMethod?.IsStatic == true, _ => false };
        if (!isStatic && source.Instance is null) throw new ArgumentException("An instance is required for this binding.");
        if (source.Member is PropertyInfo p2 && (p2.GetIndexParameters().Length != 0 || p2.GetMethod is not { IsPublic: true }))
            throw new ArgumentException("Bindings require a public, readable non-indexed property.");
    }
    internal static MemberInfo FindMember(Type type, string name, Type? delegateType = null)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        if (type.GetProperty(name, flags) is { } property) return property;
        if (type.GetEvent(name, flags) is { } evt) return evt;
        var methods = type.GetMethods(flags).Where(m => m.Name == name && !m.ContainsGenericParameters).ToArray();
        if (delegateType is not null)
        {
            var invoke = delegateType.GetMethod("Invoke")!;
            methods = methods.Where(m => m.ReturnType == invoke.ReturnType && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(invoke.GetParameters().Select(p => p.ParameterType))).ToArray();
        }
        return methods.Length switch { 1 => methods[0], > 1 => throw new AmbiguousMatchException($"Member '{type.Name}.{name}' is overloaded; use an unambiguous delegate signature."),
            _ => throw new MissingMemberException(type.FullName, name) };
    }
    private (object? Instance, MemberInfo Member) Resolve()
    {
        if (_member is not null) return (_root, _member);
        object? instance = _root; Type type = _rootType;
        for (int i = 0; i < _path!.Length - 1; i++)
        {
            if (type.GetNestedType(_path[i], BindingFlags.Public) is { } nested) { type = nested; instance = null; continue; }
            var prop = type.GetProperty(_path[i], BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                ?? throw new MissingMemberException(type.FullName, _path[i]);
            instance = prop.GetValue(prop.GetMethod?.IsStatic == true ? null : instance)
                ?? throw new InvalidOperationException($"Binding segment '{_path[i]}' is null.");
            type = instance.GetType();
        }
        return (instance, FindMember(type, _path[^1], TargetType is { } t && typeof(Delegate).IsAssignableFrom(t) ? t : null));
    }
    public bool IsStatic => Resolve().Member switch { PropertyInfo p => p.GetMethod?.IsStatic == true, MethodInfo m => m.IsStatic, EventInfo e => e.AddMethod?.IsStatic == true, _ => false };
    public bool IsProperty => Resolve().Member is PropertyInfo;
    public bool IsEvent => Resolve().Member is EventInfo;
    public bool IsMethod => Resolve().Member is MethodInfo;
    public EventInfo EventInfo => Resolve().Member as EventInfo ?? throw new InvalidOperationException("This binding is not an event.");
    internal bool RequiresPolling => !_disposed && IsProperty && (TargetType is null || !typeof(Delegate).IsAssignableFrom(TargetType)) && (_root is not INotifyPropertyChanged || _path is { Length: > 1 });
    internal Type SourceType => Resolve().Member switch { PropertyInfo p => p.PropertyType, EventInfo e => e.EventHandlerType!, MethodInfo m => m.ReturnType, _ => typeof(object) };
    internal bool CanWrite => Resolve().Member is PropertyInfo { SetMethod.IsPublic: true };

    public object? GetValue()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var source = Resolve();
        object? value = source.Member switch
        {
            PropertyInfo p => p.GetValue(p.GetMethod!.IsStatic ? null : source.Instance),
            MethodInfo m => m.IsStatic ? m.CreateDelegate(TargetType ?? typeof(Action)) : m.CreateDelegate(TargetType ?? typeof(Action), source.Instance),
            _ => throw new InvalidOperationException("Events do not have a readable value.")
        };
        value = TargetType is { } target ? (Converter ?? MxmlValueConverter.Convert)(value, target) : value;
        return Transform is null ? value : Transform(value);
    }
    public void SetValue(object? value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Mode != BindingMode.TwoWay) throw new InvalidOperationException($"A {Mode} binding cannot write to its source.");
        var source = Resolve();
        if (source.Member is not PropertyInfo { SetMethod.IsPublic: true } property) throw new InvalidOperationException("The binding source is read-only.");
        property.SetValue(property.SetMethod.IsStatic ? null : source.Instance, (Converter ?? MxmlValueConverter.Convert)(value, property.PropertyType));
    }
    internal void Watch(Action changed) { lock (_gate) { _changed = changed; if (_active) Rewire(); } }
    public void Subscribe(Delegate handler) { PrepareEvent(handler); Resume(); }
    internal void PrepareEvent(Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!EventInfo.EventHandlerType!.IsInstanceOfType(handler)) throw new ArgumentException("Handler signature does not match the event.", nameof(handler));
        Suspend(); _handler = handler;
    }
    public void Unsubscribe() { Suspend(); _handler = null; }
    internal void Resume()
    {
        lock (_gate)
        {
            if (_disposed || _active) return;
            _active = true;
            Rewire();
        }
    }
    internal void Suspend()
    {
        lock (_gate) { _active = false; Unwire(); }
    }
    private void Unwire()
    {
        if (_eventSubscription is { } sub) { sub.Event.RemoveEventHandler(sub.Event.AddMethod!.IsStatic ? null : sub.Instance, _handler); _eventSubscription = null; }
        foreach (var unsubscribe in _unsubscribe) unsubscribe();
        _unsubscribe.Clear();
    }
    private void Rewire()
    {
        Unwire();
        if (_handler is not null)
        {
            var (instance, member) = Resolve(); var evt = (EventInfo)member;
            evt.AddEventHandler(evt.AddMethod!.IsStatic ? null : instance, _handler); _eventSubscription = (instance, evt);
        }
        if (_changed is null) return;
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        void Observe(object? value)
        {
            if (value is null || !seen.Add(value)) return;
            if (value is INotifyPropertyChanged notify)
            { PropertyChangedEventHandler handler = (_, _) => Changed(); notify.PropertyChanged += handler; _unsubscribe.Add(() => notify.PropertyChanged -= handler); }
            if (value is INotifyCollectionChanged collection)
            { NotifyCollectionChangedEventHandler handler = (_, _) => Changed(); collection.CollectionChanged += handler; _unsubscribe.Add(() => collection.CollectionChanged -= handler); }
        }
        object? current = _root; Type type = _rootType; Observe(current);
        if (_path is not null)
            foreach (var segment in _path)
            {
                if (type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy) is not { } property) break;
                current = property.GetValue(property.GetMethod?.IsStatic == true ? null : current); Observe(current);
                if (current is null) break; type = current.GetType();
            }
        else if (_member is PropertyInfo prop) Observe(prop.GetValue(prop.GetMethod?.IsStatic == true ? null : _root));
    }
    private void Changed()
    {
        Action? changed;
        lock (_gate) { if (!_active || _disposed) return; Rewire(); changed = _changed; }
        changed?.Invoke();
    }
    public void Dispose() { lock (_gate) { if (_disposed) return; Suspend(); _handler = null; _changed = null; _disposed = true; } }

    public static Binding CreateStaticPropertyBinding(Type type, string path) => CreatePath(type, path);
    public static Binding CreatePropertyBinding(object instance, string path) => CreatePath(instance, path);
    public static Binding CreateMethodBinding(object instance, string name) => new(instance, instance.GetType().GetMethod(name, Type.EmptyTypes) ?? throw new MissingMethodException(instance.GetType().FullName, name));
    public static Binding CreateEventBinding(object instance, string name) => new(instance, instance.GetType().GetEvent(name) ?? throw new MissingMemberException(instance.GetType().FullName, name));
    public static Binding Create(object instance, string memberName) => CreatePath(instance, memberName);
}
