using MelonUI.Base;

namespace MelonUI.Managers;

/// <summary>A popup registration. Close detaches its content; Dispose also disposes the content.</summary>
public sealed class PopupHandle : IDisposable
{
    private readonly ConsoleWindowManager _window;
    private readonly UIElement? _previousFocus;
    private readonly int _attachmentVersion;
    private bool _closed;
    public UIElement Content { get; }
    public bool IsModal { get; }
    public bool IsOpen => IsActive;
    internal UIElement? PreviousFocus => _previousFocus;
    internal bool IsActive => !_closed && !Content.IsDisposed && Content.ParentWindow == _window && Content.AttachmentVersion == _attachmentVersion;

    internal PopupHandle(ConsoleWindowManager window, UIElement content, UIElement? previousFocus, bool isModal)
    {
        _window = window;
        Content = content;
        _previousFocus = previousFocus;
        _attachmentVersion = content.AttachmentVersion;
        IsModal = isModal;
    }

    public void Close()
    {
        if (_closed) return;
        if (Content.IsDisposed || Content.ParentWindow != _window || Content.AttachmentVersion != _attachmentVersion)
        {
            _closed = true;
            return;
        }
        _window.VerifyAccess();
        _closed = true;
        Content.Remove();
        _window.RestorePopupFocus(_previousFocus);
    }

    public void Dispose()
    {
        if (Content.IsDisposed) return;
        Close();
        Content.Dispose();
    }
}
