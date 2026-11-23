using System.Reflection;

namespace MelonUI.Markup;

/// <summary>Host-supplied type/factory registrations and limits for trusted MXML.</summary>
public sealed class MxmlCompilerOptions
{
    public const int CurrentLanguageVersion = 1;
    public IList<Assembly> Assemblies { get; } = new List<Assembly>();
    public IList<string> Namespaces { get; } = new List<string>();
    public IDictionary<Type, Func<object>> Factories { get; } = new Dictionary<Type, Func<object>>();
    public string? BaseDirectory { get; set; }
    public int MaxIncludeDepth { get; set; } = 32;
    public long MaxDocumentCharacters { get; set; } = 4 * 1024 * 1024;
}
