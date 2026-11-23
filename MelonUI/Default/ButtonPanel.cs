using MelonUI.Base;
using MelonUI.Managers;

namespace MelonUI.Default;

/// <summary>A single focus stop for a horizontal set of actions and its overflow menu.</summary>
public class ButtonPanel : UIElement
{
    public IReadOnlyList<Button> Buttons => Children.OfType<Button>().ToArray();
    protected internal override IEnumerable<UIElement> FocusChildren => [];
    private readonly List<Button> _visible = new();
    private readonly List<Button> _overflow = new();
    private readonly Dictionary<Button, (int X, int Y, int Width)> _positions = new();
    private PopupHandle? _popup;
    private int _selected;
    public int MaxLines { get; set; } = 1;
    public int MaxOverflowMenuLines { get; set; } = 5;
    public ButtonPanel() { ShowBorder = false; Height = "1"; EnableCaching = false; }
    public void AddButton(Button button) => Children.Add(button);
    public void RemoveButton(Button button) => Children.Remove(button);
    protected override void LayoutChildren()
    {
        _visible.Clear(); _overflow.Clear(); _positions.Clear();
        var a = ContentRect;
        var buttons = Buttons.Where(b => b.IsVisible).ToArray();
        int rows = Math.Max(0, Math.Min(MaxLines, a.Height));
        int x = a.X, row = 0;
        foreach (var button in buttons)
        {
            int width = ConsoleBuffer.GetStringWidth(button.Caption);
            int reserved = 8;
            if (x > a.X && x + width > a.X + a.Width - reserved) { x = a.X; row++; }
            if (row >= rows || width > Math.Max(0, a.Width - reserved)) { _overflow.Add(button); continue; }
            _visible.Add(button); _positions.Add(button, (x, a.Y + row, width)); x += width + 1;
        }
        foreach (var child in Children) child.CalculateLayout(0, 0, 0, 0);
        _selected = Math.Clamp(_selected, 0, Math.Max(0, _visible.Count - 1));
        if (_popup is { IsActive: true } && _overflow.Count == 0) CloseOverflow();
    }
    private void ToggleOverflow()
    {
        if (_popup is { IsActive: true }) { CloseOverflow(); return; }
        CloseOverflow();
        if (_overflow.Count == 0 || ParentWindow is not { } window) return;
        int viewWidth = Math.Max(1, window.ViewportWidth), viewHeight = Math.Max(1, window.ViewportHeight);
        int menuWidth = Math.Min(viewWidth, Math.Max(12, ActualWidth));
        int menuHeight = Math.Min(viewHeight, Math.Max(1, MaxOverflowMenuLines) + 2);
        int menuY = GetAbsoluteY() + ActualHeight;
        if (menuY + menuHeight > viewHeight) menuY = Math.Max(0, GetAbsoluteY() - menuHeight);
        var menu = new OverflowMenu(_overflow.ToList())
        {
            Width = $"{menuWidth}", Height = $"{menuHeight}",
            X = $"{Math.Clamp(GetAbsoluteX(), 0, viewWidth - menuWidth)}", Y = $"{menuY}", MaxOverflowMenuLines = MaxOverflowMenuLines
        };
        menu.Invoked += CloseOverflow;
        _popup = window.ShowPopup(menu);
    }
    private void CloseOverflow() { var popup = _popup; _popup = null; popup?.Dispose(); }
    public override bool TryHandleKey(ConsoleKeyInfo key)
    {
        if (LockControls || !IsEffectivelyVisible || !IsEffectivelyEnabled) return false;
        if (base.TryHandleKey(key)) return true;
        if ((key.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control | ConsoleModifiers.Shift)) != 0) return false;
        if (key.Key == ConsoleKey.Oem3) { ToggleOverflow(); return true; }
        if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow)
        {
            if (_visible.Count > 0) _selected = (_selected + (key.Key == ConsoleKey.LeftArrow ? -1 : 1) + _visible.Count) % _visible.Count;
            Invalidate(); return true;
        }
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            if (_visible.Count > 0) _visible[_selected].PerformClick();
            else if (_overflow.Count > 0) ToggleOverflow();
            return true;
        }
        var button = Buttons.FirstOrDefault(b => b.IsVisible && b.IsEnabled && b._key == key.Key);
        if (button is null) return false;
        button.PerformClick(); return true;
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        foreach (var button in _visible)
        {
            var p = _positions[button];
            bool selected = IsFocused && button == _visible[_selected];
            ControlDrawing.Text(buffer, p.X, p.Y, button.Caption, p.Width,
                button.IsEnabled ? selected ? FocusedForeground : Foreground : System.Drawing.Color.Gray,
                selected ? FocusedBackground : Background);
        }
        if (_overflow.Count > 0 && a.Height > 0)
            ControlDrawing.Text(buffer, a.X + Math.Max(0, a.Width - 8), a.Y + Math.Max(0, Math.Min(MaxLines, a.Height) - 1), "[(`) …]", Math.Min(a.Width, 8), Foreground, Background);
    }
    protected override void OnDetachedFromWindow(ConsoleWindowManager window) { CloseOverflow(); base.OnDetachedFromWindow(window); }
    protected override void DisposeCore() { CloseOverflow(); base.DisposeCore(); }
}

public class OverflowMenu : OptionsMenu
{
    private readonly List<(Button Button, UIElement? Owner)> _sources = new();
    public int MaxOverflowMenuLines { get; set; } = 5;
    internal event Action? Invoked;
    public OverflowMenu(List<Button> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        foreach (var button in buttons)
        {
            var action = button;
            var owner = button.Parent;
            _sources.Add((button, owner));
            Options.Add(new MenuItem(action.Text, () =>
            {
                if (!Available(action, owner)) return;
                action.PerformClick();
                Invoked?.Invoke();
            }) { IsEnabled = Available(action, owner) });
        }
    }
    private static bool Available(Button button, UIElement? owner) =>
        !button.IsDisposed && button.Parent == owner && button.IsEffectivelyVisible && button.IsEffectivelyEnabled && !button.LockControls;
    private void RefreshActions()
    {
        for (int i = 0; i < Math.Min(_sources.Count, Options.Count); i++)
        {
            var (button, owner) = _sources[i];
            Options[i].IsEnabled = Available(button, owner);
            if (!button.IsDisposed) Options[i].Option = button.Text;
        }
    }
    public override bool TryHandleKey(ConsoleKeyInfo key) { RefreshActions(); return base.TryHandleKey(key); }
    protected override void RenderContent(ConsoleBuffer buffer) { RefreshActions(); base.RenderContent(buffer); }
    protected override void DisposeCore() { Invoked = null; base.DisposeCore(); }
}
