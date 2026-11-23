# MelonUI 1.0 release and migration

The local package version is 1.0.0. The library uses SemVer: breaking public API or MXML language
changes require a major version; compatible features increment minor; compatible fixes increment patch.
`MxmlVersion="1"` selects the current markup contract. Packages are built locally by validation;
no NuGet publication, Git tag or GitHub release is performed by these scripts.

## Build and validate

```powershell
# Windows: full controls, demos, optional shaders and generator
dotnet build MelonUIDemo.sln -c Release --warnaserror
dotnet run --project MelonUI.Tests -c Release --no-build -- --gpu --soak
./scripts/Verify-Packages.ps1
dotnet run --no-launch-profile --project MelonUIDemo -- --mxml
```

On Linux/macOS, build `MelonUI.slnf` and run the portable tests with `--soak` (omit `--gpu`).
`Verify-Packages.ps1 -CoreOnly` packs the core and checks a fresh application/plugin consumer cache;
it selects core-only automatically outside Windows. The full shader source build needs Windows
because ComputeSharp 3.1's source generator invokes Windows compiler APIs. A prebuilt optional shader
assembly can use its OpenGL path where supported; Direct3D compute is Windows-only.

The core package contains its binding analyzer under `analyzers/dotnet/cs`, plus README, license and
dependency notices. Runtime dependencies are Pastel and Wcwidth. The optional MelonUI.Shaders package
contains OpenGL/Direct3D controls and their dependencies; it is unnecessary for ordinary console UIs.
The separate package-consumer sample verifies an external custom control in a plugin assembly,
generated properties, nested MXML, redacted capture and absence of graphics/Roslyn/audio runtime deps.

The validation workflow builds/tests Debug and Release and checks local packages on Windows, Linux
and macOS. Shader execution and native interactive terminal checks need a suitable real machine;
hosted CI does not establish GPU support or native terminal usability. Workflow upload artifacts are
local build products, not package-feed publishing. Dependency audit results should be refreshed before
any public release. See THIRD-PARTY-NOTICES.md for checked package metadata and native notices.

## OpenGL lifetime

Constructing/compiling an OpenGLShaderElement no longer creates a graphics context. After composition,
call its public `Init()` on the application main thread **before** awaiting `RunAsync`:

```csharp
window.Elements.Add(page);
page.Find<OpenGLShaderElement>("Shader")!.Init();
await window.RunAsync();
```

This keeps parse-only/headless validation independent of GLFW and honors its main-thread requirement.
The shader demo follows this flow. Missing/failed initialization appears as an error in the control;
`ErrorMessage` exposes the failure. Reinitialization after a failed attempt must also happen on the
application main thread. GPU rendering is paced at 60 updates/second, paused on detach and resumed on
reattach. Detach retains the reusable instance/native resources; final Dispose stops/joins the worker
and releases them. Shader compilation/link errors report through this path instead of Console.WriteLine.
Use `--shader --smoke` for actual frame startup, repeated resizing and shutdown. Compute display is lazy
and has its own Windows capability fallback. Neither is required by Melon's future console integration.

## Breaking changes from the unfinished 0.x API

| Previous behavior/API | 1.0 contract |
| --- | --- |
| Compile clears the live tree immediately | Draft validation; failure preserves the current page, success disposes replaced content |
| Nested elements become siblings / reversed order | Correct parent/Children ownership and declaration order |
| Attribute-order-dependent type/backend declarations | Assemblies, namespaces and backends resolved in phases |
| Parser helper methods / mutable Backends list | Compile/CompileFile, CompilerOptions, borrowed BackendObjects, CompiledBackends and GetBackend<T> |
| CompileFile-only source context | Compile(markup, sourcePath) also supports background-read reloads and relative includes |
| Flags overwrite callbacks / repeated flags collapse | Typed ordered actions; flags append after the existing callback, with target validation |
| Implicit UI objects inside callbacks | Explicit targets plus Show/Hide, or C# popup ownership |
| Private/field binding paths and silent failures | Public property/method/event paths, validated signatures/conversions and observable runtime errors |
| Unsupported [Binding] targets crash the generator | Field-only generation with actionable diagnostics and independent analyzer packaging |
| GPU libraries in the terminal package | Optional MelonUI.Shaders package |
| OpenGL context created while parsing | Explicit main-thread Init before RunAsync |
| Continuous identical idle output | Updating remains recurring; FrameRendered fires only after actual presentations |

Sprint-one/two lifecycle, focus, detach-versus-disposal, secret capture and audio/image removals remain
documented in csharp-lifecycle.md and default-controls.md. No old plugin compatibility layer is provided.
Higher-level application forms/autocomplete/navigation, Melon service contracts and console/server
separation are integration work, rather than new prerequisites for these library APIs.

## Practical limits

Supported runtime is .NET 10. The generator is netstandard2.0 and uses the consumer SDK's Roslyn.
The 1.0 MXML/plugin reflection contract is untrimmed/JIT; NativeAOT and arbitrary trimming are unsupported.
FilePicker enumerates local folders synchronously. Complex combining/ZWJ display shaping remains a
terminal/font/cell-buffer limitation; editing preserves text-element boundaries. Native macOS and
additional terminal families/SSH still need hands-on confirmation beyond portable build/test CI.
Reload soak checks are bounded evidence, not a multi-day resource certification. These limits should
travel with the release rather than being presented as capabilities that were never tested.
