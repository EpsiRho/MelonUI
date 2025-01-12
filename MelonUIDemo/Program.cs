using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Testing;
using System.Drawing;
using MelonUI.Enums;
using System.Diagnostics;
using System.Collections.Concurrent;
using MelonUI.Base;
using MelonUIDemo.Backends;
using Pastel;
using MelonUI.Helpers;
using System.Threading;
using MelonUI.Shaders;


DemoWelcomeBackend.CWM = new ConsoleWindowManager();
DemoWelcomeBackend.CWM.EnableTitleBar = true;
DemoWelcomeBackend.CWM.SetTitle("[MelonUI V1.0b MXML Demo (1216122424)]");
DemoWelcomeBackend.CWM.RegisterKeyboardControl(ConsoleKey.F1, () =>
{
    DemoWelcomeBackend.CWM.UsePlatformSpecificRenderer = !DemoWelcomeBackend.CWM.UsePlatformSpecificRenderer;
}, "Toggle Renderer Type");

DemoWelcomeBackend.CWM.FrameRendered += framems;

void framems(object? sender, EventArgs e)
{
    var now = DateTime.Now;
    var dif = now - DemoWelcomeBackend.LastFrameTime;
    DemoWelcomeBackend.FrameTimes.Insert(0,dif.TotalMilliseconds);
    DemoWelcomeBackend.FrameTimes = DemoWelcomeBackend.FrameTimes.Take(100).ToList();
    double fuckyou = DemoWelcomeBackend.FrameTimes.Average();
    string bitch = fuckyou.ToString("0000.0000");
    DemoWelcomeBackend.FrameTimeDisplay = $"LFT: {bitch}ms";

    DemoWelcomeBackend.LastFrameTime = DateTime.Now;
}

TextBlock tb = new TextBlock()
{
    Text = "",
    X = "25%",
    Y = "25%",
    Width = "50%",
    Height = "50%",
};

//string wpxml = File.ReadAllText(@"C:\Users\jhset\Desktop\MelonUIogl\MelonUIDemo\Pages\DemoMenu.xml");

string wpxml = @"
<MUIPage Width=""99%""
		 Height=""90%""
		 Name=""DemoPage""
		 Assemblies=""MelonUI,MelonUIDemo""
		 ShowBorder=""false""
		 Namespaces=""MelonUI.Base,MelonUI.Default,MelonUI.Shaders,MelonUIDemo.Testing,MelonUIDemo.Backends""
		 Backends=""TestPageBackend,DemoWelcomeBackend"">


	<MusicPlayerElement
			   Width=""30%""
			   IsVisible=""false""
		   Y=""50%""
			   Height=""20%""/>
	<FPSCounter XYAlignment=""TopRight""
				Width=""9""
				Height=""4""
				OnFrameRendered=""{DemoWelcomeBackend.CWM.FrameRendered}""/>
	
	<OptionsMenu Name=""MUIDemoMenu""
				 X=""0""
				 Y=""0""
				 IsVisible=""false""
				 Width=""50%""
				 Height=""40%"">
		<OptionsMenu.Options>
			<MenuItem Option=""Open MXML Viewer""/>
			<MenuItem Option=""Show Line Demo"">
				<MenuItem.OnSelect MXMLFlags=""Show(DemoLine:false)""/>
			</MenuItem>
		</OptionsMenu.Options>

	</OptionsMenu>

	<TextBlock Text=""{DemoWelcomeBackend.RenderTypeDisplay}""
			   XYAlignment=""TopRight""
			   TextAlignment=""Centered""
			   Width=""15""
			   Height=""3""
			   Y=""5""/>

	<TextBlock Text=""{DemoWelcomeBackend.ConsoleSizeDisplay}""
			   XYAlignment=""TopRight""
			   TextAlignment=""Centered""
			   Width=""15""
			   Height=""3""
			   Y=""9""/>
	
	<TextBlock Text=""{DemoWelcomeBackend.FrameTimeDisplay}""
			   XYAlignment=""TopRight""
			   TextAlignment=""Centered""
			   Width=""35""
			   Height=""3""
			   Y=""13""/>

</MUIPage>
";

//string wpxml = File.ReadAllText(@"C:\Users\jhset\Desktop\test.xml");
var WelcomePage = new MUIPage();
var wpcompiled = WelcomePage.Compile(wpxml);

var item = new OpenGLShaderElement()
{
    Width = "100%",
    Height = "100%",
};

item.ShaderSource = @"
#version 330 core

#ifdef GL_ES
precision mediump float;
#endif

out vec4 FragColor;

uniform float time;
uniform vec2 resolution;

float PI  = 4.0*atan(1.0);

vec3 sunLight  = normalize( vec3(  0.35, 0.2,  0.3 ) );
vec3 cameraPos;
vec3 sunColour = vec3(1.0, .75, .6);
const mat2 rotate2D = mat2(1.932, 1.623, -1.623, 1.952);
float gTime = 0.0;

//--------------------------------------------------------------------------
// Noise functions...
float Hash( float n )
{
    return fract(sin(n)*43758.5453123);
}

//--------------------------------------------------------------------------
float Hash(vec2 p)
{
	return fract(sin(dot(p, vec2(2.9898, 78.233))) * 43758.5453);
}

//--------------------------------------------------------------------------
float Noise( in vec2 x )
{
    vec2 p = floor(x);
    vec2 f = fract(x);
    f = f*f*(3.0-2.0*f);
    float n = p.x + p.y*57.0;
    float res = mix(mix( Hash(n+  0.0), Hash(n+  1.0),f.x),
                    mix( Hash(n+ 57.0), Hash(n+ 58.0),f.x),f.y);
    return res;
}

vec2 Voronoi( in vec2 x )
{
	vec2 p = floor( x );
	vec2 f = fract( x );
	float res=100.0,id;
	for( int j=-1; j<=1; j++ )
	for( int i=-1; i<=1; i++ )
	{
		vec2 b = vec2( float(i), float(j) );
		vec2 r = vec2( b ) - f  + Hash( p + b );
		float d = dot(r,r);
		if( d < res )
		{
			res = d;
			id  = Hash(p+b);
		}			
    }
	return vec2(max(.4-sqrt(res), 0.0),id);
}


//--------------------------------------------------------------------------
vec2 Terrain( in vec2 p)
{
	float type = 0.0;
	vec2 pos = p*0.003;
	float w = 50.0;
	float f = .0;
	for (int i = 0; i < 3; i++)
	{
		f += Noise(pos) * w;
		w = w * 0.62;
		pos *= 2.5;
	}

	return vec2(f, type);
}

//--------------------------------------------------------------------------
vec2 Map(in vec3 p)
{
	vec2 h = Terrain(p.xz);
    return vec2(p.y - h.x, h.y);
}

//--------------------------------------------------------------------------
float FractalNoise(in vec2 xy)
{
	float w = .7;
	float f = 0.0;

	for (int i = 0; i < 3; i++)
	{
		f += Noise(xy) * w;
		w = w*0.6;
		xy = 2.0 * xy;
	}
	return f;
}

//--------------------------------------------------------------------------
// Grab all sky information for a given ray from camera
vec3 GetSky(in vec3 rd)
{
	float sunAmount = max( dot( rd, sunLight), 0.0 );
	float v = pow(1.0-max(rd.y,0.0),6.);
	vec3  sky = mix(vec3(.1, .2, .3), vec3(.32, .32, .32), v);
	sky = sky + sunColour * sunAmount * sunAmount * .25;
	sky = sky + sunColour * min(pow(sunAmount, 800.0)*1.5, .3);
	return clamp(sky, 0.0, 1.0);
}

//--------------------------------------------------------------------------
// Merge grass into the sky background for correct fog colouring...
vec3 ApplyFog( in vec3  rgb, in float dis, in vec3 dir)
{
	float fogAmount = clamp(dis*dis* 0.0000012, 0.0, 1.0);
	return mix( rgb, GetSky(dir), fogAmount );
}

//--------------------------------------------------------------------------
vec3 DE(vec3 p)
{
	float base = Terrain(p.xz).x - 1.9;
	float height = Noise(p.xz*2.0)*.75 + Noise(p.xz)*.35 + Noise(p.xz*.5)*.2;
	//p.y += height;
	float y = p.y - base-height;
	y = y*y;
	vec2 ret = Voronoi((p.xz*2.5+sin(y*4.0+p.zx*12.3)*.12+vec2(sin(time*2.3+1.5*p.z),sin(time*3.6+1.5*p.x))*y*.5));
	float f = ret.x * .6 + y * .58;
	return vec3( y - f*1.4, clamp(f * 1.5, 0.0, 1.0), ret.y);
}

//--------------------------------------------------------------------------
// eiffie's code for calculating the aperture size for a given distance...
float CircleOfConfusion(float t)
{
	return max(t * .04, (2.0 / resolution.y) * (1.0+t));
}

//--------------------------------------------------------------------------
float Linstep(float a, float b, float t)
{
	return clamp((t-a)/(b-a),0.,1.);
}

//--------------------------------------------------------------------------
vec3 GrassBlades(in vec3 rO, in vec3 rD, in vec3 mat, in float dist)
{
	float d = 0.0;
	float f;
	// Only calculate cCoC once is enough here...
	float rCoC = CircleOfConfusion(dist*.3);
	float alpha = 0.0;
	
	vec4 col = vec4(mat*0.15, 0.0);

	for (int i = 0; i < 15; i++)
	{
		if (col.w > .99) break;
		vec3 p = rO + rD * d;
		
		vec3 ret = DE(p);
		ret.x += .5 * rCoC;

		if (ret.x < rCoC)
		{
			alpha = (1.0 - col.y) * Linstep(-rCoC, rCoC, -ret.x);//calculate the mix like cloud density
			f = clamp(ret.y, 0.0, 1.0);
			// Mix material with white tips for grass...
			vec3 gra = mix(mat, vec3(.35, .35, min(pow(ret.z, 4.0)*35.0, .35)), pow(ret.y, 9.0)*.7) * ret.y;
			col += vec4(gra * alpha, alpha);
		}
		d += max(ret.x * .7, .1);
	}
	if(col.w < .2)
		col.xyz = vec3(0.1, .15, 0.05);
	return col.xyz;
}

//--------------------------------------------------------------------------
// Calculate sun light...
void DoLighting(inout vec3 mat, in vec3 pos, in vec3 normal, in vec3 eyeDir, in float dis)
{
	float h = dot(sunLight,normal);
	mat = mat * sunColour*(max(h, 0.0)+.2);
}

//--------------------------------------------------------------------------
vec3 TerrainColour(vec3 pos, vec3 dir,  vec3 normal, float dis, float type)
{
	vec3 mat;
	if (type == 0.0)
	{
		// Random colour...
		mat = mix(vec3(.0,.3,.0), vec3(.2,.3,.0), Noise(pos.xz*.025));
		// Random shadows...
		float t = FractalNoise(pos.xz * .1)+.5;
		// Do grass blade tracing...
		mat = GrassBlades(pos, dir, mat, dis) * t;
		DoLighting(mat, pos, normal,dir, dis);
	}
	mat = ApplyFog(mat, dis, dir);
	return mat;
}

//--------------------------------------------------------------------------
// Home in on the surface by dividing by two and split...
float BinarySubdivision(in vec3 rO, in vec3 rD, float t, float oldT)
{
	float halfwayT = 0.0;
	for (int n = 0; n < 5; n++)
	{
		halfwayT = (oldT + t ) * .5;
		if (Map(rO + halfwayT*rD).x < .05)
		{
			t = halfwayT;
		}else
		{
			oldT = halfwayT;
		}
	}
	return t;
}

//--------------------------------------------------------------------------
bool Scene(in vec3 rO, in vec3 rD, out float resT, out float type )
{
    float t = 5.;
	float oldT = 0.0;
	float delta = 0.;
	vec2 h = vec2(1.0, 1.0);
	bool hit = false;
	for( int j=0; j < 80; j++ )
	{
	    vec3 p = rO + t*rD;
		if (p.y < 105.0 && !hit)
		{
			h = Map(p); // ...Get this position's height mapping.
	
			// Are we inside, and close enough to fudge a hit?...
			if( h.x < 0.05)
			{
				// Yes! So home in on height map...
				resT = BinarySubdivision(rO, rD, t, oldT);
				type = h.y;
				hit = true;
			}else
			{
				// Delta ray advance - a fudge between the height returned
				// and the distance already travelled.
				// Compromise between speed and accuracy...
				delta = max(0.04, 0.35*h.x) + (t*0.04);
				oldT = t;
				t += delta;
			}
		}
	}

	return hit;
}

//--------------------------------------------------------------------------
vec3 CameraPath( float t )
{
	//t = time + t;
    vec2 p = vec2(200.0 * sin(3.54*t), 200.0 * cos(2.0*t) );
	return vec3(p.x+55.0,  12.0+sin(t*.3)*6.5, -94.0+p.y);
} 

//--------------------------------------------------------------------------
vec3 PostEffects(vec3 rgb, vec2 xy)
{
	// Gamma first...
	rgb = pow(rgb, vec3(0.45));
	
	// Then...
	#define CONTRAST 1.1
	#define SATURATION 1.3
	#define BRIGHTNESS 1.3
	rgb = mix(vec3(.5), mix(vec3(dot(vec3(.2125, .7154, .0721), rgb*BRIGHTNESS)), rgb*BRIGHTNESS, SATURATION), CONTRAST);
	// Vignette...
	rgb *= .4+0.5*pow(40.0*xy.x*xy.y*(1.0-xy.x)*(1.0-xy.y), 0.2 );	
	return rgb;
}

//--------------------------------------------------------------------------
void main(void)
{
	float m = (1./resolution.x)*300.0;
	float gTime = (time*5.0+m+2352.0)*.006;
    vec2 xy = gl_FragCoord.xy / resolution.xy;
	vec2 uv = (-1.0 + 2.0 * xy) * vec2(resolution.x/resolution.y,1.0);
	vec3 camTar;
	
	if (xy.y < .13 || xy.y >= .87)
	{
		// Top and bottom cine-crop - what a waste! :)
		FragColor=vec4(vec4(0.0));
		return;
	}

	#ifdef STEREO
	float isCyan = mod(gl_FragCoord.x + mod(gl_FragCoord.y,2.0),2.0);
	#endif

	cameraPos = CameraPath(gTime + 0.0);
	camTar	 = CameraPath(gTime + .009);
	cameraPos.y += Terrain(CameraPath(gTime + .009).xz).x;
	camTar.y = cameraPos.y;
	
	float roll = .4*sin(gTime+.5);
	vec3 cw = normalize(camTar-cameraPos);
	vec3 cp = vec3(sin(roll), cos(roll),0.0);
	vec3 cu = cross(cw,cp);
	vec3 cv = cross(cu,cw);
	vec3 dir = normalize(uv.x*cu + uv.y*cv + 1.3*cw);
	mat3 camMat = mat3(cu, cv, cw);

	#ifdef STEREO
	cameraPos += .85*cu*isCyan; // move camera to the right - the rd vector is still good
	#endif

	vec3 col;
	float distance;
	float type;
	if( !Scene(cameraPos, dir, distance, type) )
	{
		// Missed scene, now just get the sky...
		col = GetSky(dir);
	}
	else
	{
		// Get world coordinate of landscape...
		vec3 pos = cameraPos + distance * dir;
		// Get normal from sampling the high definition height map
		// Use the distance to sample larger gaps to help stop aliasing...
		vec2 p = vec2(0.1, 0.0);
		vec3 nor  	= vec3(0.0,		Terrain(pos.xz).x, 0.0);
		vec3 v2		= nor-vec3(p.x,	Terrain(pos.xz+p).x, 0.0);
		vec3 v3		= nor-vec3(0.0,	Terrain(pos.xz-p.yx).x, -p.x);
		nor = cross(v2, v3);
		nor = normalize(nor);

		// Get the colour using all available data...
		col = TerrainColour(pos, dir, nor, distance, type);
	}
	
	// bri is the brightness of sun at the centre of the camera direction.
	// Yeah, the lens flares is not exactly subtle, but it was good fun making it.
	float bri = dot(cw, sunLight)*.75;
	if (bri > 0.0)
	{
		vec2 sunPos = vec2( dot( sunLight, cu ), dot( sunLight, cv ) );
		vec2 uvT = uv-sunPos;
		uvT = uvT*(length(uvT));
		bri = pow(bri, 6.0)*.8;

		// glare = the red shifted blob...
		float glare1 = max(dot(normalize(vec3(dir.x, dir.y+.3, dir.z)),sunLight),0.0)*1.4;
		// glare2 is the yellow ring...
		float glare2 = max(1.0-length(uvT+sunPos*.5)*4.0, 0.0);
		uvT = mix (uvT, uv, -2.3);
		// glare3 is a purple splodge...
		float glare3 = max(1.0-length(uvT+sunPos*5.0)*1.2, 0.0);

		col += bri * vec3(1.0, .0, .0)  * pow(glare1, 12.5)*.05;
		col += bri * vec3(1.0, 1.0, 0.2) * pow(glare2, 2.0)*2.5;
		col += bri * sunColour * pow(glare3, 2.0)*3.0;
	}
	col = PostEffects(col, xy);	
	
	#ifdef STEREO	
	col *= vec3( isCyan, 1.0-isCyan, 1.0-isCyan );	
	#endif
	
	FragColor=vec4(col,1.0);
}

//--------------------------------------------------------------------------
";

WelcomePage.AddElement(item);
//DemoWelcomeBackend.CWM.AddElement(item);


//foreach(var m in WelcomePage.CompilerMessages)
//{
//    Console.WriteLine(m);
//}
//return;

// Compiler Test Code
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("[Compiler Output]");

var msg = WelcomePage.GetSimpleCompilerDisplay();
Console.WriteLine($"{msg}");

TestPageBackend.TestText = ParamParser.GetGradientString(TestPageBackend.TestText, new[] { Color.FromArgb(255,255,50,50), Color.FromArgb(255, 0, 100, 255) });


// Test Code
if (!wpcompiled)
{
    DemoWelcomeBackend.CWM.SetStatus("What have you done");
    int w = Console.WindowWidth;
    while (true)
    {
        if (w != Console.WindowWidth)
        {
            Console.Clear();
            Console.WriteLine($"[Compiler Output ({w})]");
            msg = WelcomePage.GetSimpleCompilerDisplay();
            Console.WriteLine($"{msg}");
            w = Console.WindowWidth;
        }
        Thread.Sleep(10);
    }

    return;
}
else
{
    DemoWelcomeBackend.CWM.SetStatus("MUIPage compiled.");
    DemoWelcomeBackend.CWM.AddElement(WelcomePage);

}

DemoWelcomeBackend.CWM.ManageConsole(DemoWelcomeBackend.CancelSource.Token);

DemoWelcomeBackend.CWM.StartDispatcherQueue(DemoWelcomeBackend.CancelSource.Token);

bool XLeft = false;
bool XRight = true;
bool YUp = false;
bool YDown = false;

int CurX1 = 90;
int CurY1 = 90;

int CurX2 = 10;
int CurY2 = 10;

float step = 0.0f;
                             // Red                          Orange                            Yellow                            Green                           Light Blue                        Blue                            Purple                            Pink                              Red
Color[] Line1Gradient = new[] { Color.FromArgb(255,255,0,0), Color.FromArgb(255, 255, 160, 0), Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 0, 255, 0), Color.FromArgb(255, 0, 190, 255), Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 137, 0, 255), Color.FromArgb(255, 255, 0, 205), Color.FromArgb(255, 255, 0, 0) };
                             // Cyan                         Pink                                Cyan
Color[] Line2Gradient = new[] { Color.FromArgb(255,0,255,255), Color.FromArgb(255, 255, 0, 200), Color.FromArgb(255, 0, 255, 255) };

DemoWelcomeBackend.CWM.RegisterKeyboardControl(ConsoleKey.F12, () =>
{
    var str = DemoWelcomeBackend.CWM.Screenshot(false);
    string dir = $"{DateTime.Now}.txt";
    var chs = Path.GetInvalidFileNameChars();
    foreach (var item in chs)
    {
        dir = dir.Replace(item, '-');
    }
    Directory.CreateDirectory("Screenshots");
    File.WriteAllText($"Screenshots\\{dir}", str);
}, "Screenshot");

while (true) 
{
    TestPageBackend.CurX1 = $"{CurX1}%";
    TestPageBackend.CurY1 = $"{CurY1}%";
    TestPageBackend.CurX2 = $"{CurX2}%";
    TestPageBackend.CurY2 = $"{CurY2}%";
    TestPageBackend.LineColor = ParamParser.GetGradientColor(Line1Gradient, step);
    TestPageBackend.AntiLineColor = ParamParser.GetGradientColor(Line2Gradient, 1.0f - step);
    step += 0.005f;
    if (step >= 1.0f)
    {
        step = 0.0f;
    }
    if (XRight)
    {
        CurX2++;
        CurX1--;
        if (CurX2 >= 85)
        {
            XRight = false;
            YDown = true;
        }
    }
    else if (XLeft)
    {
        CurX2--;
        CurX1++;
        if (CurX2 <= 10)
        {
            XLeft = false;
            YUp = true;
        }
    }
    else if (YUp)
    {
        CurY2--;
        CurY1++;
        if (CurY2 <= 10)
        {
            YUp = false;
            XRight = true;
        }
    }
    else if (YDown)
    {
        CurY2++;
        CurY1--;
        if (CurY2 >= 85)
        {
            YDown = false;
            XLeft = true;
        }
    }



    TestPageBackend.CurrentPosStr = $"Position A: ({TestPageBackend.CurX1},{TestPageBackend.CurY1})\nPosition B: ({TestPageBackend.CurX2},{TestPageBackend.CurY2})\nStep: {step}";
    Thread.Sleep(20);
}

return;

