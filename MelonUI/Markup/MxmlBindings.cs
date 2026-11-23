using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MelonUI.Base;

namespace MelonUI.Markup;

internal sealed partial class MxmlCompiler
{
    private readonly Dictionary<(object Target, string Property), Binding> _propertyBindings = new();
    private void AssignAttribute(object target, XAttribute attribute, Context context, UIElement owner)
        => AssignValue(target, attribute.Name.LocalName, attribute.Value, attribute, context, owner);
    private void AssignValue(object target, string name, string value, XObject attribute, Context context, UIElement owner)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            if (target.GetType().GetEvent(name, BindingFlags.Public | BindingFlags.Instance) is { } targetEvent)
            {
                if (!IsExpression(value)) Fail("MXML022", "An event handler needs a binding expression.", attribute);
                var callbackSource = Bind(value, targetEvent.EventHandlerType, context, attribute);
                if (callbackSource.GetValue() is not Delegate) Fail("MXML022", "An event needs a compatible backend method or delegate.", attribute);
                var subscription = new Binding(target, targetEvent);
                subscription.PrepareEvent(BoundEventDelegate(callbackSource, targetEvent.EventHandlerType!, owner));
                StoreOwnerBinding(owner, $"__mxml_callback_{_bindingSequence++}", callbackSource);
                StoreOwnerBinding(owner, $"__mxml_event_{_bindingSequence++}", subscription);
                return;
            }
            var methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name == name).ToArray();
            if (methods.Length == 0 || !IsExpression(value)) Fail("MXML020", $"Unknown property, event or event-handler method '{name}'.", attribute);
            var binding = Bind(value, null, context, attribute);
            if (!binding.IsEvent) Fail("MXML022", "A method attribute must reference a backend event.", attribute);
            Delegate? handler = null;
            foreach (var method in methods)
            {
                try { handler = method.CreateDelegate(binding.EventInfo.EventHandlerType!, target); break; }
                catch (ArgumentException) { }
            }
            if (handler is null) Fail("MXML022", "No public method matches the event's handler signature.", attribute);
            var original = handler!;
            var wrapped = WrapEvent(original, owner);
            binding.PrepareEvent(wrapped);
            StoreOwnerBinding(owner, $"__mxml_event_{_bindingSequence++}", binding);
            return;
        }
        if (value.StartsWith("{}", StringComparison.Ordinal)) { AssignLiteral(target, property, value[2..], attribute); return; }
        if (value is "{Null}" or "{x:Null}") { AssignLiteral(target, property, null, attribute); return; }
        if (!IsExpression(value))
        {
            if (value.StartsWith('{')) Fail("MXML022", "An unfinished binding expression needs '}', or use '{}' to escape a literal opening brace.", attribute);
            AssignLiteral(target, property, value, attribute); return;
        }
        var source = Bind(value, property.PropertyType, context, attribute);
        if (source.IsEvent) Fail("MXML022", "Assign backend events to public handler methods, not value properties.", attribute);
        if (property.SetMethod is not { IsPublic: true }) Fail("MXML021", $"Property {property.Name} is read-only.", attribute);
        var initial = source.GetValue();
        AssignLiteral(target, property, initial, attribute); // Validate conversion and signature before publishing a tree.
        if (source.Mode == BindingMode.OneTime) { source.Dispose(); return; }
        if (target is not IBindingHost bindingHost) Fail("MXML022", $"{target.GetType().Name} does not implement IBindingHost. Use Mode=OneTime or generate [Binding] properties.", attribute);
        _propertyBindings[(target, property.Name)] = source;
        if (ReferenceEquals(target, _document.Draft)) _document.Bindings[property.Name] = source;
        else
        {
            ((IBindingHost)target).SetBinding(property.Name, source);
            if (target is not UIElement) StoreOwnerBinding(owner, $"__mxml_value_{_bindingSequence++}", source);
        }
    }
    private void StoreOwnerBinding(UIElement owner, string key, Binding binding)
    { if (owner == _document.Draft) _document.Bindings[key] = binding; else owner.SetBinding(key, binding); }
    private static bool IsExpression(string value) => value.StartsWith('{') && value.EndsWith('}');
    private Binding Bind(string expression, Type? expected, Context context, XObject location)
    {
        string path = expression[1..^1].Trim(); BindingMode? requestedMode = null;
        if (path.StartsWith("Binding ", StringComparison.Ordinal))
        {
            string? bindingPath = null;
            foreach (var option in path[8..].Split(',', StringSplitOptions.TrimEntries))
            {
                var pair = option.Split('=', 2, StringSplitOptions.TrimEntries);
                if (pair.Length != 2) Fail("MXML022", "Use {Binding Path=Backend.Property, Mode=OneWay|TwoWay|OneTime}.", location);
                if (pair[0] == "Path" && bindingPath is null) bindingPath = pair[1];
                else if (pair[0] == "Mode" && requestedMode is null && Enum.TryParse<BindingMode>(pair[1], false, out var mode) && Enum.IsDefined(mode)) requestedMode = mode;
                else Fail("MXML022", $"Unknown, duplicate or invalid binding option '{pair[0]}'.", location);
            }
            path = bindingPath ?? throw new MxmlException("MXML022", "A Binding expression needs Path.", location);
        }
        var parts = path.Split('.');
        if (parts.Length < 2 || parts.Any(string.IsNullOrWhiteSpace)) Fail("MXML022", "Use a backend alias/type followed by its public member path.", location);
        object? root = null; string remaining = "";
        if (context.Backends.TryGetValue(parts[0], out var backend)) { root = backend; remaining = string.Join('.', parts.Skip(1)); }
        else
        {
            for (int count = parts.Length - 1; count > 0; count--)
            {
                try { root = ResolveType(string.Join('.', parts.Take(count)), context, location); remaining = string.Join('.', parts.Skip(count)); break; }
                catch (MxmlException) { }
            }
        }
        if (root is null) Fail("MXML022", $"Unknown backend or type in binding '{path}'.", location);
        Binding? result = null;
        Guard(location, () => result = Binding.CreatePath(root!, remaining, expected));
        result!.Converter = ConvertValue;
        result.Mode = requestedMode ?? (result.CanWrite ? BindingMode.TwoWay : BindingMode.OneWay);
        if (requestedMode == BindingMode.TwoWay && !result.CanWrite) Fail("MXML022", "TwoWay requires a public writable source property.", location);
        return result;
    }
    private static Delegate WrapEvent(Delegate callback, UIElement owner)
    {
        var parameters = callback.GetType().GetMethod("Invoke")!.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        var body = Expression.Call(typeof(MxmlCompiler).GetMethod(nameof(DeliverEvent), BindingFlags.Static | BindingFlags.NonPublic)!,
            Expression.Constant(owner), Expression.Constant(callback, typeof(Delegate)), Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object)))));
        return Expression.Lambda(callback.GetType(), body, parameters).Compile();
    }
    private static Delegate BoundEventDelegate(Binding source, Type delegateType, UIElement owner)
    {
        var parameters = delegateType.GetMethod("Invoke")!.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        var body = Expression.Call(typeof(MxmlCompiler).GetMethod(nameof(DeliverBoundEvent), BindingFlags.Static | BindingFlags.NonPublic)!,
            Expression.Constant(owner), Expression.Constant(source), Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object)))));
        return Expression.Lambda(delegateType, body, parameters).Compile();
    }
    private static void DeliverBoundEvent(UIElement owner, Binding source, object?[] args) =>
        DeliverEvent(owner, (Action)(() => ((Delegate)source.GetValue()!).DynamicInvoke(args)), []);
    private static void DeliverEvent(UIElement owner, Delegate callback, object?[] args)
    {
        if (owner.IsDisposed || owner.ParentWindow is not { } window) return;
        var version = owner.AttachmentVersion;
        void Invoke()
        {
            if (owner.IsDisposed || owner.AttachmentVersion != version) return;
            try { callback.DynamicInvoke(args); }
            catch (TargetInvocationException ex) when (ex.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
        }
        if (window.CheckAccess()) Invoke();
        else
        {
            try { window.Post(Invoke); }
            catch (InvalidOperationException) when (window.IsStopping || !window.IsRunning) { }
        }
    }

    private void AssignActions(object target, PropertyInfo property, XElement xml, Context context, UIElement owner)
    {
        var signature = property.PropertyType.GetMethod("Invoke")!;
        if (signature.ReturnType != typeof(void)) Fail("MXML030", "Action blocks require a void callback, such as Action or Action<T>.", xml);
        var operations = ReadActions(xml, context);
        if (_propertyBindings.TryGetValue((target, property.Name), out var source))
            source.Transform = value => Compose(value as Delegate, property.PropertyType, operations);
        else
            Guard(xml, () => AssignLiteral(target, property, Compose(property.GetValue(target) as Delegate, property.PropertyType, operations), xml));
    }
    private void AssignEventActions(object target, EventInfo evt, XElement xml, Context context, UIElement owner)
    {
        if (evt.EventHandlerType!.GetMethod("Invoke")!.ReturnType != typeof(void)) Fail("MXML030", "Action events must return void.", xml);
        var binding = new Binding(target, evt);
        binding.PrepareEvent(WrapEvent(Compose(null, evt.EventHandlerType, ReadActions(xml, context)), owner));
        StoreOwnerBinding(owner, $"__mxml_event_{_bindingSequence++}", binding);
    }
    private Action[] ReadActions(XElement xml, Context context)
    {
        var operations = new List<Action>();
        if (xml.Attribute("MXMLFlags") is { } flags) ParseFlags(flags.Value, context, flags, operations);
        foreach (var node in xml.Elements())
        {
            if (node.Name.LocalName == "Actions") foreach (var action in node.Elements()) ActionNode(action, context, operations);
            else ActionNode(node, context, operations);
        }
        if (operations.Count == 0) Fail("MXML030", "An action property needs at least one action.", xml);
        return operations.ToArray();
    }
    private static Delegate Compose(Delegate? original, Type delegateType, Action[] operations)
    {
        var args = delegateType.GetMethod("Invoke")!.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        var statements = new List<Expression>();
        if (original is not null) statements.Add(Expression.Invoke(Expression.Constant(original, delegateType), args));
        statements.AddRange(operations.Select(action => Expression.Invoke(Expression.Constant(action))));
        return Expression.Lambda(delegateType, Expression.Block(statements), args).Compile();
    }
    private void ActionNode(XElement node, Context context, List<Action> operations)
    {
        string name = node.Name.LocalName;
        var allowed = name switch
        {
            "Show" or "Toggle" => new[] { "Target", "Focus" }, "Hide" or "Focus" => new[] { "Target" },
            "Set" => new[] { "Target", "Property", "Value" }, "Invoke" => new[] { "Path" }, _ => Array.Empty<string>()
        };
        if (allowed.Length == 0 || node.HasElements || node.Attributes().Any(a => !allowed.Contains(a.Name.LocalName))) Fail("MXML030", $"Unknown or malformed action '{name}'.", node);
        if (name == "Invoke")
        {
            var path = node.Attribute("Path") ?? throw new MxmlException("MXML030", "Invoke needs Path.", node);
            var binding = Bind("{" + path.Value.Trim('{', '}') + "}", typeof(Action), context, path);
            var action = binding.GetValue() as Action ?? throw new MxmlException("MXML030", "Invoke requires a parameterless void backend method/delegate.", path);
            operations.Add(action); binding.Dispose(); return;
        }
        var target = node.Attribute("Target") ?? throw new MxmlException("MXML030", $"{name} needs Target.", node);
        bool focus = name == "Show";
        if (node.Attribute("Focus") is { } focusAttribute) Guard(focusAttribute, () => focus = bool.Parse(focusAttribute.Value));
        AddTargetOperation(name, target.Value, focus, context, node, operations, node.Attribute("Property")?.Value, node.Attribute("Value")?.Value);
    }
    private void ParseFlags(string flags, Context context, XObject location, List<Action> operations)
    {
        int position = 0;
        var pattern = new Regex(@"\G\s*(Show|Hide|Toggle|Focus)\(\s*([^()]*)\s*\)\s*(?:[,;]\s*|$)", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        while (position < flags.Length)
        {
            var match = pattern.Match(flags, position);
            if (!match.Success) Fail("MXML030", "Use ordered Show(Name[:true|false]), Hide(Name), Toggle(Name) or Focus(Name) flags.", location);
            string name = match.Groups[1].Value;
            var arguments = match.Groups[2].Value.Split(new[] { ':', ',' }, StringSplitOptions.TrimEntries);
            if (arguments.Length > (name is "Show" or "Toggle" ? 2 : 1) || string.IsNullOrWhiteSpace(arguments[0])) Fail("MXML030", "Invalid action arguments.", location);
            bool focus = name == "Show";
            if (arguments.Length == 2) Guard(location, () => focus = bool.Parse(arguments[1]));
            AddTargetOperation(name, arguments[0], focus, context, location, operations);
            position = match.Index + match.Length;
        }
        if (position == 0) Fail("MXML030", "Remove an empty MXMLFlags attribute.", location);
    }
    private void AddTargetOperation(string name, string path, bool focus, Context context, XObject location, List<Action> operations, string? propertyName = null, string? value = null)
    {
        UIElement? target = null; PropertyInfo? property = null; object? converted = null;
        if (string.IsNullOrWhiteSpace(path)) Fail("MXML031", "An action target cannot be empty.", location);
        _validate.Add(() =>
        {
            target = ResolveTarget(context, path, location);
            if (name == "Set")
            {
                if (propertyName is null || value is null) Fail("MXML030", "Set needs Property and Value.", location);
                property = Property(target, propertyName!, location);
                if (property.SetMethod is not { IsPublic: true }) Fail("MXML030", "Set requires a writable public property.", location);
                Guard(location, () => converted = ConvertValue(value, property.PropertyType));
            }
        });
        operations.Add(() =>
        {
            var element = Live(target!);
            if (element.IsDisposed || !element.IsWithin(Live(context.Scope))) return;
            switch (name)
            {
                case "Show": element.IsVisible = true; break;
                case "Hide": element.IsVisible = false; return;
                case "Toggle": element.IsVisible = !element.IsVisible; break;
                case "Focus": element.ParentWindow?.Focus(element); return;
                case "Set": property!.SetValue(element, converted); return;
            }
            if (focus && element.IsEffectivelyVisible && element.IsEffectivelyEnabled &&
                (element.CanFocus || ElementTree.Descendants(element.Children).Any(e => e.CanFocus))) element.ParentWindow?.Focus(element);
        });
    }
}
