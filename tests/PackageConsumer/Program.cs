using System;
using System.IO;
using System.Linq;
using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;

await using var window = new ConsoleWindowManager();
using var page = new MUIPage();
page.CompilerOptions.Assemblies.Add(typeof(PackagePlugin.Badge).Assembly);
page.CompilerOptions.Namespaces.Add("PackagePlugin");
if (!page.Compile("""
    <MUIPage MxmlVersion="1" ShowBorder="false">
      <MUIPage Name="Nested" ShowBorder="false"><Badge Name="Plugin" Caption="Packaged generator works" Height="1" ShowBorder="false"/></MUIPage>
      <TextBox Name="Secret" Text="secret value" HideCharacters="true" Y="2" Height="3"/>
    </MUIPage>
    """)) throw new Exception(page.GetSimpleCompilerDisplay());
window.Elements.Add(page);
string capture = window.CaptureText(width: 60, height: 10);
if (!capture.Contains("Packaged generator works") || capture.Contains("secret value")) throw new Exception("Package consumer composition/capture failed.");
var plugin = page.Find<PackagePlugin.Badge>("Plugin")!;
if (plugin.Caption != "Packaged generator works" || plugin.Parent is not MUIPage) throw new Exception("Plugin binding generator or ownership failed.");
var runtimeDependencies = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "PackageConsumer.deps.json"));
foreach (string forbidden in new[] { "OpenTK", "ComputeSharp", "Microsoft.CodeAnalysis", "MelonUIBindingGenerator", "NAudio", "ImageSharp" })
    if (runtimeDependencies.Contains(forbidden, StringComparison.OrdinalIgnoreCase)) throw new Exception($"Unexpected core runtime dependency: {forbidden}");
Console.WriteLine("PASS: clean NuGet app/plugin consumer; generated custom control, nested MXML, redacted capture and terminal-only dependencies.");
