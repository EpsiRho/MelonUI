using System.ComponentModel;

namespace MelonUIDemo;

// The object Settings.mxml binds to. Raising PropertyChanged is what makes the page update.
public sealed class SettingsModel : INotifyPropertyChanged
{
    private string _name = "Melon listener";
    private string _status = "Ready";
    public event PropertyChangedEventHandler? PropertyChanged;

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

    // Called by the button's OnPressed="{App.Save}".
    public void Save() => Status = $"Saved {Name}";

    private void Changed(string property) => PropertyChanged?.Invoke(this, new(property));
}
