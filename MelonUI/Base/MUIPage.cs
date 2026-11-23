using System.Diagnostics;
using System.Reflection;
using MelonUI.Attributes;
using MelonUI.Enums;
using MelonUI.Managers;
using MelonUI.Markup;

namespace MelonUI.Base;

/// <summary>A reusable C# page or transactionally compiled MXML document.</summary>
public partial class MUIPage : UIElement
{
    [Binding] private string mXMLFilePath = "";
    public string MXML { get; private set; } = "";
    public bool Failed { get; private set; }
    public List<CompilerMessage> CompilerMessages { get; } = new();
    public CompilerMessage CompilerFocusedMessage { get; private set; } = new("No compilation has run.", MessageSeverity.Info);
    public TimeSpan CompilationFinished { get; private set; }
    /// <summary>Externally supplied backends are borrowed and never disposed by the compiler.</summary>
    public Dictionary<string, object> BackendObjects { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, object> CompiledBackends { get; internal set; } = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
    public T? GetBackend<T>(string alias) where T : class
    {
        VerifyAccess();
        if (!CompiledBackends.TryGetValue(alias, out var backend) && !BackendObjects.TryGetValue(alias, out backend)) return null;
        return backend as T ?? throw new InvalidOperationException($"Backend '{alias}' is {backend.GetType().Name}, not {typeof(T).Name}.");
    }
    public Dictionary<Type, Func<object, object>> TypeExtensions { get; } = new();
    public MxmlCompilerOptions CompilerOptions { get; } = new();
    private readonly Dictionary<string, ConsoleBuffer> _bufferCache = new();
    private List<IDisposable> _compiledResources = new();
    private HashSet<string> _compiledBindingKeys = new(StringComparer.Ordinal);
    public MUIPage() => ConsiderForFocus = false;

    public bool CompileFile(string filePath)
    {
        VerifyAccess();
        try
        {
            var fullPath = Path.GetFullPath(filePath, CompilerOptions.BaseDirectory ?? Environment.CurrentDirectory);
            return CompileCore(File.ReadAllText(fullPath), fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { BeginDiagnostics(); AddError("MXML001", $"Cannot read MXML file: {ex.Message}", filePath); return false; }
    }
    public bool Compile(string markup, string? sourcePath = null)
    {
        VerifyAccess();
        try { return CompileCore(markup, sourcePath is null ? null : Path.GetFullPath(sourcePath, CompilerOptions.BaseDirectory ?? Environment.CurrentDirectory)); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        { BeginDiagnostics(); AddError("MXML001", ex.Message, sourcePath); return false; }
    }
    private void BeginDiagnostics() { CompilerMessages.Clear(); Failed = false; CompilationFinished = TimeSpan.Zero; }
    internal void AddError(string code, string message, string? source = null, int line = -1, int column = -1)
    {
        Failed = true;
        CompilerFocusedMessage = new CompilerMessage(message, MessageSeverity.Error, (line, column)) { Code = code, SourcePath = source };
        CompilerMessages.Add(CompilerFocusedMessage);
    }
    private bool CompileCore(string markup, string? source)
    {
        BeginDiagnostics(); var clock = Stopwatch.StartNew();
        using var compiler = new MxmlCompiler(this);
        try
        {
            var document = compiler.Build(markup, source);
            Commit(document);
            MXML = markup; MXMLFilePath = source ?? "";
            CompilerFocusedMessage = new CompilerMessage($"Compiled {document.ElementCount} elements.", MessageSeverity.Success) { Code = "MXML000", SourcePath = source };
            CompilerMessages.Add(CompilerFocusedMessage);
            return true;
        }
        catch (MxmlParseException ex) { AddError("MXML003", ex.Message, ex.SourcePath ?? source, ex.Line, ex.Column); return false; }
        catch (MxmlException ex) { AddError(ex.Code, ex.Message, ex.SourcePath ?? source, ex.Line, ex.Column); return false; }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        { AddError("MXML099", ex.GetBaseException().Message, source); return false; }
        finally { CompilationFinished = clock.Elapsed; }
    }

    private void Commit(MxmlDocument document)
    {
        var oldChildren = Children.ToArray();
        var newChildren = document.Draft.Children.ToArray();
        var oldResources = _compiledResources;
        var oldKeys = _compiledBindingKeys;
        var oldValues = document.Assignments.Keys.ToDictionary(p => p, p => p.GetValue(this));
        var savedBindings = new Dictionary<string, Binding>(StringComparer.Ordinal);
        var oldFocus = ParentWindow?.FocusedElement;
        string? focusName = oldFocus?.IsWithin(this) == true ? oldFocus.Name : null;
        foreach (var key in oldKeys.Concat(document.Bindings.Keys).Concat(document.Assignments.Keys.Select(p => p.Name)).Distinct())
            if (DetachBinding(key) is { } binding) savedBindings.Add(key, binding);
        document.Draft.Children.Clear();
        try
        {
            foreach (var assignment in document.Assignments) assignment.Key.SetValue(this, assignment.Value);
            Children.Clear(); foreach (var child in newChildren) Children.Add(child);
            document.LiveRoot = this;
            foreach (var binding in document.Bindings) SetBinding(binding.Key, binding.Value);
        }
        catch
        {
            foreach (var key in document.Bindings.Keys) DetachBinding(key);
            Children.Clear(); foreach (var child in oldChildren) Children.Add(child);
            foreach (var assignment in oldValues) assignment.Key.SetValue(this, assignment.Value);
            foreach (var binding in savedBindings) SetBinding(binding.Key, binding.Value);
            foreach (var child in newChildren) document.Draft.Children.Add(child);
            throw;
        }
        _compiledResources = document.TakeResources(); _compiledBindingKeys = document.Bindings.Keys.ToHashSet(StringComparer.Ordinal);
        CompiledBackends = document.Draft.CompiledBackends;
        document.Committed = true;
        foreach (var child in oldChildren) Cleanup(child);
        foreach (var binding in savedBindings.Values.Distinct()) Cleanup(binding);
        foreach (var resource in oldResources.AsEnumerable().Reverse()) Cleanup(resource);
        if (focusName is not null && ParentWindow is { } window)
        {
            var matches = ElementTree.Descendants(Children).Where(e => e.Name == focusName && e.CanFocus).ToArray();
            if (matches.Length == 1) window.Focus(matches[0]);
        }
        Invalidate();
    }
    private void Cleanup(IDisposable resource)
    {
        try { resource.Dispose(); }
        catch (Exception ex) { CompilerMessages.Add(new CompilerMessage($"Old document cleanup failed: {ex.GetBaseException().Message}", MessageSeverity.Warning) { Code = "MXML090" }); }
    }
    public string GetSimpleCompilerDisplay(int maxWidth = 100)
    {
        maxWidth = Math.Max(1, maxWidth);
        return string.Join(Environment.NewLine, CompilerMessages.SelectMany(message =>
            $"{message.Severity} {message.Code} {message.SourcePath ?? "MXML"}:{message.MxmlLineNumber.Line}:{message.MxmlLineNumber.Position}: {message.Message}".Split('\n')
                .Select(line => ControlDrawing.Clip(line.TrimEnd('\r'), maxWidth))));
    }
    protected override void OnChildRemoved(UIElement child) { _bufferCache.Remove(child.UID); base.OnChildRemoved(child); }
    protected override void OnDetachedFromWindow(ConsoleWindowManager window) { _bufferCache.Clear(); base.OnDetachedFromWindow(window); }
    protected override void DisposeCore()
    {
        _bufferCache.Clear();
        foreach (var resource in _compiledResources.AsEnumerable().Reverse()) Cleanup(resource);
        _compiledResources.Clear(); base.DisposeCore();
        CompiledBackends = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        foreach (var element in Children.Where(x => x.IsVisible).OrderBy(x => x.Z).ToArray())
        {
            if (element.IsDisposed || element.Parent != this || !element.IsVisible || element.ActualWidth == 0 || element.ActualHeight == 0) continue;
            if (element.RequiresContinuousRendering || element.NeedsRecalculation || !_bufferCache.TryGetValue(element.UID, out var childBuffer))
            {
                childBuffer = element.Render();
                if (element.EnableCaching && element.Parent == this) _bufferCache[element.UID] = childBuffer;
            }
            buffer.WriteBuffer(element.ActualX, element.ActualY, childBuffer, element.RespectBackgroundOnDraw);
        }
    }
}
