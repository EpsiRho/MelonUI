using System.Globalization;
using System.Reflection;
using System.Text.Json;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;
using MelonUI.Docs;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var root = Path.GetFullPath(args.Length > 0 ? args[0] : "..");
var temporary = Path.Combine(Path.GetTempPath(), "mui-doc-" + Guid.NewGuid().ToString("N")[..6]);
Directory.CreateDirectory(Path.Combine(temporary, "Pages"));
File.WriteAllText(Path.Combine(temporary, "Layout.mxml"), "<MUIPage/>");
var previews = new Dictionary<string, string>();
try
{
    foreach (var sample in Samples.All(temporary))
    {
        await using var window = new ConsoleWindowManager();
        var element = sample.Build();
        window.Elements.Add(element);
        previews[sample.Slug] = Trim(window.CaptureText(width: 64, height: 12));
        // Path captions are environment-specific. Replace only that rendered row in the published capture.
        if (sample.Slug == "filepicker")
        {
            var lines = previews[sample.Slug].Split('\n');
            lines[1] = "│/demo".PadRight(53) + "│";
            previews[sample.Slug] = string.Join('\n', lines);
        }
        using var page = new MUIPage();
        page.BackendObjects.Add("App", new SettingsModel());
        sample.Configure(page);
        if (!page.Compile(sample.Mxml)) throw new Exception($"{sample.Slug}: {page.GetSimpleCompilerDisplay()}");
        Console.WriteLine($"PASS: {sample.Slug} C# construction/render + MXML compile");
    }
    await using (var window = new ConsoleWindowManager())
    {
        var page = QuickStart.CreatePage();
        window.Elements.Add(page);
        previews["quickstart"] = Trim(window.CaptureText(width: 64, height: 12));
        page.Children.OfType<Button>().Single().PerformClick();
        if (!page.Children.OfType<TextBlock>().Single().Text.Contains("Hello, Melon listener!")) throw new Exception("Quick start callback failed.");
    }
    using (var page = new MUIPage())
    {
        var model = new SettingsModel();
        page.BackendObjects.Add("App", model);
        var source = Path.Combine(root, "examples", "Settings.mxml");
        if (!page.CompileFile(source)) throw new Exception(page.GetSimpleCompilerDisplay());
        page.Find<TextBox>("Name")!.Text = "Ada";
        if (model.Name != "Ada") throw new Exception("Two-way example failed.");
        page.Children.OfType<Button>().Single().PerformClick();
        if (!page.Find<TextBlock>("Saved")!.IsVisible || model.Status != "Saved Ada") throw new Exception("Callback/action example failed.");
        var old = page.Find<TextBox>("Name");
        if (page.Compile("<MUIPage><Unknown/></MUIPage>") || page.Find<TextBox>("Name") != old) throw new Exception("Failed reload example failed.");
        if (!page.CompileFile(source) || !old!.IsDisposed || page.Find<TextBox>("Name")!.Text != "Ada") throw new Exception("Successful reload example failed.");
    }
    using (var badge = new Badge())
    {
        var model = new SettingsModel();
        badge.SetBinding(nameof(Badge.Caption), Binding.CreatePath(model, nameof(SettingsModel.Name)));
        badge.Caption = "Bound badge";
        if (model.Name != "Bound badge") throw new Exception("Generated setter failed.");
        badge.TryHandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        if (!badge.Active) throw new Exception("Custom keyboard example failed.");
        badge.RemoveBinding(nameof(Badge.Caption));
        badge.Caption = "Ready";
        badge.CalculateLayout(0, 0, 64, 5);
        previews["badge"] = Trim(badge.Render().Screenshot(false));
    }
    using (var page = new MUIPage())
    {
        page.CompilerOptions.Assemblies.Add(typeof(Badge).Assembly);
        page.CompilerOptions.Namespaces.Add("MelonUI.Docs");
        if (!page.Compile("<MUIPage ShowBorder=\"false\"><Badge Caption=\"Custom control\" Active=\"true\"/></MUIPage>")) throw new Exception(page.GetSimpleCompilerDisplay());
    }
    Console.WriteLine("PASS: two-way model, callbacks/actions, transactional reload, generated Badge properties, input and MXML registration.");
    var json = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(Path.Combine(root, "content", "previews.json"), JsonSerializer.Serialize(previews, json));
    var types = typeof(UIElement).Assembly.GetExportedTypes().OrderBy(t => t.FullName).Select(t => new
    {
        name = t.Name.Split('`')[0], fullName = t.FullName, ns = t.Namespace,
        kind = t.IsEnum ? "enum" : t.IsInterface ? "interface" : t.IsValueType ? "struct" : "class",
        baseType = t.BaseType is { } b && b != typeof(object) ? TypeName(b) : null,
        isElement = typeof(UIElement).IsAssignableFrom(t), isAbstract = t.IsAbstract,
        signatures = Members(t),
        values = t.IsEnum ? Enum.GetNames(t) : Array.Empty<string>()
    }).ToArray();
    File.WriteAllText(Path.Combine(root, "content", "api.json"), JsonSerializer.Serialize(types, json));
    Console.WriteLine($"Exported {types.Length} public core types and {previews.Count} renderer captures.");
}
finally { Directory.Delete(temporary, recursive: true); }

static string Trim(string text) => string.Join('\n', text.Replace("\r", "").Split('\n').Select(s => s.TrimEnd())).TrimEnd('\n');
static string TypeName(Type t)
{
    if (t.IsByRef) return "ref " + TypeName(t.GetElementType()!);
    if (t.IsArray) return TypeName(t.GetElementType()!) + "[]";
    var aliases = new Dictionary<Type, string> { [typeof(void)] = "void", [typeof(string)] = "string", [typeof(bool)] = "bool", [typeof(int)] = "int", [typeof(float)] = "float", [typeof(double)] = "double", [typeof(object)] = "object", [typeof(long)] = "long", [typeof(char)] = "char", [typeof(byte)] = "byte" };
    if (aliases.TryGetValue(t, out var alias)) return alias;
    if (!t.IsGenericType) return t.Name;
    return t.GetGenericTypeDefinition() == typeof(Nullable<>) ? TypeName(t.GetGenericArguments()[0]) + "?" : t.Name.Split('`')[0] + "<" + string.Join(", ", t.GetGenericArguments().Select(TypeName)) + ">";
}
static string Parameters(MethodBase method) => string.Join(", ", method.GetParameters().Select(p =>
    (p.GetCustomAttribute<ParamArrayAttribute>() is not null ? "params " : "") + TypeName(p.ParameterType) + " " + p.Name +
    (p.HasDefaultValue ? " = " + (p.DefaultValue is null ? "null" : p.DefaultValue is string s ? JsonSerializer.Serialize(s) : p.DefaultValue is bool flag ? flag.ToString().ToLowerInvariant() : Convert.ToString(p.DefaultValue, CultureInfo.InvariantCulture)) : "")));
static string[] Members(Type t)
{
    const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    return t.GetConstructors(flags).Select(c => t.Name.Split('`')[0] + "(" + Parameters(c) + ")")
        .Concat(t.GetProperties(flags).Select(p => TypeName(p.PropertyType) + " " + p.Name + (p.GetIndexParameters().Length > 0 ? "[" + string.Join(", ", p.GetIndexParameters().Select(q => TypeName(q.ParameterType))) + "]" : "") + " { " + (p.GetMethod?.IsPublic == true ? "get; " : "") + (p.SetMethod?.IsPublic == true ? "set; " : "") + "}"))
        .Concat(t.GetEvents(flags).Select(e => "event " + TypeName(e.EventHandlerType!) + " " + e.Name))
        .Concat(t.GetFields(flags).Where(f => !t.IsEnum).Select(f => TypeName(f.FieldType) + " " + f.Name))
        .Concat(t.GetMethods(flags).Where(m => !m.IsSpecialName).Select(m => TypeName(m.ReturnType) + " " + m.Name + (m.IsGenericMethod ? "<" + string.Join(", ", m.GetGenericArguments().Select(TypeName)) + ">" : "") + "(" + Parameters(m) + ")"))
        .OrderBy(s => s).ToArray();
}
