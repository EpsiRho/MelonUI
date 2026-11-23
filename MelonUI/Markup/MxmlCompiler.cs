using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using MelonUI.Base;
using MelonUI.Default;

namespace MelonUI.Markup;

internal sealed class MxmlException : Exception
{
    internal string Code { get; }
    internal string? SourcePath { get; }
    internal int Line { get; }
    internal int Column { get; }
    internal MxmlException(string code, string message, XObject? location = null, string? source = null) : base(message)
    {
        Code = code; SourcePath = source ?? (Uri.TryCreate(location?.BaseUri, UriKind.Absolute, out var uri) && uri.IsFile ? uri.LocalPath : null);
        var info = location as IXmlLineInfo; Line = info?.HasLineInfo() == true ? info.LineNumber : -1; Column = info?.HasLineInfo() == true ? info.LinePosition : -1;
    }
}

internal sealed class MxmlDocument
{
    internal MUIPage Draft { get; } = new();
    internal MUIPage LiveRoot { get; set; }
    internal Dictionary<PropertyInfo, object?> Assignments { get; } = new();
    internal Dictionary<string, Binding> Bindings { get; } = new(StringComparer.Ordinal);
    internal List<IDisposable> Resources { get; private set; } = new();
    internal int ElementCount { get; set; }
    internal bool Committed { get; set; }
    internal MxmlDocument() => LiveRoot = Draft;
    internal List<IDisposable> TakeResources() { var result = Resources; Resources = new(); return result; }
}

internal sealed partial class MxmlCompiler : IDisposable
{
    private sealed class Context
    {
        internal required MUIPage Scope;
        internal string? File;
        internal List<Assembly> Assemblies = new();
        internal List<string> Namespaces = new();
        internal Dictionary<string, object> Backends = new(StringComparer.Ordinal);
        internal Context Fork(MUIPage page, string? file) => new()
        { Scope = page, File = file, Assemblies = new(Assemblies), Namespaces = new(Namespaces), Backends = new(Backends, StringComparer.Ordinal) };
    }
    private readonly MUIPage _host;
    private readonly MxmlCompilerOptions _options;
    private readonly MxmlDocument _document = new();
    private readonly HashSet<string> _includes = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly List<Action> _validate = new();
    private readonly List<UIElement> _constructed = new();
    private readonly Dictionary<UIElement, XObject> _locations = new();
    private int _bindingSequence;
    internal MxmlCompiler(MUIPage host) { _host = host; _options = host.CompilerOptions; }

    internal MxmlDocument Build(string markup, string? source)
    {
        if (_options.MaxIncludeDepth < 1 || _options.MaxDocumentCharacters < 1) throw new ArgumentOutOfRangeException(nameof(_options));
        var context = new Context { Scope = _document.Draft, File = source };
        context.Assemblies.Add(typeof(MUIPage).Assembly);
        foreach (var assembly in _options.Assemblies.Concat(_host.BackendObjects.Values.Select(b => b.GetType().Assembly)).Distinct())
            if (!context.Assemblies.Contains(assembly)) context.Assemblies.Add(assembly);
        context.Namespaces.AddRange(new[] { "MelonUI.Base", "MelonUI.Default" }.Concat(_options.Namespaces).Distinct());
        foreach (var backend in _host.BackendObjects) context.Backends.Add(backend.Key, backend.Value);
        if (source is not null) _includes.Add(source);
        PopulatePage(_document.Draft, ReadXml(markup, source), context, 0);
        foreach (var validate in _validate) validate();
        return _document;
    }
    private XElement ReadXml(string markup, string? source)
    {
        try
        {
            using var text = new StringReader(markup);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = _options.MaxDocumentCharacters };
            using var reader = XmlReader.Create(text, settings, source is null ? "" : new Uri(source).AbsoluteUri);
            return XElement.Load(reader, LoadOptions.SetLineInfo | LoadOptions.SetBaseUri | LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new MxmlParseException(ex.Message, source, ex.LineNumber, ex.LinePosition);
        }
    }
    private void PopulatePage(MUIPage page, XElement xml, Context context, int depth)
    {
        if (xml.Name.LocalName != nameof(MUIPage) && xml.Name.LocalName != page.GetType().Name) Fail("MXML002", "The document root must be MUIPage.", xml);
        Declarations(xml, context);
        Populate(page, xml, context, page, depth);
        page.CompiledBackends = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new Dictionary<string, object>(context.Backends, StringComparer.Ordinal));
        _validate.Add(() => ValidateNames(page));
    }
    private static IEnumerable<string> SplitList(string value) => value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.Ordinal);
    private void Declarations(XElement xml, Context context)
    {
        if (xml.Attribute("MxmlVersion") is { } version && version.Value != MxmlCompilerOptions.CurrentLanguageVersion.ToString(CultureInfo.InvariantCulture))
            Fail("MXML004", $"Unsupported MxmlVersion '{version.Value}'. This library supports version 1.", version);
        // Explicit phases make XML attribute ordering irrelevant.
        if (xml.Attribute("Assemblies") is { } assemblies)
            Guard(assemblies, () =>
            {
                foreach (var name in SplitList(assemblies.Value))
                {
                    var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name) ?? Assembly.Load(new AssemblyName(name));
                    if (!context.Assemblies.Contains(assembly)) context.Assemblies.Add(assembly);
                }
            });
        if (xml.Attribute("Namespaces") is { } spaces)
            context.Namespaces.AddRange(SplitList(spaces.Value).Where(s => !context.Namespaces.Contains(s)));
        if (xml.Attribute("Backends") is { } backends)
            foreach (var declaration in SplitList(backends.Value))
            {
                var pair = declaration.Split('=', 2, StringSplitOptions.TrimEntries);
                var type = ResolveType(pair[^1], context, backends);
                string alias = pair.Length == 2 ? pair[0] : type.Name;
                if (context.Backends.TryGetValue(alias, out var external))
                {
                    var existingType = external as Type ?? external.GetType();
                    if (existingType != type) Fail("MXML011", $"Backend alias '{alias}' is already assigned to {existingType.Name}.", backends);
                    continue;
                }
                if (type.IsAbstract && type.IsSealed) context.Backends.Add(alias, type);
                else
                {
                    var instance = Construct(type, backends);
                    context.Backends.Add(alias, instance);
                    if (instance is IDisposable resource) Own(context.Scope, resource);
                }
            }
    }
    private object Construct(Type type, XObject location)
    {
        object? result = null;
        Guard(location, () => result = _options.Factories.TryGetValue(type, out var factory) ? factory() : Activator.CreateInstance(type));
        if (result is null || !type.IsInstanceOfType(result)) Fail("MXML012", $"A factory for {type.Name} must return a new compatible instance.", location);
        if (result is UIElement element)
        {
            if (element.IsDisposed || element.Parent is not null || element.ParentWindow is not null) Fail("MXML012", "Factories must return unattached, undisposed elements.", location);
            if (_constructed.Contains(element)) Fail("MXML012", "Factories cannot return the same element more than once.", location);
            _constructed.Add(element); _locations[element] = location; _document.ElementCount++;
        }
        return result!;
    }
    private void Own(UIElement owner, IDisposable resource)
    { if (owner == _document.Draft) _document.Resources.Add(resource); else owner.OwnResource(resource); }

    private Type ResolveType(string name, Context context, XObject location)
    {
        var candidates = context.Assemblies.SelectMany(assembly => context.Namespaces.Prepend("").Select(space => assembly.GetType(space.Length == 0 ? name : space + "." + name, false, false)))
            .OfType<Type>().Distinct().ToArray();
        if (candidates.Length != 1) Fail("MXML010", candidates.Length == 0 ? $"Unknown type '{name}'. Register its assembly/namespace." : $"Type '{name}' is ambiguous; use its full namespace.", location);
        return candidates[0];
    }
    private Type ResolveElementType(XElement xml, Context context)
    {
        string uri = xml.Name.NamespaceName;
        if (uri.Length == 0 || uri == "urn:melonui:1") return ResolveType(xml.Name.LocalName, context, xml);
        if (!uri.StartsWith("clr-namespace:", StringComparison.Ordinal)) Fail("MXML010", $"Unsupported XML namespace '{uri}'.", xml);
        var parts = uri[14..].Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length > 1 && parts[1].StartsWith("assembly=", StringComparison.Ordinal))
        {
            var assembly = Assembly.Load(new AssemblyName(parts[1][9..]));
            if (!context.Assemblies.Contains(assembly)) context.Assemblies.Add(assembly);
        }
        return ResolveType(parts[0] + "." + xml.Name.LocalName, context, xml);
    }

    private object ParseObject(XElement xml, Context context, UIElement owner, int depth)
    {
        var type = ResolveElementType(xml, context);
        var instance = Construct(type, xml);
        if (instance is IDisposable disposable && instance is not UIElement) Own(owner, disposable);
        var childContext = instance is MUIPage page ? context.Fork(page, context.File) : context;
        if (instance is MUIPage included)
        {
            var source = xml.Attribute("Source") ?? xml.Attribute("MXMLFilePath");
            if (source is not null)
            {
                if (depth >= _options.MaxIncludeDepth) Fail("MXML040", "The MXML include depth limit was reached.", source);
                string path = "";
                Guard(source, () => path = Path.GetFullPath(source.Value, context.File is { } file ? Path.GetDirectoryName(file)! : _options.BaseDirectory ?? Environment.CurrentDirectory));
                if (!_includes.Add(path)) Fail("MXML040", $"Circular MXML include: {path}", source);
                try
                {
                    string markup = ""; Guard(source, () => markup = File.ReadAllText(path));
                    childContext.File = path;
                    PopulatePage(included, ReadXml(markup, path), childContext, depth + 1);
                    included.MXMLFilePath = path;
                }
                finally { _includes.Remove(path); }
            }
            Declarations(xml, childContext);
        }
        Populate(instance, xml, childContext, instance as UIElement ?? owner, depth);
        if (instance is MUIPage scope)
        {
            scope.CompiledBackends = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new Dictionary<string, object>(childContext.Backends, StringComparer.Ordinal));
            _validate.Add(() => ValidateNames(scope));
        }
        return instance;
    }
    private void Populate(object instance, XElement xml, Context context, UIElement owner, int depth)
    {
        var attributes = new HashSet<string>(StringComparer.Ordinal);
        var propertyElements = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attribute in xml.Attributes())
        {
            if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace == XNamespace.Xml || attribute.Name.LocalName is "Grid.Row" or "Grid.Column") continue;
            if (instance is MUIPage && attribute.Name.LocalName is "Assemblies" or "Namespaces" or "Backends" or "Source" or "MXMLFilePath" or "MxmlVersion") continue;
            if (attribute.Name.LocalName == "MXMLFlags") Fail("MXML030", "Place MXMLFlags on a callback property element, such as Button.OnPressed.", attribute);
            attributes.Add(attribute.Name.LocalName);
            Guard(attribute, () => AssignAttribute(instance, attribute, context, owner));
        }
        var content = xml.Nodes().OfType<XText>().Select(t => t.Value).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
        if (content.Length > 0)
        {
            if (instance is TextBlock text && xml.Attribute(nameof(TextBlock.Text)) is null) text.Text = string.Concat(content);
            else Fail("MXML020", $"Text content is unsupported on {instance.GetType().Name}; use a property element.", xml);
        }
        foreach (var child in xml.Elements())
        {
            if (child.Name.LocalName.Contains('.')) PropertyElement(instance, child, context, owner, depth, attributes, propertyElements);
            else if (instance is UIElement parent)
            {
                var value = ParseObject(child, context, owner, depth);
                if (value is not UIElement element) Fail("MXML020", "Non-UI objects belong inside an explicit collection/property element.", child);
                AttachChild(parent, (UIElement)value, child);
            }
            else Fail("MXML020", $"Use a property element to add content to {instance.GetType().Name}.", child);
        }
    }
    private static void AttachChild(UIElement parent, UIElement child, XElement location)
    {
        parent.Children.Add(child);
        if (parent is GridContainer grid && (location.Attribute("Grid.Row") is not null || location.Attribute("Grid.Column") is not null))
            Guard(location, () => grid.SetCell(child, int.Parse(location.Attribute("Grid.Row")?.Value ?? "0", CultureInfo.InvariantCulture), int.Parse(location.Attribute("Grid.Column")?.Value ?? "0", CultureInfo.InvariantCulture)));
    }
    private PropertyInfo Property(object target, string name, XObject location) => target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
        ?? throw new MxmlException("MXML020", $"Unknown public property '{target.GetType().Name}.{name}'.", location);
    private object? ConvertValue(object? value, Type target)
    {
        if (_host.TypeExtensions.TryGetValue(target, out var convert)) return convert(value!);
        return MxmlValueConverter.Convert(value, target);
    }
    private void AssignLiteral(object target, PropertyInfo property, object? value, XObject location)
    {
        if (property.SetMethod is not { IsPublic: true }) Fail("MXML021", $"Property {property.Name} is read-only.", location);
        var converted = ConvertValue(value, property.PropertyType);
        // Setting a draft's generated property must never write through an earlier/factory binding.
        if (target is IBindingHost host) host.RemoveBinding(property.Name);
        _propertyBindings.Remove((target, property.Name));
        property.SetValue(target, converted);
        if (converted is UIElement child && target is UIElement parent && child.Parent is null) parent.Children.Add(child);
        if (ReferenceEquals(target, _document.Draft)) _document.Assignments[property] = converted;
    }
    private void PropertyElement(object target, XElement xml, Context context, UIElement owner, int depth, HashSet<string> attributes, HashSet<string> propertyElements)
    {
        var parts = xml.Name.LocalName.Split('.');
        if (parts.Length != 2) Fail("MXML020", "A property element must use Owner.Property syntax.", xml);
        if (!propertyElements.Add(parts[1])) Fail("MXML023", $"Property '{parts[1]}' is declared more than once.", xml);
        bool validOwner = false;
        for (var type = target.GetType(); type is not null; type = type.BaseType) validOwner |= type.Name == parts[0];
        if (!validOwner) Fail("MXML020", $"Property owner '{parts[0]}' does not match {target.GetType().Name}.", xml);
        if (target.GetType().GetEvent(parts[1], BindingFlags.Public | BindingFlags.Instance) is { } evt)
        { AssignEventActions(target, evt, xml, context, owner); return; }
        var property = Property(target, parts[1], xml);
        if (typeof(Delegate).IsAssignableFrom(property.PropertyType)) { AssignActions(target, property, xml, context, owner); return; }
        if (attributes.Contains(property.Name)) Fail("MXML023", $"Property '{property.Name}' has both an attribute and a property-element value.", xml);
        if (xml.Attribute("MXMLFlags") is not null) Fail("MXML030", "MXMLFlags requires a callback property.", xml);
        if (target is IBindingHost host) host.RemoveBinding(property.Name);
        _propertyBindings.Remove((target, property.Name));
        var elements = xml.Elements().ToArray();
        if (property.GetValue(target) is ElementCollection children)
        {
            foreach (var element in elements)
            {
                var value = ParseObject(element, context, owner, depth);
                if (value is not UIElement ui) Fail("MXML020", "Children accepts only UI elements.", element);
                AttachChild((UIElement)target, (UIElement)value, element);
            }
            return;
        }
        var collectionType = property.PropertyType.GetInterfaces().Append(property.PropertyType).FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
        if (collectionType is not null)
        {
            object? collection = property.GetValue(target);
            if (collection is null)
            {
                collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(collectionType.GenericTypeArguments));
                AssignLiteral(target, property, collection, xml);
            }
            var add = collectionType.GetMethod("Add")!;
            foreach (var element in elements)
            {
                var value = ParseObject(element, context, owner, depth);
                Guard(element, () => add.Invoke(collection, [ConvertValue(value, collectionType.GenericTypeArguments[0])]));
                if (value is UIElement child && target is UIElement parent && child.Parent is null) parent.Children.Add(child);
            }
            return;
        }
        if (property.PropertyType.IsGenericType && property.GetValue(target) is { } customCollection && property.PropertyType.GetMethod("Add") is { } customAdd)
        {
            foreach (var element in elements) { var value = ParseObject(element, context, owner, depth); Guard(element, () => customAdd.Invoke(customCollection, [value])); }
            return;
        }
        if (elements.Length == 0) Guard(xml, () => AssignValue(target, property.Name, xml.Value, xml, context, owner));
        else if (property.PropertyType.IsArray)
        {
            var values = elements.Select(element => ParseObject(element, context, owner, depth)).ToArray();
            Guard(xml, () => AssignLiteral(target, property, values, xml));
        }
        else if (elements.Length == 1)
        {
            var value = ParseObject(elements[0], context, owner, depth);
            Guard(xml, () => AssignLiteral(target, property, value, xml));
        }
        else Fail("MXML020", $"Property '{property.Name}' accepts one value, not a collection.", xml);
    }
    private static IEnumerable<UIElement> ScopeElements(MUIPage page)
    {
        foreach (var child in page.Children)
        {
            yield return child;
            if (child is MUIPage) continue;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
        static IEnumerable<UIElement> Descendants(UIElement parent)
        {
            foreach (var child in parent.Children) { yield return child; if (child is not MUIPage) foreach (var descendant in Descendants(child)) yield return descendant; }
        }
    }
    private void ValidateNames(MUIPage page)
    {
        var duplicate = ScopeElements(page).Where(e => !string.IsNullOrEmpty(e.Name)).GroupBy(e => e.Name, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new MxmlException("MXML031", $"Name '{duplicate.Key}' is duplicated within one page scope.", _locations.GetValueOrDefault(duplicate.Skip(1).First()));
    }
    private UIElement ResolveTarget(Context context, string path, XObject location)
    {
        UIElement target = context.Scope;
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part == ".") continue;
            if (target is not MUIPage scope) Fail("MXML031", $"'{target.Name}' is not a page scope; qualify targets with page names separated by '/'.", location);
            var matches = ScopeElements((MUIPage)target).Where(e => e.Name == part).ToArray();
            if (matches.Length != 1) Fail("MXML031", $"Action target '{path}' is missing or ambiguous in this page scope.", location);
            target = matches[0];
        }
        return target;
    }
    private UIElement Live(UIElement element) => element == _document.Draft ? _document.LiveRoot : element;
    private static void Guard(XObject location, Action action)
    {
        try { action(); }
        catch (MxmlException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        { throw new MxmlException("MXML020", ex.GetBaseException().Message, location); }
    }
    private static void Fail(string code, string message, XObject? location = null) => throw new MxmlException(code, message, location);
    public void Dispose()
    {
        if (!_document.Committed)
        {
            foreach (var binding in _document.Bindings.Values.Distinct()) binding.Dispose();
            foreach (var resource in _document.Resources.AsEnumerable().Reverse()) TryDispose(resource);
            foreach (var element in _constructed.Where(e => e.Parent is null)) TryDispose(element);
        }
        TryDispose(_document.Draft);
        _constructed.Clear(); _validate.Clear(); _propertyBindings.Clear(); _locations.Clear();
    }
    private void TryDispose(IDisposable resource)
    {
        try { resource.Dispose(); }
        catch (Exception ex) { _host.CompilerMessages.Add(new CompilerMessage($"Compilation cleanup failed: {ex.GetBaseException().Message}", MelonUI.Enums.MessageSeverity.Warning) { Code = "MXML090" }); }
    }
}

internal sealed class MxmlParseException : Exception
{
    internal string? SourcePath { get; }
    internal int Line { get; }
    internal int Column { get; }
    internal MxmlParseException(string message, string? source, int line, int column) : base(message) { SourcePath = source; Line = line; Column = column; }
}
