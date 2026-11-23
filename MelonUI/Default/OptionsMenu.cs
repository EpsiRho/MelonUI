using System.Drawing;
using MelonUI.Attributes;
using MelonUI.Base;

namespace MelonUI.Default;

public partial class MenuItem
{
    [Binding] private string _Option = "";
    [Binding] private Action? onSelect;
    [Binding] private bool _IsEnabled = true;
    public MenuItem() { }
    public MenuItem(string option, Action? onSelect) { Option = option; OnSelect = onSelect; }
}

public partial class OptionsMenu : UIElement
{
    [Binding] private List<MenuItem> _Options = new();
    [Binding] private string menuName = "";
    [Binding] private Color selectedForeground = Color.Cyan;
    [Binding] private Color selectedBackground = Color.FromArgb(80, 80, 80);
    private int _currentIndex;
    private int _scrollOffset;
    public int SelectedIndex
    {
        get => Options is { Count: > 0 } ? Math.Clamp(_currentIndex, 0, Options.Count - 1) : -1;
        set { VerifyAccess(); _currentIndex = Math.Max(0, value); Invalidate(); }
    }
    public MenuItem? SelectedItem => SelectedIndex >= 0 ? Options[SelectedIndex] : null;

    public OptionsMenu() { EnableCaching = false; }
    public void Show() => IsVisible = true;
    public override bool TryHandleKey(ConsoleKeyInfo key)
    {
        if (LockControls || !IsEffectivelyVisible || !IsEffectivelyEnabled) return false;
        if (base.TryHandleKey(key)) return true;
        if ((key.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) return false;
        int count = Options?.Count ?? 0;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow: Move(-1); return true;
            case ConsoleKey.DownArrow: Move(1); return true;
            case ConsoleKey.Home: SelectEdge(false); return true;
            case ConsoleKey.End: SelectEdge(true); return true;
            case ConsoleKey.PageUp: Move(-Math.Max(1, ContentRect.Height - 1)); return true;
            case ConsoleKey.PageDown: Move(Math.Max(1, ContentRect.Height - 1)); return true;
            case ConsoleKey.Enter:
            case ConsoleKey.RightArrow:
                if (SelectedItem is { IsEnabled: true } item) item.OnSelect?.Invoke();
                if (!IsDisposed) Invalidate(); return true;
        }
        if (count > 0 && char.IsLetterOrDigit(key.KeyChar))
        {
            for (int offset = 1; offset <= count; offset++)
            {
                int index = (SelectedIndex + offset) % count;
                if (Options![index] is { IsEnabled: true } item && (item.Option ?? "").StartsWith(key.KeyChar.ToString(), StringComparison.OrdinalIgnoreCase))
                { SelectedIndex = index; break; }
            }
            return true;
        }
        return false;
    }

    private void Move(int direction)
    {
        int count = Options?.Count ?? 0;
        if (count == 0) return;
        int index = SelectedIndex;
        int sign = Math.Sign(direction);
        for (int step = 0; step < Math.Max(1, Math.Abs(direction)); step++)
            for (int search = 0; search < count; search++)
            {
                index = (index + sign + count) % count;
                if (Options![index]?.IsEnabled == true) break;
            }
        SelectedIndex = index;
    }

    private void SelectEdge(bool last)
    {
        var enabled = (Options ?? []).Select((item, index) => (item, index)).Where(x => x.item?.IsEnabled == true).ToArray();
        if (enabled.Length > 0) SelectedIndex = last ? enabled[^1].index : enabled[0].index;
    }

    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var area = ContentRect;
        if (area.Width == 0 || area.Height == 0) return;
        int y = area.Y, rows = area.Height;
        if (!string.IsNullOrEmpty(MenuName))
        { ControlDrawing.Text(buffer, area.X, y++, MenuName, area.Width, Foreground, Background); rows--; }
        if (rows <= 0) return;
        int count = Options?.Count ?? 0;
        _currentIndex = count == 0 ? 0 : Math.Clamp(_currentIndex, 0, count - 1);
        if (count == 0) { ControlDrawing.Text(buffer, area.X, y, "(empty)", area.Width, Foreground, Background); return; }
        if (Options![_currentIndex]?.IsEnabled != true && Options.Any(x => x?.IsEnabled == true)) SelectEdge(false);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, count - rows));
        if (_currentIndex < _scrollOffset) _scrollOffset = _currentIndex;
        if (_currentIndex >= _scrollOffset + rows) _scrollOffset = _currentIndex - rows + 1;
        for (int row = 0; row < rows && row + _scrollOffset < count; row++)
        {
            int index = row + _scrollOffset;
            var item = Options[index];
            bool selected = index == _currentIndex && IsFocused;
            var fg = item?.IsEnabled == false ? Color.Gray : selected ? SelectedForeground : Foreground;
            ControlDrawing.Text(buffer, area.X, y + row, (item?.Option ?? "").PadRight(area.Width), area.Width, fg, selected ? SelectedBackground : Background);
        }
    }
}
