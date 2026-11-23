param([string]$Configuration = 'Release', [string]$ArtifactsDirectory, [switch]$CoreOnly)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$ArtifactsDirectory) { $ArtifactsDirectory = Join-Path $repoRoot 'artifacts\package-validation' }
$artifacts = [IO.Path]::GetFullPath($ArtifactsDirectory)
$feed = Join-Path $artifacts 'feed'
$packages = Join-Path $artifacts ('consumer-cache-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($feed) | Out-Null
if (!$IsWindows) { $CoreOnly = $true }
$projects = if ($CoreOnly) { @('MelonUI') } else { @('MelonUI', 'MelonUI.Shaders') }
foreach ($project in $projects) {
    & dotnet pack (Join-Path $repoRoot "$project\$project.csproj") -c $Configuration -o $feed --nologo -m:1 -nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "Packing $project failed." }
}
$nugetConfig = Join-Path $artifacts 'NuGet.Config'
$escapedFeed = [Security.SecurityElement]::Escape($feed)
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear/><add key="local" value="$escapedFeed"/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>
"@ | Set-Content -LiteralPath $nugetConfig
$consumer = Join-Path $repoRoot 'tests\PackageConsumer\PackageConsumer.csproj'
& dotnet restore $consumer --configfile $nugetConfig --packages $packages --disable-parallel --nologo
if ($LASTEXITCODE -ne 0) { throw 'Fresh package-consumer restore failed.' }
& dotnet build $consumer -c $Configuration --no-restore --nologo -m:1 -nr:false /p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Fresh app/plugin package-consumer build failed.' }
& dotnet run --project $consumer -c $Configuration --no-build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Package consumer failed.' }
$result = [ordered]@{ Configuration=$Configuration; CorePackage='MelonUI.1.0.0.nupkg'; ShaderPackage=$(if (!$CoreOnly) { 'MelonUI.Shaders.1.0.0.nupkg' } else { $null }); FreshConsumerCache=$packages; Passed=$true }
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifacts 'package-result.json')
