using System.Diagnostics;
using System.Drawing;
using MelonUI.Attributes;
using MelonUI.Base;
using MelonUI.Enums;

namespace MelonUI.Default;

public class WorkItem
{
    public static readonly string[] Indicators = ["⠁", "⠉", "⠙", "⠸", "⠴", "⠦", "⠇", "⠃"];
    public string Label { get; set; } = "";
    public string ElmName { get; set; } = "";
    public long TotalItems { get; set; }
    public long CompletedItems { get; set; }
    public Stopwatch timeSinceRunning { get; } = new();
    private WorkStatus _status = WorkStatus.Waiting;
    public WorkStatus Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            if (value == WorkStatus.Running) timeSinceRunning.Restart(); else timeSinceRunning.Stop();
            _status = value;
        }
    }
    public WorkItem() { }
    public WorkItem(string name, string label, bool isRunning) : this(name, label, 0, isRunning) { }
    public WorkItem(string name, string label, int totalItems, bool isRunning)
    { ElmName = name; Label = label; TotalItems = totalItems; Status = isRunning ? WorkStatus.Running : WorkStatus.Waiting; }
    public void Complete() => Status = WorkStatus.Completed;
    public void Error() => Status = WorkStatus.Errored;
    public void Run() => Status = WorkStatus.Running;
    public void Wait() => Status = WorkStatus.Waiting;
    public string GetIndicator() => Status switch
    {
        WorkStatus.Running => Indicators[(int)(timeSinceRunning.ElapsedMilliseconds / 150 % Indicators.Length)],
        WorkStatus.Completed => "✓", WorkStatus.Errored => "✕", _ => "⠿"
    };
    public string GetElapsedTime() => $"{(int)timeSinceRunning.Elapsed.TotalHours:00}:{timeSinceRunning.Elapsed.Minutes:00}:{timeSinceRunning.Elapsed.Seconds:00}";
}

public partial class WorkLog : UIElement
{
    [Binding] private Color runningForeground = Color.FromArgb(33, 165, 255);
    [Binding] private Color waitingForeground = Color.Gray;
    [Binding] private Color erroredForeground = Color.FromArgb(255, 32, 32);
    [Binding] private Color completedForeground = Color.FromArgb(32, 255, 108);
    [Binding] private LockedList<WorkItem> tasks = new();
    [Binding] private int _visibleStartIndex;
    [Binding] private bool showStatus = true;
    [Binding] private bool showTime = true;
    [Binding] private bool showProgressBar;
    [Binding] private bool showItemCount;
    public bool FollowRunning { get; set; } = true;
    public WorkLog() { ConsiderForFocus = false; EnableCaching = false; }
    public WorkLog(LockedList<WorkItem> tasks) : this() { Tasks = tasks ?? throw new ArgumentNullException(nameof(tasks)); }
    public WorkItem? GetItemByName(string name) => Tasks?.FirstOrDefault(item => item.ElmName == name);
    protected override void RenderContent(ConsoleBuffer buffer)
    {
        var a = ContentRect;
        if (a.Width == 0 || a.Height == 0) return;
        var snapshot = Tasks?.ToList().Where(t => t is not null).ToArray() ?? [];
        int start = Math.Clamp(VisibleStartIndex, 0, Math.Max(0, snapshot.Length - a.Height));
        if (FollowRunning)
        {
            int running = Array.FindIndex(snapshot, item => item.Status == WorkStatus.Running);
            start = running < 0 ? Math.Max(0, snapshot.Length - a.Height) : Math.Clamp(running, 0, Math.Max(0, snapshot.Length - a.Height));
        }
        for (int row = 0; row < a.Height && row + start < snapshot.Length; row++)
        {
            var item = snapshot[row + start];
            string prefix = (ShowStatus ? item.GetIndicator() + " " : "") + (ShowItemCount ? $"({Math.Max(0, item.CompletedItems)}/{Math.Max(0, item.TotalItems)}) " : "");
            string suffix = ShowTime ? " " + item.GetElapsedTime() : "";
            int remaining = Math.Max(0, a.Width - ConsoleBuffer.GetStringWidth(prefix + suffix));
            string label = ControlDrawing.Clip(item.Label, remaining);
            if (ShowProgressBar && item.TotalItems > 0 && remaining >= 5)
            {
                int width = Math.Min(20, Math.Max(1, remaining / 2 - 2));
                int full = (int)(Math.Clamp(item.CompletedItems / (double)item.TotalItems, 0, 1) * width);
                string bar = "[" + new string('#', full) + new string('-', width - full) + "]";
                label = ControlDrawing.Clip(item.Label, remaining - bar.Length - 1) + " " + bar;
            }
            var color = item.Status switch { WorkStatus.Running => RunningForeground, WorkStatus.Waiting => WaitingForeground,
                WorkStatus.Errored => ErroredForeground, WorkStatus.Completed => CompletedForeground, _ => Foreground };
            ControlDrawing.Text(buffer, a.X, a.Y + row, prefix + label + suffix, a.Width, color, Background);
        }
    }
}
