using System.Reflection;
using System.Runtime.Loader;
using MelonUI.Base;
using MelonUIBindingGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class GeneratorTests
{
    internal static IEnumerable<(string Name, Func<Task> Run)> All =>
    [
        ("binding generator supports aliases, qualified attributes, multiple fields and duplicate type names", NamesAndFields),
        ("binding generator handles global/nested/generic classes without consumer usings", NestedTypes),
        ("binding generator reports invalid targets and collisions without crashing", Diagnostics)
    ];
    private static void Check(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
    private static (Compilation Compilation, GeneratorDriverRunResult Result) Generate(string source)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Append(typeof(UIElement).Assembly.Location).Distinct(StringComparer.OrdinalIgnoreCase);
        var references = paths.Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("GeneratorConsumer" + Guid.NewGuid().ToString("N"), [CSharpSyntaxTree.ParseText(source, options)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new BindingGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var diagnostics);
        Check(!diagnostics.Any(d => d.Id == "CS8785"), string.Join(Environment.NewLine, diagnostics));
        return (generated, driver.GetRunResult());
    }
    private static Assembly Load(Compilation compilation)
    {
        using var image = new MemoryStream(); var result = compilation.Emit(image);
        Check(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        image.Position = 0; return AssemblyLoadContext.Default.LoadFromStream(image);
    }
    private static Task NamesAndFields()
    {
        var result = Generate("""
            using Bound = MelonUI.Attributes.BindingAttribute;
            namespace First { public partial class Same { [Bound] private int _one = 1, _two = 2; } }
            namespace Second { public partial class Same { [global::MelonUI.Attributes.BindingAttribute] private string _text = "literal {braces}"; } }
            """);
        Check(result.Result.GeneratedTrees.Length == 2);
        var assembly = Load(result.Compilation); var first = assembly.GetType("First.Same")!; var second = assembly.GetType("Second.Same")!;
        Check((int)first.GetProperty("Two")!.GetValue(Activator.CreateInstance(first))! == 2);
        Check((string)second.GetProperty("Text")!.GetValue(Activator.CreateInstance(second))! == "literal {braces}");
        return Task.CompletedTask;
    }
    private static Task NestedTypes()
    {
        var result = Generate("""
            public partial class Global { [MelonUI.Attributes.Binding] private int _value = 3; }
            public partial class Parent { [MelonUI.Attributes.Binding] private int _first = 1; }
            public partial class Child : Parent { [MelonUI.Attributes.Binding] private int _second = 2; }
            namespace Deep {
                public partial class Outer<T> where T : class {
                    public partial class Inner<U> where U : struct {
                        [MelonUI.Attributes.Binding] private global::System.Collections.Generic.Dictionary<T,U> _map = new();
                    }
                }
                public abstract partial class Widget : MelonUI.Base.UIElement {
                    [MelonUI.Attributes.Binding] private global::System.String? _optional;
                }
            }
            """);
        Load(result.Compilation); return Task.CompletedTask;
    }
    private static Task Diagnostics()
    {
        var result = Generate("""
            using MelonUI.Attributes;
            public partial class Invalid {
                [Binding] public string Value { get; set; } = "";
                [Binding] public event System.Action? Tick;
                [Binding] private readonly int _fixed = 1;
                [Binding] private static int _shared;
                [Binding] private int _value;
            }
            public class NotPartial { [Binding] private int _count; }
            """);
        var ids = result.Result.Diagnostics.Select(d => d.Id).ToHashSet();
        foreach (string id in new[] { "MUIB001", "MUIB002", "MUIB003", "MUIB004" }) Check(ids.Contains(id), $"Missing diagnostic {id}.");
        Check(result.Result.Results.All(r => r.Exception is null)); return Task.CompletedTask;
    }
}
