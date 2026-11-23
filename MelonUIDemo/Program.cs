using MelonUIDemo;

if (args.Contains("--markup") || args.Contains("--mxml")) await MxmlControlsDemo.RunAsync(args.Contains("--smoke"));
else if (args.Contains("--controls")) await ControlsDemo.RunAsync(args.Contains("--smoke"));
else if (args.Contains("--shader")) await MxmlShaderDemo.RunAsync(args.Contains("--smoke"));
else await CSharpDemo.RunAsync(args.Contains("--smoke"));
