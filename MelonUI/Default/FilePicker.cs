using System.Drawing;
using MelonUI.Attributes;
using MelonUI.Base;

namespace MelonUI.Default;

public enum FilePickerMode { File, Directory, FileOrDirectory }

public partial class FilePicker : UIElement
{
    private List<FileSystemInfo> _entries = new();
    private int _currentIndex;
    private int _scrollOffset;
    [Binding] private Action? onDirectorySelected;
    [Binding] private Action? onFileSelected;
    [Binding] private string path = "";
    public string CurrentDirectory { get; private set; } = Environment.CurrentDirectory;
    public string? ErrorMessage { get; private set; }
    public FilePickerMode SelectionMode { get; set; } = FilePickerMode.File;
    public IReadOnlyList<FileSystemInfo> Entries => _entries.AsReadOnly();
    public FileSystemInfo? SelectedEntry => _entries.Count == 0 ? null : _entries[Math.Clamp(_currentIndex, 0, _entries.Count - 1)];
    public event Action<string>? SelectionConfirmed;
    public event Action? Cancelled;
    /// <summary>File extensions including their dot; empty accepts all files. Directories remain navigable.</summary>
    public HashSet<string> Extensions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public FilePicker() : this(null) { }
    public FilePicker(string? dir) { EnableCaching = false; NavigateTo(dir ?? Environment.CurrentDirectory); }
    public void Show() => IsVisible = true;
    public bool NavigateTo(string directory)
    {
        VerifyAccess();
        try
        {
            string fullPath = System.IO.Path.GetFullPath(directory);
            var entries = new DirectoryInfo(fullPath).GetFileSystemInfos()
                .Where(entry => entry is DirectoryInfo || Extensions.Count == 0 || Extensions.Contains(entry.Extension))
                .OrderBy(entry => entry is DirectoryInfo ? 0 : 1).ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToList();
            CurrentDirectory = fullPath; _entries = entries; _currentIndex = _scrollOffset = 0; ErrorMessage = null;
            Invalidate(); return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException)
        { ErrorMessage = ex.Message; Invalidate(); return false; }
    }
    public bool Refresh() => NavigateTo(CurrentDirectory);
    public bool NavigateToParent()
    {
        VerifyAccess();
        try { return Directory.GetParent(CurrentDirectory) is { } parent && NavigateTo(parent.FullName); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { ErrorMessage = ex.Message; Invalidate(); return false; }
    }
    public bool SelectCurrentDirectory()
    {
        VerifyAccess();
        if (SelectionMode == FilePickerMode.File || !Directory.Exists(CurrentDirectory)) return false;
        Confirm(CurrentDirectory, directory: true); return true;
    }
    private void Confirm(string selectedPath, bool directory)
    {
        Path = selectedPath;
        if (directory) OnDirectorySelected?.Invoke(); else OnFileSelected?.Invoke();
        SelectionConfirmed?.Invoke(selectedPath);
    }
    public override bool TryHandleKey(ConsoleKeyInfo key)
    {
        if (LockControls || !IsEffectivelyVisible || !IsEffectivelyEnabled) return false;
        if (base.TryHandleKey(key)) return true;
        if ((key.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) return false;
        int count = _entries.Count;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow: _currentIndex = Math.Max(0, _currentIndex - 1); break;
            case ConsoleKey.DownArrow: _currentIndex = Math.Min(Math.Max(0, count - 1), _currentIndex + 1); break;
            case ConsoleKey.Home: _currentIndex = 0; break;
            case ConsoleKey.End: _currentIndex = Math.Max(0, count - 1); break;
            case ConsoleKey.PageUp: _currentIndex = Math.Max(0, _currentIndex - Math.Max(1, ContentRect.Height - 1)); break;
            case ConsoleKey.PageDown: _currentIndex = Math.Min(Math.Max(0, count - 1), _currentIndex + Math.Max(1, ContentRect.Height - 1)); break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.Backspace: NavigateToParent(); break;
            case ConsoleKey.F5: Refresh(); break;
            case ConsoleKey.F2: SelectCurrentDirectory(); break;
            case ConsoleKey.Escape:
                var cancel = Cancelled; cancel?.Invoke(); return cancel is not null;
            case ConsoleKey.Spacebar:
                if (SelectionMode == FilePickerMode.File) return false;
                if (SelectedEntry is DirectoryInfo selectedDir)
                {
                    if (Directory.Exists(selectedDir.FullName)) Confirm(selectedDir.FullName, true);
                    else { Refresh(); ErrorMessage = "The selected directory no longer exists."; }
                }
                else SelectCurrentDirectory();
                break;
            case ConsoleKey.Enter:
            case ConsoleKey.RightArrow:
                if (SelectedEntry is DirectoryInfo dir) NavigateTo(dir.FullName);
                else if (key.Key == ConsoleKey.Enter && SelectedEntry is FileInfo file && SelectionMode != FilePickerMode.Directory)
                {
                    if (File.Exists(file.FullName)) Confirm(file.FullName, false);
                    else { Refresh(); ErrorMessage = "The selected file no longer exists."; }
                }
                break;
            default:
                if (!char.IsLetterOrDigit(key.KeyChar) || count == 0) return false;
                for (int offset = 1; offset <= count; offset++)
                {
                    int index = (_currentIndex + offset) % count;
                    if (_entries[index].Name.StartsWith(key.KeyChar.ToString(), StringComparison.OrdinalIgnoreCase)) { _currentIndex = index; break; }
                }
                break;
        }
        if (!IsDisposed) Invalidate(); return true;
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        if (a.Width == 0 || a.Height == 0) return;
        ControlDrawing.Text(buffer, a.X, a.Y, CurrentDirectory, a.Width, Foreground, Background);
        if (a.Height < 2) return;
        if (ErrorMessage is { } error)
        { ControlDrawing.Text(buffer, a.X, a.Y + 1, error, a.Width, Color.Red, Background); return; }
        if (_entries.Count == 0)
        { ControlDrawing.Text(buffer, a.X, a.Y + 1, "(empty directory)", a.Width, Foreground, Background); return; }
        int rows = a.Height - 1;
        _currentIndex = Math.Clamp(_currentIndex, 0, _entries.Count - 1);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _entries.Count - rows));
        if (_currentIndex < _scrollOffset) _scrollOffset = _currentIndex;
        if (_currentIndex >= _scrollOffset + rows) _scrollOffset = _currentIndex - rows + 1;
        for (int row = 0; row < rows && row + _scrollOffset < _entries.Count; row++)
        {
            int index = row + _scrollOffset; var entry = _entries[index];
            bool selected = IsFocused && index == _currentIndex;
            string label = (entry is DirectoryInfo ? "[+] " : "    ") + entry.Name;
            ControlDrawing.Text(buffer, a.X, a.Y + 1 + row, label.PadRight(a.Width), a.Width,
                selected ? FocusedForeground : Foreground, selected ? FocusedBackground : Background);
        }
    }
    protected override void DisposeCore() { SelectionConfirmed = null; Cancelled = null; base.DisposeCore(); }
}
