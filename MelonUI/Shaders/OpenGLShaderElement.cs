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
        private OpenGLWindowHandler _openGLWindowHandler;
        private Thread _openGLWindowHandlerThread;

        [Binding]
        private string shaderPath;

        [Binding]
        private string shaderSource;

        public OpenGLShaderElement()
        {
            _openGLWindowHandler = new OpenGLWindowHandler(120, 1);
        }

        private bool IsInit;
        public void Init()
        {
            _openGLWindowHandler.ShaderPath = ShaderPath;
            _openGLWindowHandler.ShaderSource = ShaderSource;
            _openGLWindowHandlerThread = new Thread(() =>
            {
                lock (_openGLWindowHandler._renderLock)
                {
                    _openGLWindowHandler.Context.MakeCurrent();
                }

                _openGLWindowHandler.Run();
            });
            _openGLWindowHandlerThread.Start();

            //while (_openGLWindowHandler.IsReady == false) ;

            MinWidth = "1";
            MinHeight = "1";
        }


        /// <summary>
        /// Renders the FPS display text inside the UIElement.
        /// </summary>
        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (!IsInit)
            {
                Init();
                IsInit = true;
            }

            if (buffer.Width != _openGLWindowHandler.currentFBOWidth || buffer.Height != _openGLWindowHandler.currentFBOHeight)
            {
                _openGLWindowHandler.newWidth = buffer.Width;
                _openGLWindowHandler.newHeight = buffer.Height;
                buffer.Resize(_openGLWindowHandler.newWidth, _openGLWindowHandler.newHeight);

            }


            lock (_openGLWindowHandler._renderLock)
            {
                int width = buffer.Width;
                int height = buffer.Height;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceIndex = y * width + x;
                        int destIndex = (height - 1 - y) * width + x;  // Changed this line
                        buffer.buffer[destIndex] = _openGLWindowHandler.PixelBufferPair[_openGLWindowHandler.PixelBufferReadIndex][sourceIndex];
                    }
                }
            }
        }
    }
}
