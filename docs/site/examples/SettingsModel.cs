using System.ComponentModel;
using System.Drawing;
using MelonUI.Default;

namespace MelonUI.Docs;

public sealed class SettingsModel : INotifyPropertyChanged
{
    private string _name = "Melon listener";
    private string _status = "Ready";
    private Color _accent = Color.FromArgb(18, 171, 239);
    public event PropertyChangedEventHandler? PropertyChanged;
    public float[] Peaks { get; } = [.1f, .3f, .8f, .5f, 1f, .6f, .2f, .4f];
    public string Name
    {
        get => _name;
        set { if (_name == value) return; _name = value; Changed(nameof(Name)); }
    }
    public string Status
    {
        get => _status;
        private set { _status = value; Changed(nameof(Status)); }
    }
    public Color Accent
    {
        get => _accent;
        set { _accent = value; Changed(nameof(Accent)); }
    }
    public void Save() => Status = $"Saved {Name}";
    public void Submit(string text, TextBox input) => Status = $"Hello, {text}";
    public void PreviewColor(Color color) => Accent = color;
    public void SaveColor(Color color) => Status = $"Accent #{color.R:X2}{color.G:X2}{color.B:X2}";
    public void OpenFile(string path) => Status = $"Opened {System.IO.Path.GetFileName(path)}";
    private void Changed(string property) => PropertyChanged?.Invoke(this, new(property));
}
