# Third-party notices

MelonUI is MIT licensed. Its dependencies retain their own copyright notices and licenses.
The package IDs, versions and license declarations below are taken from restored NuGet metadata.
No ImageSharp, NAudio or ATL dependency is included.

| Dependency | Package use | Declared license | Upstream |
| --- | --- | --- | --- |
| Pastel 6.0.0 | Terminal core | MIT, packaged LICENSE reproduced below | https://github.com/silkfire/Pastel |
| Wcwidth 2.0.0 | Terminal core | MIT | https://github.com/spectreconsole/wcwidth |
| ComputeSharp / ComputeSharp.Core 3.1.0 | Optional shader package | MIT | https://github.com/Sergio0694/ComputeSharp |
| OpenTK.Graphics / OpenTK.Windowing.Desktop and required OpenTK components 4.9.3 | Optional shader package | MIT | https://github.com/opentk/opentk |
| OpenTK.redist.glfw 3.3.8.39 | Optional shader native runtime | zlib-style license; packaged COPYING.md reproduced below | https://www.glfw.org/license.html |
| Microsoft.CodeAnalysis.CSharp / Common 4.12.0 | Generator build and tests only | MIT | https://github.com/dotnet/roslyn |
| Microsoft.CodeAnalysis.Analyzers 3.11.0 | Build tooling only | MIT | https://github.com/dotnet/roslyn-analyzers |

The source generator is distributed as an analyzer; Roslyn is supplied by the consumer's SDK and
is not a terminal runtime dependency. NuGet retains each dependency's license metadata. Applications
redistributing native/dependency binaries should carry their associated upstream notices as well.
No third-party source is vendored into the terminal assembly.

## Pastel 6.0.0 LICENSE
MIT License

Copyright (c) 2018 Gabriel Bider

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## OpenTK.redist.glfw 3.3.8.39 COPYING.md

Copyright (c) 2002-2006 Marcus Geelnard

Copyright (c) 2006-2019 Camilla Löwy

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would
   be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not
   be misrepresented as being the original software.

3. This notice may not be removed or altered from any source
   distribution.


