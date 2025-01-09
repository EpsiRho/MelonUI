using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MelonUIBindingGenerator.Generators;

//#pragma warning disable RS1035 // "Do not use banned APIs for analyzers" But I want to.

[Generator]
public class BindingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all fields with [Binding] attribute
        var bindingFields = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsBindingField(s),
                transform: static (ctx, _) => GetBindingField(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(bindingFields,
            static (spc, field) => Execute(field!, spc));
    }

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

        //var bindingAttribute = fieldSymbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "BindingAttribute");
        //File.WriteAllText($"C:/Users/jhset/Desktop/piss.cs", $"{string.Join("\n", fieldSymbol.GetAttributes().FirstOrDefault().AttributeClass.Name)}");
        //var typeArg = bindingAttribute.ConstructorArguments[0].Value;
        //File.WriteAllText($"C:/Users/jhset/Desktop/ass.cs", $"{typeArg}");
        var root = fieldDeclaration.SyntaxTree.GetCompilationUnitRoot();
        var usings = root.Usings
           .Select(u => u.ToFullString())
           .ToList();

        // Extract field information
        var containingType = fieldSymbol.ContainingType;
        var fieldName = fieldDeclaration.Declaration.Variables[0].Identifier.Text;


        var fi = new FieldInfo(
            containingType.ContainingNamespace.ToDisplayString(),
            containingType.Name,
            fieldName,
            fieldDeclaration.Declaration.Type.ToString(),
            fieldDeclaration.Modifiers.ToString(),
            usings);

        return fi;
    }

    private static void Execute(FieldInfo field, SourceProductionContext context)
    {
        // Generate backing field and property
        string propName = field.PropertyName;
        if (propName.StartsWith("_"))
        {
            propName = propName.Substring(1);
        }
        string propPublic = $"{propName[0].ToString().ToUpper()}{propName.Substring(1)}";
        var usingsText = string.Join("\n", field.Usings);
        string source = "";
        if(field.PropertyType == "string")
        {
            source = $@"
// Piss
{usingsText}

namespace {field.Namespace}
{{

    partial class {field.ClassName}
    {{
        // Auto-property
        public {field.PropertyType} {propPublic}
        {{
            get
            {{
                var val = GetBoundValue(nameof({propPublic}), $""{{{field.PropertyName}}}"");
                string stred = $""{{val}}"";
                return stred;
            }}
            set => SetBoundValue(nameof({propPublic}), value, ref {field.PropertyName});
        }}
    }}
}}";
        }
        else
        {
            source = $@"
// Piss
{usingsText}

namespace {field.Namespace}
{{
    partial class {field.ClassName}
    {{
        // Auto-property
        public {field.PropertyType} {propPublic}
        {{
            get => ({field.PropertyType})GetBoundValue(nameof({propPublic}), {field.PropertyName});
            set => SetBoundValue(nameof({propPublic}), value, ref {field.PropertyName});
        }}
    }}
}}";
        }
        

        context.AddSource(
            $"{field.ClassName}.{field.PropertyName}.g.cs",
            source);
        //File.WriteAllText($"C:/Users/jhset/Desktop/GeneratorTesting/{field.ClassName}.{field.PropertyName}.g.cs", source);
    }
}


internal class FieldInfo {
    public string Namespace;
    public string ClassName;
    public string PropertyName;
    public string PropertyType;
    public string Modifiers;
    public List<string> Usings;
    public FieldInfo(string ns, string cn, string pn, string pt, string md, List<string> u)
    {
        Namespace = ns;
        ClassName = cn;
        PropertyName = pn;
        PropertyType = pt;
        Modifiers = md;
        Usings = u;
    }
    public override string ToString() => $"{Namespace}.{ClassName}.{PropertyName} : {PropertyType}";
}