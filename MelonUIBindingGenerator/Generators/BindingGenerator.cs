using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MelonUIBindingGenerator.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class BindingGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor Target = Rule("MUIB001", "Unsupported binding target", "[Binding] supports mutable instance fields, not {0}");
    private static readonly DiagnosticDescriptor Partial = Rule("MUIB002", "Partial class required", "Type '{0}' and every containing type must be a nonstatic partial class");
    private static readonly DiagnosticDescriptor Mutable = Rule("MUIB003", "Mutable field required", "Binding field '{0}' must be a mutable instance field");
    private static readonly DiagnosticDescriptor Collision = Rule("MUIB004", "Generated property collision", "Field '{0}' generates property '{1}', which already exists or is generated more than once");
    private static readonly DiagnosticDescriptor Reserved = Rule("MUIB005", "Binding helper collision", "Type '{0}' declares reserved binding helpers; derive from UIElement or remove the conflicting helpers");
    private static DiagnosticDescriptor Rule(string id, string title, string message) => new(id, title, message, "MelonUI", DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName("MelonUI.Attributes.BindingAttribute",
            static (_, _) => true, static (ctx, _) => ctx.TargetSymbol).Collect();
        context.RegisterSourceOutput(candidates, static (output, symbols) => Generate(output, symbols));
    }

    private static bool IsUiElement(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.BaseType)
            if (current.ToDisplayString() == "MelonUI.Base.UIElement") return true;
        return false;
    }
    private static bool HasGeneratedFields(INamedTypeSymbol type) => type.GetMembers().OfType<IFieldSymbol>()
        .Any(field => field.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "MelonUI.Attributes.BindingAttribute"));
    private static bool UsesInheritedBindings(INamedTypeSymbol type)
    {
        if (IsUiElement(type)) return true;
        for (var parent = type.BaseType; parent is not null; parent = parent.BaseType)
            if (HasGeneratedFields(parent) || (parent.GetMembers("GetBoundValue").Length > 0 && parent.GetMembers("SetBoundValue").Length > 0)) return true;
        return false;
    }
    private static bool HasInheritedMember(INamedTypeSymbol type, string name)
    {
        for (var parent = type.BaseType; parent is not null; parent = parent.BaseType)
            if (parent.GetMembers(name).Length > 0 || parent.GetMembers().OfType<IFieldSymbol>().Any(f => f.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "MelonUI.Attributes.BindingAttribute") && PropertyName(f) == name)) return true;
        return false;
    }
    private static string PropertyName(IFieldSymbol field)
    {
        var name = field.Name.TrimStart('_');
        return name.Length == 0 ? "" : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
    private static string Id(string text) => SyntaxFacts.GetKeywordKind(text) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(text) != SyntaxKind.None ? "@" + text : text;

    private static void Generate(SourceProductionContext output, ImmutableArray<ISymbol> symbols)
    {
        var fields = new List<IFieldSymbol>();
        foreach (var symbol in symbols.Distinct(SymbolEqualityComparer.Default))
        {
            if (symbol is not IFieldSymbol field) { output.ReportDiagnostic(Diagnostic.Create(Target, symbol.Locations.FirstOrDefault(), symbol.Kind.ToString())); continue; }
            if (field.IsStatic || field.IsReadOnly || field.IsConst) { output.ReportDiagnostic(Diagnostic.Create(Mutable, field.Locations.FirstOrDefault(), field.Name)); continue; }
            fields.Add(field);
        }
        foreach (var group in fields.GroupBy(f => f.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
        {
            var type = group.First().ContainingType;
            var chain = new Stack<INamedTypeSymbol>();
            bool valid = true;
            for (var current = type; current != null; current = current.ContainingType)
            {
                chain.Push(current);
                if (current.TypeKind != TypeKind.Class || current.IsStatic || current.IsRecord || current.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is not ClassDeclarationSyntax declaration || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword)))
                { output.ReportDiagnostic(Diagnostic.Create(Partial, current.Locations.FirstOrDefault(), current.Name)); valid = false; }
            }
            if (!valid) continue;
            bool ui = UsesInheritedBindings(type);
            if (!ui && new[] { "SetBinding", "RemoveBinding", "GetBoundValue", "SetBoundValue", "__melonBindings" }.Any(name => type.GetMembers(name).Length > 0))
            { output.ReportDiagnostic(Diagnostic.Create(Reserved, type.Locations.FirstOrDefault(), type.Name)); continue; }
            var source = new StringBuilder("// <auto-generated/>\n#nullable enable\n");
            bool hasNamespace = !type.ContainingNamespace.IsGlobalNamespace;
            if (hasNamespace) source.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).Append(" {\n");
            foreach (var current in chain)
            {
                source.Append("partial class ").Append(Id(current.Name));
                if (current.TypeParameters.Length > 0) source.Append('<').Append(string.Join(", ", current.TypeParameters.Select(p => Id(p.Name)))).Append('>');
                if (SymbolEqualityComparer.Default.Equals(current, type) && !ui) source.Append(" : global::MelonUI.Base.IBindingHost");
                source.Append(" {\n");
            }
            if (!ui) source.Append(@"
private readonly global::MelonUI.Base.BindingStore __melonBindings = new();
public void SetBinding(string name, global::MelonUI.Base.Binding binding) => __melonBindings.SetBinding(name, binding);
public bool RemoveBinding(string name) => __melonBindings.RemoveBinding(name);
protected object? GetBoundValue(string name, object? local) => __melonBindings.GetValue(name, local);
protected void SetBoundValue<T>(string name, T value, ref T local) => __melonBindings.SetValue(name, value, ref local);
");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in group.OrderBy(f => f.Name, StringComparer.Ordinal))
            {
                string name = PropertyName(field);
                if (name.Length == 0 || type.GetMembers(name).Length > 0 || HasInheritedMember(type, name) || !names.Add(name))
                { output.ReportDiagnostic(Diagnostic.Create(Collision, field.Locations.FirstOrDefault(), field.Name, name)); continue; }
                var format = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
                string fieldType = field.Type.ToDisplayString(format);
                source.Append("public ").Append(fieldType).Append(' ').Append(Id(name)).Append(" { get => (").Append(fieldType).Append(")GetBoundValue(nameof(")
                    .Append(Id(name)).Append("), ").Append(Id(field.Name)).Append(")!; set => SetBoundValue(nameof(").Append(Id(name)).Append("), value, ref ").Append(Id(field.Name)).Append("); }\n");
            }
            foreach (var _ in chain) source.Append("}\n");
            if (hasNamespace) source.Append("}\n");
            ulong hash = 14695981039346656037;
            foreach (char character in group.Key) hash = unchecked((hash ^ character) * 1099511628211);
            output.AddSource(type.Name + "." + hash.ToString("X16") + ".g.cs", source.ToString());
        }
    }
}
