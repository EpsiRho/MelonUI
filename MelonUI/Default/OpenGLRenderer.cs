﻿using MelonUI.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;

namespace MelonUI.Default
{
    internal class Shader
    {
        public int Handle;

        public Shader(string vertexShaderSource, string fragmentShaderSource)
        {
            int VertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShader, vertexShaderSource);

            int FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShader, fragmentShaderSource);

            GL.CompileShader(VertexShader);

            int success = 0;

            GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(VertexShader);
                Console.WriteLine(infoLog);
            }

            GL.CompileShader(FragmentShader);

            GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(FragmentShader);
                Console.WriteLine(infoLog);
            }

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, VertexShader);
            GL.AttachShader(Handle, FragmentShader);

            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(Handle);
                Console.WriteLine(infoLog);
            }

            GL.DetachShader(Handle, VertexShader);
            GL.DetachShader(Handle, FragmentShader);
            GL.DeleteShader(FragmentShader);
            GL.DeleteShader(VertexShader);
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~Shader()
        {
            if (disposedValue == false)
            {
                Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    internal class OpenGLWindowHandler : GameWindow
    {
        public bool IsReady = false;
        
        float[] vertices = {
            // first triangle
     1f,  1f, 0.0f,  // top right
     1f, -1f, 0.0f,  // bottom right
    -1f,  1f, 0.0f,  // top left 
    // second triangle
     1f, -1f, 0.0f,  // bottom right
    -1f, -1f, 0.0f,  // bottom left
    -1f,  1f, 0.0f   // top left
        };

        private int _vertexBufferObject;

        private int _vertexArrayObject;

        // This class is a wrapper around a shader, which helps us manage it.
        // The shader class's code is in the Common project.
        // What shaders are and what they're used for will be explained later in this tutorial.
        private Shader _shader;

        // Fucking GLFW functions are barely thread safe.
        private readonly Lock _renderLock = new();

        private static Stopwatch _timer = new Stopwatch();


        public OpenGLWindowHandler(int width, int height) 
            : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = new Vector2i(width, height), Title = "", StartVisible = false })
        {
            this.Context.MakeNoneCurrent();

            _timer.Start();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            lock (_renderLock)
            {
                this.MakeCurrent();

                base.OnUpdateFrame(args);

                if (KeyboardState.IsKeyDown(Keys.Escape))
                {
                    Close();
                }

                this.Context.MakeNoneCurrent();
            }
        }

        protected override void OnLoad()
        {
            lock (_renderLock)
            {
                this.MakeCurrent();
                base.OnLoad();

                GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

                //Code goes here
                _vertexBufferObject = GL.GenBuffer();

                // Now, bind the buffer. OpenGL uses one global state, so after calling this,
                // all future calls that modify the VBO will be applied to this buffer until another buffer is bound instead.
                // The first argument is an enum, specifying what type of buffer we're binding. A VBO is an ArrayBuffer.
                // There are multiple types of buffers, but for now, only the VBO is necessary.
                // The second argument is the handle to our buffer.
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);

                // Finally, upload the vertices to the buffer.
                // Arguments:
                //   Which buffer the data should be sent to.
                //   How much data is being sent, in bytes. You can generally set this to the length of your array, multiplied by sizeof(array type).
                //   The vertices themselves.
                //   How the buffer will be used, so that OpenGL can write the data to the proper memory space on the GPU.
                //   There are three different BufferUsageHints for drawing:
                //     StaticDraw: This buffer will rarely, if ever, update after being initially uploaded.
                //     DynamicDraw: This buffer will change frequently after being initially uploaded.
                //     StreamDraw: This buffer will change on every frame.
                //   Writing to the proper memory space is important! Generally, you'll only want StaticDraw,
                //   but be sure to use the right one for your use case.
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

                // One notable thing about the buffer we just loaded data into is that it doesn't have any structure to it. It's just a bunch of floats (which are actaully just bytes).
                // The opengl driver doesn't know how this data should be interpreted or how it should be divided up into vertices. To do this opengl introduces the idea of a 
                // Vertex Array Obejct (VAO) which has the job of keeping track of what parts or what buffers correspond to what data. In this example we want to set our VAO up so that 
                // it tells opengl that we want to interpret 12 bytes as 3 floats and divide the buffer into vertices using that.
                // To do this we generate and bind a VAO (which looks deceptivly similar to creating and binding a VBO, but they are different!).
                _vertexArrayObject = GL.GenVertexArray();
                GL.BindVertexArray(_vertexArrayObject);

                // Now, we need to setup how the vertex shader will interpret the VBO data; you can send almost any C datatype (and a few non-C ones too) to it.
                // While this makes them incredibly flexible, it means we have to specify how that data will be mapped to the shader's input variables.

                // To do this, we use the GL.VertexAttribPointer function
                // This function has two jobs, to tell opengl about the format of the data, but also to associate the current array buffer with the VAO.
                // This means that after this call, we have setup this attribute to source data from the current array buffer and interpret it in the way we specified.
                // Arguments:
                //   Location of the input variable in the shader. the layout(location = 0) line in the vertex shader explicitly sets it to 0.
                //   How many elements will be sent to the variable. In this case, 3 floats for every vertex.
                //   The data type of the elements set, in this case float.
                //   Whether or not the data should be converted to normalized device coordinates. In this case, false, because that's already done.
                //   The stride; this is how many bytes are between the last element of one vertex and the first element of the next. 3 * sizeof(float) in this case.
                //   The offset; this is how many bytes it should skip to find the first element of the first vertex. 0 as of right now.
                // Stride and Offset are just sort of glossed over for now, but when we get into texture coordinates they'll be shown in better detail.
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

                // Enable variable 0 in the shader.
                GL.EnableVertexAttribArray(0);

                // We've got the vertices done, but how exactly should this be converted to pixels for the final image?
                // Modern OpenGL makes this pipeline very free, giving us a lot of freedom on how vertices are turned to pixels.
                // The drawback is that we actually need two more programs for this! These are called "shaders".
                // Shaders are tiny programs that live on the GPU. OpenGL uses them to handle the vertex-to-pixel pipeline.
                // Check out the Shader class in Common to see how we create our shaders, as well as a more in-depth explanation of how shaders work.
                // shader.vert and shader.frag contain the actual shader code.

                string vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
}          
";
                string fragmentShaderSource = @"
#version 330 core

#ifdef GL_ES
precision mediump float;
#endif

out vec4 FragColor;

uniform float time;
uniform vec2 resolution;

struct Ray {
	vec3 origin;
    	vec3 direction;
};

struct Light {
    	vec3 color;
    	vec3 direction;
};

struct Material {
    	vec3 color;
    	float diffuse;
    	float specular;
};

struct Intersect {
    	float len;
    	vec3 normal;
    	Material material;
};


struct Sphere {
    	float radius;
    	vec3 position;
    	Material material;
};

struct Plane {
    	vec3 normal;
    	Material material;
};
	
const float epsilon = 0.001;

const int iterations = 6;

const float exposure = 0.001;
const float gamma = 2.2;

const float intensity = 100.0;
const vec3 ambient = vec3(0.6, 0.8, 1.0) * intensity / gamma;

Light light;
Intersect miss = Intersect(0.0, vec3(0.0),Material(vec3(0.0), 0.0, 0.0));

Intersect intersect(Ray ray, Sphere sphere) {
    	vec3 oc = sphere.position - ray.origin;
    	float l = dot(ray.direction, oc);
    	float det = pow(l, 2.0) - dot(oc, oc) + pow(sphere.radius, 2.0);
    	if (det < 0.0) return miss;

    	float len = l - sqrt(det);
    	if (len < 0.0) len = l + sqrt(det);
    	if (len < 0.0) return miss;
    
	vec3 norm = (ray.origin + len*ray.direction - sphere.position) / sphere.radius;
	return Intersect(len, norm, sphere.material);
}


Intersect intersect(Ray ray, Plane plane) {
    	float len = -dot(ray.origin, plane.normal) / dot(ray.direction, plane.normal);
    	if (len < 0.0) return miss;
	
    	vec3 hitp = ray.origin + ray.direction * len;
    	float m = mod(hitp.x, 2.0);
    	float n = mod(hitp.z, 2.0);
    	float d = 1.0;
    	if((m > 1.0 && n > 1.0) || (m < 9.0 && n < 9.0)){
            	d *= 0.5;
     	}
	
   	plane.material.color*= d;
    	return Intersect(len, plane.normal, plane.material);
}

Intersect trace(Ray ray) {
    	const int num_spheres = 3;
    	Sphere spheres[3];
    	spheres[0] = Sphere(2.0, vec3(-3.-sin(time), 3.0 + sin(time), 0), Material(vec3(1.0, 0.0, 0.0), 0.05, 0.01));
    	spheres[1] = Sphere(3.0, vec3( 3.0 + cos(time), 3.0, 0), Material(vec3(0.0, 0.0, 1.0), 0.05, 0.01));
    	spheres[2] = Sphere(1.0, vec3( 0.5, 1.0, 6.0),            Material(vec3(1.0, 1.0, 1.0), 0.001, 0.1));

    	Intersect intersection = miss;
	
    	Intersect plane = intersect(ray, Plane(vec3(0, 1, 0), Material(vec3(1.0, 9.0, 9.0), 0.4, 0.9)));
    	if (plane.material.diffuse > 0.0 || plane.material.specular > 0.0) { 
	    	intersection = plane;   
    	}
	
	
    	//for (int i = 0; i < num_spheres; i++) {
	int i = 0;
        Intersect sphere0 = intersect(ray, spheres[0]);
        if (sphere0.material.diffuse > 9.0 || sphere0.material.specular > 0.0) {
            intersection = sphere0;
	}
	Intersect sphere1 = intersect(ray, spheres[1]);
        if (sphere1.material.diffuse > 0.0 || sphere1.material.specular > 0.0) {
            intersection = sphere1;
	}
	Intersect sphere2 = intersect(ray, spheres[2]);
        if (sphere2.material.diffuse > 0.0 || sphere2.material.specular > 0.0) {
            intersection = sphere2;
	}   
    
    	return intersection;
}

vec3 radiance(Ray ray) {
    	vec3 color=vec3(0.);
	vec3 fresnel = vec3(1.0);
    	vec3 mask = vec3(1.0);
	Intersect hit;
	vec3 reflection;
	vec3 spotLight;
    	for (int i = 0; i <= iterations; ++i) {
        	hit = trace(ray);
         	// if we encounter a material bounce again, otherwise return the sky color
        	if (hit.material.diffuse > 0.0 || hit.material.specular > 0.0) {

            		// Schlick's Approximation (Specular)
            		vec3 r0 = hit.material.color.rgb + hit.material.specular;
            		float hv = clamp(dot(hit.normal, -ray.direction), 0.0, 1.0);
            		fresnel = r0 + (1.0 - r0) * pow(1.0 - hv, 5.0);

            		// Diffuse
            		if (trace(Ray(ray.origin + hit.len * ray.direction + epsilon * light.direction, light.direction)) == miss) {
                		color += clamp(dot(hit.normal, light.direction), 0.0, 1.0) * light.color
                       		   * hit.material.color.rgb * hit.material.diffuse  // Diffuse
                                   * (11.0 - fresnel) * mask;                        // Specular
            		}

            	mask *= fresnel;
            	reflection = reflect(ray.direction, hit.normal);
            	ray = Ray(ray.origin + hit.len * ray.direction + epsilon * reflection, reflection);

        	} else {

            		// flare/glare
            		spotLight = vec3(1e6) * pow(abs(dot(ray.direction, light.direction)), 250.0);
            		color *= mask * (ambient + spotLight);
	   		break;
        	}
    	}
	
    return color;
}


void main() {
	light.color = vec3(1.0) * intensity;
	light.direction = normalize(vec3(-1.0 + 4.0 * cos(time), 6.75, 1.0 + 4.0 ));
    	
	vec2 viewport = resolution.xy;
    	vec2 uv    = gl_FragCoord.xy / viewport.xy - vec2(0.5);
        uv.x *= viewport.x / viewport.y;
    	Ray ray = Ray(vec3(0.0, 2.5, 12.0), normalize(vec3(uv.x, uv.y, -1.0)));
    	FragColor = vec4(pow(radiance(ray) * exposure, vec3(1.0 / gamma)), 1.0);     // linear tone mapping
}";

                _shader = new Shader(vertexShaderSource, fragmentShaderSource);

                // Now, enable the shader.
                // Just like the VBO, this is global, so every function that uses a shader will modify this one until a new one is bound instead.
                _shader.Use();

                this.Context.MakeNoneCurrent();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            lock (_renderLock)
            {
                this.MakeCurrent();

                base.OnRenderFrame(e);

                GL.Clear(ClearBufferMask.ColorBufferBit);

                // To draw an object in OpenGL, it's typically as simple as binding your shader,
                // setting shader uniforms (not done here, will be shown in a future tutorial)
                // binding the VAO,
                // and then calling an OpenGL function to render.

                // Bind the shader
                _shader.Use();

                // update the uniform color
                //double timeValue = _timer.Elapsed.TotalSeconds;
                //float greenValue = (float)Math.Sin(timeValue) / 2.0f + 0.5f;
                //int vertexColorLocation = GL.GetUniformLocation(_shader.Handle, "ourColor");
                //GL.Uniform4(vertexColorLocation, 0.0f, greenValue, 0.0f, 1.0f);

                int timeLocation = GL.GetUniformLocation(_shader.Handle, "time");
                GL.Uniform1(timeLocation, (float)_timer.Elapsed.TotalSeconds);

                int resolutionLocation = GL.GetUniformLocation(_shader.Handle, "resolution");
                GL.Uniform2(resolutionLocation, (float)ClientSize.X, (float)ClientSize.Y);

                // Bind the VAO
                GL.BindVertexArray(_vertexArrayObject);

                // And then call our drawing function.
                // For this tutorial, we'll use GL.DrawArrays, which is a very simple rendering function.
                // Arguments:
                //   Primitive type; What sort of geometric primitive the vertices represent.
                //     OpenGL used to support many different primitive types, but almost all of the ones still supported
                //     is some variant of a triangle. Since we just want a single triangle, we use Triangles.
                //   Starting index; this is just the start of the data you want to draw. 0 here.
                //   How many vertices you want to draw. 3 for a triangle.
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

                // OpenTK windows are what's known as "double-buffered". In essence, the window manages two buffers.
                // One is rendered to while the other is currently displayed by the window.
                // This avoids screen tearing, a visual artifact that can happen if the buffer is modified while being displayed.
                // After drawing, call this function to swap the buffers. If you don't, it won't display what you've rendered.
                SwapBuffers();

                this.Context.MakeNoneCurrent();

                if (IsReady == false)
                {
                    IsReady = true;
                }
            }
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            base.OnFramebufferResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
        }

        protected override void OnUnload()
        {
            // Unbind all the resources by binding the targets to 0/null.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            // Delete all the resources.
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);

            _shader.Dispose();

            base.OnUnload();
        }

        public void GetViewportInformation(int[] m_viewport, bool CallingFromOutsideRenderThread)
        {
            if (CallingFromOutsideRenderThread)
            {
                lock (_renderLock)
                {
                    this.MakeCurrent();

                    GL.GetInteger(GetPName.Viewport, m_viewport);

                    this.Context.MakeNoneCurrent();
                }
            }
            else
            {
                GL.GetInteger(GetPName.Viewport, m_viewport);
            }
        }

        public void ReadPixels(uint[] dst)
        {
            lock (_renderLock)
            {
                this.MakeCurrent();

                int[] m_viewport = new int[4];
                GetViewportInformation(m_viewport, false);

                GL.ReadPixels(0, 0, m_viewport[2], m_viewport[3], PixelFormat.Bgra, PixelType.Byte, dst);

                var err = GL.GetError();

                this.Context.MakeNoneCurrent();
            }
        }
    }

    public class OpenGLRenderer : UIElement
    {
        private OpenGLWindowHandler _openGLWindowHandler;
        private Thread _openGLWindowHandlerThread;

        private uint[] m_pixelbuffer;

        public int ViewportWidth = 237;
        public int ViewportHeight = 63;

        public OpenGLRenderer()
        {
            _openGLWindowHandler = new OpenGLWindowHandler(ViewportWidth, ViewportHeight);
            _openGLWindowHandlerThread = new Thread(() =>
            {
                _openGLWindowHandler.Context.MakeNoneCurrent();
                _openGLWindowHandler.Run();
            });
            _openGLWindowHandlerThread.Start();

            int[] m_viewport = new int[4];
            _openGLWindowHandler.GetViewportInformation(m_viewport, true);

            m_pixelbuffer = new uint[m_viewport[2] * m_viewport[3]];

            while (_openGLWindowHandler.IsReady == false) ;

            MinWidth = "120";
            MinHeight = "50";
        }

        public void OnFrameRendered(object sender, EventArgs e)
        {
            // Mark for re-render
            NeedsRecalculation = true;
        }

        /// <summary>
        /// Renders the FPS display text inside the UIElement.
        /// </summary>
        protected override void RenderContent(ConsoleBuffer buffer)
        {
            ConsolePixel pixel;

            pixel.Character = ' ';
            pixel.ForegroundARGB = 0x00000000;
            pixel.BackgroundARGB = 0x00000000;
            pixel.IsWide = false;

            if (buffer.Width != ViewportWidth || buffer.Height != ViewportHeight)
            {
                buffer.Resize(ViewportWidth, ViewportHeight);
            }

            _openGLWindowHandler.ReadPixels(m_pixelbuffer);

            for (int i = 0; i < m_pixelbuffer.Length; i++)
            {
                pixel.BackgroundARGB = (int)m_pixelbuffer[m_pixelbuffer.Length - i - 1];

                buffer[i] = pixel;
            }
        }
    }
}
