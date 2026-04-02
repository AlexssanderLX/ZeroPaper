param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "tools\ZeroPaper.PrintAgent\ZeroPaper.PrintAgent.csproj"
$publishDir = Join-Path $root "tools\ZeroPaper.PrintAgent\publish-$Runtime"
$downloadsDir = Join-Path $root "frontend\public\downloads"
$targetExe = Join-Path $downloadsDir "zeropaper-print-agent-$Runtime.exe"

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Path $downloadsDir -Force | Out-Null

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

Copy-Item -LiteralPath (Join-Path $publishDir "ZeroPaper.PrintAgent.exe") -Destination $targetExe -Force

Write-Output "Agente gerado em: $targetExe"
