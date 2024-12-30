using MelonUI.Base;
using MelonUI.Enums;
using MelonUI.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Default
{
    public class WorkItem
    {
        public int IndicatorIndex { get; set; }
        public int IndicatorTickMax { get; set; } = 10;
        public static string[] Indicators 
            = new string[] { "⠁", "⠉", "⠙", "⠸", "⠴", "⠦", "⠇", "⠃" };
        public string Label { get; set; }
        private WorkStatus _status;
        public WorkStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if(value == _status)
                {
                    return;
                }
                
                if(value == WorkStatus.Running)
                {
                    timeSinceRunning = Stopwatch.StartNew();
                }
                else if (value == WorkStatus.Completed || value == WorkStatus.Errored)
                {
                    if (timeSinceRunning != null)
                    {
                        timeSinceRunning.Stop();
                    }
                }
                _status = value;
            }
        }
        public Stopwatch timeSinceRunning { get; set; }
        public long TotalItems { get; set; }
        public long CompletedItems { get; set; }
        public string ElmName { get; set; }
        public WorkItem()
        { 

        }
        public WorkItem(string name, string label, bool isRunning)
        {
            ElmName = name;
            Label = label;
            Status = isRunning ? WorkStatus.Running : WorkStatus.Waiting;
            if (isRunning)
            {
                timeSinceRunning = Stopwatch.StartNew();
            }
        }
        public WorkItem(string name, string label, int totalItems, bool isRunning)
        {
            ElmName = name;
            Label = label;
            TotalItems = totalItems;
            Status = isRunning ? WorkStatus.Running : WorkStatus.Waiting;
            if (isRunning)
            {
                timeSinceRunning = Stopwatch.StartNew();
            }
        }

        public void Complete()
        {
            Status = WorkStatus.Completed;
        }
        public void Error()
        {
            Status = WorkStatus.Errored;
        }
        public void Run()
        {
            Status = WorkStatus.Running;
        }
        public void Wait()
        {
            Status = WorkStatus.Waiting;
        }

        public string GetIndicator()
        {
            if (Status == WorkStatus.Running)
            {
                var indicator = Indicators[GetIndicatorIndexByTime()];
                return indicator;
            }
            else if (Status == WorkStatus.Errored)
            {
                return "✕";
            }
            return Status == WorkStatus.Completed ? "✓" : "⠿";
        }

        private int GetIndicatorIndexByTime()
        {
            if (timeSinceRunning == null) return 0;
            long elapsedMilliseconds = timeSinceRunning.ElapsedMilliseconds;
            return (int)((elapsedMilliseconds / 150) % Indicators.Length); // Change speed via interval
        }

        public string GetElapsedTime()
        {
            if (timeSinceRunning == null) return "00:00:00";
            var elapsed = timeSinceRunning.Elapsed;
            return $"{elapsed.Minutes:00}:{elapsed.Seconds:00}:{elapsed.Milliseconds:00}";
        }
    }
    public class WorkLog : UIElement
    {
        public Color RunningForeground { get; set; } = Color.FromArgb(255, 33, 165, 255);
        public Color WaitingForeground { get; set; } = Color.FromArgb(255, 105, 105, 105);
        public Color ErroredForeground { get; set; } = Color.FromArgb(255, 255, 32, 32);
        public Color CompletedForeground { get; set; } = Color.FromArgb(255, 32, 255, 108);
        private LockedList<WorkItem> Tasks;
        private int _visibleStartIndex;
        public int VisibleStartIndex
        {
            get
            {
                return _visibleStartIndex;
            }
            set
            {
                if (value != _visibleStartIndex && value != -1)
                {
                    _visibleStartIndex = value;
                }
            }
        }
        private int _totalLines;

        public bool ShowStatus { get; set; } = true;
        public bool ShowTime { get; set; } = true;
        public bool ShowProgressBar { get; set; } = false;
        public bool ShowItemCount { get; set; } = false;

        public WorkLog(LockedList<WorkItem> tasks)
        {
            Tasks = tasks;
            VisibleStartIndex = 0;
        }
        public WorkItem GetItemByName(string name)
        {
            return Tasks.FirstOrDefault(x => x.ElmName == name);
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            try
            {
                int availableHeight = ActualHeight - (ShowBorder ? 2 : 0);
                int width = ActualWidth - (ShowBorder ? 2 : 0);
                _totalLines = availableHeight;

                UpdateVisibleStartIndex();
                RenderTasks(buffer, width);
            }
            catch (Exception ex)
            {
            }
        }


        private void UpdateVisibleStartIndex()
        {
            int visibleCount = Tasks.Where(x => x.Status == WorkStatus.Waiting || x.Status == WorkStatus.Running).Count();
            if (visibleCount < _totalLines)
            {
                _visibleStartIndex = Math.Max(Tasks.Count - _totalLines, 0);
            }
            else
            {
                int latestRunningIndex = Tasks.IndexOf(Tasks.FirstOrDefault(t => t.Status == WorkStatus.Running));
                if (latestRunningIndex != -1) _visibleStartIndex = latestRunningIndex;

                _visibleStartIndex = Math.Clamp(_visibleStartIndex, 0, Math.Max(Tasks.Count - _totalLines, 0));
            }
        }


        private void RenderTasks(ConsoleBuffer buffer, int width)
        {
            int xOffset = ShowBorder ? 1 : 0;
            int yOffset = ShowBorder ? 1 : 0;
            int renderedLines = 0;

            for (int i = VisibleStartIndex; i < Tasks.Count && renderedLines < _totalLines; i++)
            {
                var task = Tasks[i];
                string line = FormatTaskLine(task, width);
                buffer.WriteString(xOffset, yOffset + renderedLines, line, GetTaskColor(task), Background);
                renderedLines++;
            }
        }

        private string FormatTaskLine(WorkItem task, int width)
        {
            int itemCountBoost = ShowItemCount ? 1 : 0;
            string status = ShowStatus ? task.GetIndicator() : "";
            string time = ShowTime ? GetElapsedTime(task) : "";

            string itemCount = ShowItemCount && task.TotalItems > 0
                ? $"({task.CompletedItems}/{task.TotalItems})"
                : "";

            string progress = ShowProgressBar && task.TotalItems > 0
                ? RenderProgressBar(task, width - time.Length - itemCount.Length - status.Length - task.Label.Length - 7 - itemCountBoost)
                : "";

            string Name = ShowProgressBar && progress != "" ? progress : task.Label;

            string namePart = "";
            if (itemCount == "")
            {
                namePart = $"{status} {task.Label} {progress}".Trim();
            }
            else
            {
                namePart = $"{status} {itemCount} {task.Label} {progress}".Trim();
            }
            string trimmedName = namePart.Length > width - time.Length - 1
                ? namePart.Substring(0, width - time.Length - 6) + "..."
                : namePart;

            return $" {trimmedName.PadRight(width - time.Length - 3 - itemCountBoost)} {time}";
        }

        private string RenderProgressBar(WorkItem task, int barWidth)
        {
            if(barWidth <= 2)
            {
                return "";
            }
            int completedBlocks = (int)((double)task.CompletedItems / task.TotalItems * barWidth);
            completedBlocks = completedBlocks >= barWidth ? barWidth : completedBlocks;
            return $"[{new string('#', completedBlocks)}{new string('-', barWidth - completedBlocks)}]";
        }

        private string GetElapsedTime(WorkItem task)
        {
            if (task.timeSinceRunning == null) return "00:00";
            var elapsed = task.timeSinceRunning.Elapsed;
            return $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        private Color GetTaskColor(WorkItem task)
        {
            return task.Status switch
            {
                WorkStatus.Running => RunningForeground,
                WorkStatus.Waiting => WaitingForeground,
                WorkStatus.Errored => ErroredForeground,
                WorkStatus.Completed => CompletedForeground,
                _ => Foreground,
            };
        }
    }
}