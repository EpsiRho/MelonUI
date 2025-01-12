using ComputeSharp;
using MelonUI.Base;
using MelonUI.Default;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MelonUI.Components
{
    using ComputeSharp;

    [ThreadGroupSize(DefaultThreadGroupSizes.XZ)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MyShader : IComputeShader
    {
        public readonly IReadWriteNormalizedTexture2D<float4> texture;

        public void Execute()
        {
            float2 uv = ThreadIds.XY / new float2(texture.Width, texture.Height);
            float3 color = new float3(uv.X, uv.Y, 0.5f);
            texture[ThreadIds.XY] = new float4(color, 1.0f);
        }
    }
    public class ShaderDisplay : UIElement
    {
        private readonly int textureWidth;
        private readonly int textureHeight;
        private ReadWriteTexture2D<float4> texture;

        private ShaderBase managedShader;

        public ShaderDisplay()
        {
            textureWidth = 1;
            textureHeight = 1;
            texture = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<float4>(textureWidth, textureHeight);
            managedShader = new ShaderBase(new MyShader());
        }

        /// <summary>
        /// Updates the texture using the provided shader.
        /// </summary>
        /// <typeparam name="T">The type of the shader.</typeparam>
        public void UpdateShader(ShaderBase shader, params object[] args)
        {
            var sType = shader.GetType();
            //GraphicsDevice.GetDefault().For(textureWidth, textureHeight, shader);
            NeedsRecalculation = true;
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            var data = new float4[textureWidth * textureHeight];
            texture.CopyTo(data);

            for (int y = 0; y < Math.Min(textureHeight, buffer.Height); y++)
            {
                for (int x = 0; x < Math.Min(textureWidth, buffer.Width); x++)
                {
                    var pixel = data[y * textureWidth + x];
                    var color = Color.FromArgb(
                        (int)(pixel.X * 255),
                        (int)(pixel.Y * 255),
                        (int)(pixel.Z * 255)
                    );

                    buffer.SetPixel(x, y, '█', color, Background);
                }
            }
        }
    }
}
