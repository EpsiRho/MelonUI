using System.Diagnostics;
using MelonUI.Base;

namespace MelonUI.Default;

public class GridContainer : UIElement
{
    private readonly int _rows;
    private readonly int[] _columns;
    private readonly Dictionary<UIElement, (int Row, int Column)> _positions = new();
    private readonly Stopwatch _animation = new();
    private bool _closing;
    private Action? _closed;
    private const double AnimationSeconds = .18;
    public int Rows => _rows;
    public IReadOnlyList<int> ColumnsPerRow => Array.AsReadOnly(_columns);
    public GridContainer() : this(1) { }
    public GridContainer(int rows, bool animateExpansion = false, params int[] columnsPerRow)
    {
        if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));
        ArgumentNullException.ThrowIfNull(columnsPerRow);
        if (columnsPerRow.Length == 0) columnsPerRow = Enumerable.Repeat(1, rows).ToArray();
        if (columnsPerRow.Length != rows || columnsPerRow.Any(c => c < 1)) throw new ArgumentException("One positive column count is required per row.", nameof(columnsPerRow));
        _rows = rows; _columns = (int[])columnsPerRow.Clone();
        ConsiderForFocus = false; EnableCaching = false;
        if (animateExpansion) _animation.Start();
    }
    public void AddElement(UIElement element, int row, int column) { ValidateCell(row, column); Children.Add(element); SetCell(element, row, column); }
    public void SetCell(UIElement element, int row, int column)
    {
        VerifyAccess(); ValidateCell(row, column);
        if (element.Parent != this) throw new InvalidOperationException("Add the element to this grid first.");
        _positions[element] = (row, column); Invalidate();
    }
    private void ValidateCell(int row, int column)
    {
        if (row < 0 || row >= _rows) throw new ArgumentOutOfRangeException(nameof(row));
        if (column < 0 || column >= _columns[row]) throw new ArgumentOutOfRangeException(nameof(column));
    }
    protected override void OnChildAdded(UIElement child) { _positions.Add(child, (0, 0)); base.OnChildAdded(child); }
    protected override void OnChildRemoved(UIElement child) { _positions.Remove(child); base.OnChildRemoved(child); }
    protected override void LayoutChildren()
    {
        var a = ContentRect;
        double amount = _animation.IsRunning ? Math.Clamp(_animation.Elapsed.TotalSeconds / AnimationSeconds, 0, 1) : 1;
        if (_closing) amount = 1 - amount;
        int width = (int)(a.Width * amount), height = (int)(a.Height * amount);
        int x = a.X + (a.Width - width) / 2, y = a.Y + (a.Height - height) / 2;
        foreach (var child in Children.ToArray())
        {
            if (child.IsDisposed || child.Parent != this) continue;
            var cell = _positions[child];
            int left = (int)((long)width * cell.Column / _columns[cell.Row]);
            int right = (int)((long)width * (cell.Column + 1) / _columns[cell.Row]);
            int top = (int)((long)height * cell.Row / _rows);
            int bottom = (int)((long)height * (cell.Row + 1) / _rows);
            child.CalculateLayout(x + left, y + top, right - left, bottom - top);
        }
    }
    public void AnimateOpen() { VerifyAccess(); _closing = false; _closed = null; IsVisible = true; _animation.Restart(); Invalidate(); }
    public void AnimateClose(Action? finalAction = null)
    {
        VerifyAccess();
        if (!IsVisible) { finalAction?.Invoke(); return; }
        _closing = true; _closed = finalAction; _animation.Restart(); Invalidate();
    }
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        foreach (var child in Children.Where(c => c.IsVisible && c.ActualWidth > 0 && c.ActualHeight > 0).OrderBy(c => c.Z).ToArray())
            if (!child.IsDisposed && child.Parent == this)
                buffer.WriteBuffer(child.ActualX, child.ActualY, child.Render(), child.RespectBackgroundOnDraw);
        if (_animation.IsRunning && _animation.Elapsed.TotalSeconds >= AnimationSeconds)
        {
            _animation.Stop();
            if (_closing)
            {
                _closing = false; IsVisible = false;
                var action = _closed; _closed = null; action?.Invoke();
            }
        }
    }
}
