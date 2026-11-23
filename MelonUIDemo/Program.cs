using MelonUIDemo;

// dotnet run --project MelonUIDemo             the page built in C#
// dotnet run --project MelonUIDemo -- --mxml   the same kind of page, written in MXML
// dotnet run --project MelonUIDemo -- --shader an OpenGL shader drawn in the terminal
switch (args.FirstOrDefault())
{
    case "--mxml": await MxmlDemo.RunAsync(); break;
    case "--shader": await ShaderDemo.RunAsync(); break;
    default: await CSharpDemo.RunAsync(); break;
}
