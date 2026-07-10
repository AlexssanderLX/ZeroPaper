param(
    [string]$Configuration = "Release",
    [string]$Runtime = "all",
    [switch]$SkipLegacy
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "tools\ZeroPaper.PrintAgent\ZeroPaper.PrintAgent.csproj"
$downloadsDir = Join-Path $root "frontend\public\downloads"

New-Item -ItemType Directory -Path $downloadsDir -Force | Out-Null

function Publish-AgentRuntime {
    param(
        [string]$RuntimeIdentifier
    )

    $publishDir = Join-Path $root "tools\ZeroPaper.PrintAgent\publish-$RuntimeIdentifier"
    $targetExe = Join-Path $downloadsDir "zeropaper-print-agent-$RuntimeIdentifier.exe"

    if (Test-Path $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }

    dotnet publish $project `
        -c $Configuration `
        -f net8.0-windows `
        -r $RuntimeIdentifier `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir

    Copy-Item -LiteralPath (Join-Path $publishDir "ZeroPaper.PrintAgent.exe") -Destination $targetExe -Force
    Write-Output "Agente gerado em: $targetExe"
}

function Publish-LegacyAgent {
    $publishDir = Join-Path $root "tools\ZeroPaper.PrintAgent\publish-legacy-net48"
    $targetZip = Join-Path $downloadsDir "zeropaper-print-agent-legacy-net48.zip"

    if (Test-Path $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }

    dotnet publish $project `
        -c $Configuration `
        -f net48 `
        -o $publishDir

    @(
        "ZeroPaper - agente de impressao legado",
        "",
        "Use esta versao quando o Windows mostrar que o executavel recomendado nao pode ser usado neste PC.",
        "Extraia o ZIP inteiro em uma pasta e abra ZeroPaper.PrintAgent.exe.",
        "Se o Windows pedir .NET Framework 4.8, instale esse componente da Microsoft e abra o agente novamente.",
        "Depois, cole a chave da unidade, escolha a impressora fisica e clique em Iniciar agente."
    ) | Set-Content -LiteralPath (Join-Path $publishDir "LEIA-ME-AGENTE-LEGADO.txt") -Encoding UTF8

    if (Test-Path $targetZip) {
        Remove-Item -LiteralPath $targetZip -Force
    }

    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $targetZip -Force
    Write-Output "Agente legado gerado em: $targetZip"
}

$runtimes = if ($Runtime -eq "all") {
    @("win-x86", "win-x64")
} else {
    @($Runtime)
}

foreach ($runtimeIdentifier in $runtimes) {
    Publish-AgentRuntime -RuntimeIdentifier $runtimeIdentifier
}

if (-not $SkipLegacy) {
    Publish-LegacyAgent
}
