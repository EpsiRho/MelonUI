using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MelonUIBindingGenerator.Generators;

[Generator]
public class BindingGenerator : IIncrementalGenerator
{
    private static bool IsBindingField(SyntaxNode node) =>
        node is FieldDeclarationSyntax field &&
        field.AttributeLists.Any(al => al.Attributes
            .Any(a => a.Name.ToString() == "Binding"))
        ||
        node is PropertyDeclarationSyntax prop &&
        prop.AttributeLists.Any(al => al.Attributes
            .Any(a => a.Name.ToString() == "Binding"))
        ||
        node is EventDeclarationSyntax evnt &&
        evnt.AttributeLists.Any(al => al.Attributes
            .Any(a => a.Name.ToString() == "Binding"));

    private static FieldInfo? GetBindingField(GeneratorSyntaxContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
        var model = context.SemanticModel;

        // Get field symbol
        var fieldSymbol = model.GetDeclaredSymbol(fieldDeclaration.Declaration.Variables[0]);
        if (fieldSymbol == null) return null;

        var containingType = fieldSymbol.ContainingType;
        var root = fieldDeclaration.SyntaxTree.GetCompilationUnitRoot();
        var usings = root.Usings
           .Select(u => u.ToFullString())
           .ToList();

        var fi = new FieldInfo(
            containingType.ContainingNamespace.ToDisplayString(),
            containingType.Name,
            fieldDeclaration.Declaration.Variables[0].Identifier.Text,
            fieldDeclaration.Declaration.Type.ToString(),
            fieldDeclaration.Modifiers.ToString(),
            usings,
            containingType,
            IsDerivedFromUIElement(containingType));

        return fi;
    }

    private static bool HasMethodWithName(INamedTypeSymbol containingType, string methodName)
    {
        // Look for a method in the members of the type with the specified name

        return containingType.GetMembers().OfType<IMethodSymbol>()
            .Any(m => m.Name == methodName);
    }

    private static bool IsDerivedFromUIElement(INamedTypeSymbol containingType)
    {
        var baseType = containingType.BaseType;
        //File.WriteAllText($"C:/Users/jhset/Desktop/{containingType.Name}.txt", $"{baseType.ToDisplayString()}:");
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == "MelonUI.Base.UIElement")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var bindingFields = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsBindingField(s),
                transform: static (ctx, _) => GetBindingField(ctx))
            .Where(static m => m is not null);

        // Group by containing class
        var groupedFields = bindingFields
            .Collect() // Collect all fields in a single context
            .Select(static (fields, _) =>
                fields
                    .GroupBy(f => f!.ContainingType, f => f!)
                    .ToImmutableArray());

        context.RegisterSourceOutput(groupedFields,
            static (spc, grouped) =>
            {
                foreach (var group in grouped)
                {
                    ExecuteClass(group.Key, group.ToImmutableArray(), spc);
                }
            });
    }

    private static void ExecuteClass(INamedTypeSymbol containingType, ImmutableArray<FieldInfo> fields, SourceProductionContext context)
    {
        var className = containingType.Name;
        var namespaceName = containingType.ContainingNamespace.ToDisplayString();
        var usingsText = string.Join("\n", fields.First().Usings);

        bool hasGetFunction = HasMethodWithName(containingType, "GetBoundValue");
        bool hasSetFunction = HasMethodWithName(containingType, "SetBoundValue");

        string boundFunctions = string.Empty;
        if (!hasGetFunction && !fields.First().DerivesFromUIElement)
        {
            boundFunctions += $@"
/// <summary>
/// Gets the bound value or the local value.
/// </summary>
protected object GetBoundValue(string propertyName, object localValue)
{{
    try
    {{
        if (_bindings.TryGetValue(propertyName, out var binding))
        {{
            if (binding.IsProperty)
                return binding.GetValue();
        }}
        return localValue;
    }}
    catch (Exception)
    {{
        return localValue;
    }}
}}";
        }

        if (!hasSetFunction && !fields.First().DerivesFromUIElement)
        {
            boundFunctions += $@"
/// <summary>
/// Sets the bound value or the local value.
/// </summary>
protected void SetBoundValue<T>(string propertyName, T value, ref T localStorage)
{{
    if (_bindings.TryGetValue(propertyName, out var binding))
    {{
        if (binding.IsProperty)
        {{
            binding.SetValue(value);
            return;
        }}
        // Event bindings are handled separately
    }}

    // Not bound, set locally
    localStorage = value;
}}";
        }

        var dic = boundFunctions.Length != 0 ? "protected Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();" : " ";
        var properties = fields.Select(field =>
        {
            string propName = field.PropertyName.StartsWith("_")
                ? field.PropertyName.Substring(1)
                : field.PropertyName;
            string propPublic = $"{char.ToUpper(propName[0])}{propName.Substring(1)}";

            if (field.PropertyType == "string")
            {
                return $@"
public {field.PropertyType} {propPublic}
{{
    get
    {{
        var val = GetBoundValue(nameof({propPublic}), $""{{{field.PropertyName}}}"");
        string stred = $""{{val}}"";
        return stred;
    }}
    set => SetBoundValue(nameof({propPublic}), value, ref {field.PropertyName});
}}";
            }
            else
            {
                return $@"
public {field.PropertyType} {propPublic}
{{
    get => ({field.PropertyType})GetBoundValue(nameof({propPublic}), {field.PropertyName});
    set => SetBoundValue(nameof({propPublic}), value, ref {field.PropertyName});
}}";
            }
        });

        string source = $@"
// Auto-generated code
{usingsText}

namespace {namespaceName}
{{
    partial class {className}
    {{
        {dic}
        {boundFunctions}

        {string.Join("\n", properties)}
    }}
}}";

        context.AddSource($"{className}.g.cs", source);
    }

}

internal class FieldInfo
{
    public string Namespace;
    public string ClassName;
    public string PropertyName;
    public string PropertyType;
    public string Modifiers;
    public List<string> Usings;
    public INamedTypeSymbol ContainingType;
    public bool DerivesFromUIElement;

    public FieldInfo(string ns, string cn, string pn, string pt, string md, List<string> u, INamedTypeSymbol container, bool derives)
    {
        Namespace = ns;
        ClassName = cn;
        PropertyName = pn;
        PropertyType = pt;
        Modifiers = md;
        Usings = u;
        ContainingType = container;
        DerivesFromUIElement = derives;
    }

    public override string ToString() => $"{Namespace}.{ClassName}.{PropertyName} : {PropertyType}";
}