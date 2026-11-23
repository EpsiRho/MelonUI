using MelonUI.Base;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using MelonUI.Attributes;


namespace MelonUI.Shaders
{
    public partial class OpenGLShaderElement : UIElement
    {
        private OpenGLWindowHandler? _openGLWindowHandler;
        private Thread? _openGLWindowHandlerThread;
        private volatile Exception? _renderFailure;

        [Binding]
        private string shaderPath = "";

        [Binding]
        private string shaderSource = "";
        public string? ErrorMessage => _renderFailure?.GetBaseException().Message;

        public OpenGLShaderElement()
        {
            ConsiderForFocus = false;
            EnableCaching = false;
        }

        private bool IsInit;
        public void Init()
        {
            VerifyAccess();
            if (IsInit) return;
            // Parsing/constructing a page must not create a GLFW window or require a display.
            _openGLWindowHandler = new OpenGLWindowHandler(120, 1);
            _renderFailure = null;
            IsInit = true;
            _openGLWindowHandler.ShaderPath = ShaderPath;
            _openGLWindowHandler.ShaderSource = ShaderSource;
            _openGLWindowHandlerThread = new Thread(() =>
            {
                try
                {
                    lock (_openGLWindowHandler._renderLock)
                    {
                        _openGLWindowHandler.Context.MakeCurrent();
                    }
                    _openGLWindowHandler.Run();
                }
                catch (Exception ex) { _renderFailure = ex; }
                finally
                {
                    try { _openGLWindowHandler.Dispose(); }
                    catch (Exception ex) { _renderFailure ??= ex; }
                }
            }) { IsBackground = true, Name = "MelonUI shader" };
            _openGLWindowHandlerThread.Start();

            //while (_openGLWindowHandler.IsReady == false) ;

            MinWidth = "1";
            MinHeight = "1";
        }

        protected override void DisposeCore()
        {
            StopRenderer();
            base.DisposeCore();
        }
        protected override void OnDetachedFromWindow(MelonUI.Managers.ConsoleWindowManager window)
        {
            if (_openGLWindowHandler is { } handler) handler.Paused = true;
            base.OnDetachedFromWindow(window);
        }
        protected override void OnAttachedToWindow(MelonUI.Managers.ConsoleWindowManager window)
        {
            if (_openGLWindowHandler is { } handler) handler.Paused = false;
            base.OnAttachedToWindow(window);
        }
        private void StopRenderer()
        {
            _openGLWindowHandler?.RequestStop();
            if (_openGLWindowHandlerThread is not null) _openGLWindowHandlerThread.Join();
            else _openGLWindowHandler?.Dispose();
            _openGLWindowHandler = null; _openGLWindowHandlerThread = null; IsInit = false;
        }


        /// <summary>
        /// Renders the FPS display text inside the UIElement.
        /// </summary>
        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (_renderFailure is null && !IsInit)
            {
                try { Init(); }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException) { _renderFailure = ex; }
            }
            if (_renderFailure is not null)
            {
                var a = ContentRect;
                ControlDrawing.Text(buffer, a.X, a.Y, ErrorMessage + " Initialize OpenGL with Init() on the application main thread before RunAsync().", a.Width, Foreground, Background);
                return;
            }
            _openGLWindowHandler!.Frames.RequestSize(buffer.Width, buffer.Height);
            _openGLWindowHandler.Frames.CopyLatestTo(buffer);
        }
    }
}
