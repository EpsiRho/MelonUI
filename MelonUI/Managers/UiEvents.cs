namespace MelonUI.Managers;

public sealed class UiUpdateEventArgs(TimeSpan elapsed) : EventArgs
{
    public TimeSpan Elapsed { get; } = elapsed;
}

public sealed class UiExceptionEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
    public bool Handled { get; set; }
}
