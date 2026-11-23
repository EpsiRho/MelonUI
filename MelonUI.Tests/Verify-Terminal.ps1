param([string]$Configuration = "Debug", [string]$ResultPath)
$ErrorActionPreference = 'Stop'
$Executable = Join-Path $PSScriptRoot "..\MelonUIDemo\bin\$Configuration\net10.0\MelonUIDemo.exe"
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class SprintTerminalState {
    [DllImport("kernel32.dll")] public static extern IntPtr GetStdHandle(int id);
    [DllImport("kernel32.dll")] public static extern bool GetConsoleMode(IntPtr handle, out uint mode);
    [DllImport("kernel32.dll")] public static extern uint GetConsoleCP();
    [DllImport("kernel32.dll")] public static extern uint GetConsoleOutputCP();
}
'@
function Read-TerminalState {
    [uint32]$consoleMode = 0
    if (![SprintTerminalState]::GetConsoleMode([SprintTerminalState]::GetStdHandle(-11), [ref]$consoleMode)) { throw 'No interactive Windows output handle.' }
    [ordered]@{
        Mode = $consoleMode
        InputCodePage = [SprintTerminalState]::GetConsoleCP()
        OutputCodePage = [SprintTerminalState]::GetConsoleOutputCP()
        CursorVisible = [Console]::CursorVisible
        Title = [Console]::Title
    }
}
$beforeState = Read-TerminalState
& $Executable --smoke
$demoExit = $LASTEXITCODE
$afterState = Read-TerminalState
$matches = ($beforeState | ConvertTo-Json -Compress) -eq ($afterState | ConvertTo-Json -Compress)
$result = [ordered]@{ DemoExit = $demoExit; StateRestored = $matches; Before = $beforeState; After = $afterState } | ConvertTo-Json -Depth 4
if ($ResultPath) { $result | Set-Content -LiteralPath $ResultPath } else { Write-Output $result }
if ($demoExit -ne 0 -or !$matches) { throw "Demo failed or terminal state changed: exit=$demoExit restored=$matches" }
Write-Output 'PASS: terminal demo, mode, input/output code pages, cursor visibility and title restored.'
