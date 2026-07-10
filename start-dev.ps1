$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendPath = Join-Path $root "backend"
$frontendPath = Join-Path $root "frontend"
$backendProject = Join-Path $backendPath "ZeroPaper.csproj"
$frontendUrl = "http://localhost:3000"
$backendUrl = "http://localhost:5097"

function Write-Step {
    param([string] $Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Test-Command {
    param([string] $Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Test-LocalPort {
    param([int] $Port)

    try {
        return [bool](Test-NetConnection -ComputerName "localhost" -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue)
    }
    catch {
        return $false
    }
}

Write-Host "ZeroPaper - ambiente local de desenvolvimento" -ForegroundColor Green
Write-Host "Raiz: $root"

if (-not (Test-Path -LiteralPath $backendProject)) {
    throw "Backend nao encontrado em: $backendProject"
}

if (-not (Test-Path -LiteralPath (Join-Path $frontendPath "package.json"))) {
    throw "Frontend nao encontrado em: $frontendPath"
}

if (-not (Test-Command "dotnet")) {
    throw "dotnet nao encontrado no PATH. Instale o .NET SDK antes de iniciar o backend."
}

if (-not (Test-Command "npm")) {
    throw "npm nao encontrado no PATH. Instale Node.js/npm antes de iniciar o frontend."
}

if (-not (Test-Path -LiteralPath (Join-Path $frontendPath "node_modules"))) {
    Write-Host ""
    Write-Host "Aviso: frontend\node_modules nao encontrado. Rode 'npm install' dentro da pasta frontend se o npm run dev falhar." -ForegroundColor Yellow
}

$mysqlServiceRunning = Get-Service -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match "mysql|mariadb" -or $_.DisplayName -match "mysql|mariadb" } |
    Where-Object { $_.Status -eq "Running" } |
    Select-Object -First 1

if (-not $mysqlServiceRunning -and -not (Test-LocalPort 3306)) {
    Write-Host ""
    Write-Host "Aviso: nao detectei MySQL local rodando. Verifique se o MySQL local esta rodando antes de usar o backend." -ForegroundColor Yellow
}

if (Test-LocalPort 5097) {
    Write-Host ""
    Write-Host "Aviso: a porta 5097 ja parece estar em uso. Se o backend falhar, feche o processo atual ou ajuste a porta local." -ForegroundColor Yellow
}

if (Test-LocalPort 3000) {
    Write-Host ""
    Write-Host "Aviso: a porta 3000 ja parece estar em uso. O Next.js pode usar outra porta automaticamente." -ForegroundColor Yellow
}

Write-Step "Abrindo backend em uma nova janela ($backendUrl)"
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location -LiteralPath '$backendPath'; `$env:ASPNETCORE_ENVIRONMENT='Development'; `$env:ASPNETCORE_URLS='http://0.0.0.0:5097'; `$env:PUBLIC_APP_BASE_URL='http://host.docker.internal:5097'; Write-Host 'ZeroPaper Backend - dotnet run' -ForegroundColor Green; dotnet run --project .\ZeroPaper.csproj --no-launch-profile --urls http://0.0.0.0:5097"
)

Write-Step "Abrindo frontend em uma nova janela ($frontendUrl)"
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location -LiteralPath '$frontendPath'; Write-Host 'ZeroPaper Frontend - npm run dev' -ForegroundColor Green; npm run dev"
)

Write-Step "Aguardando alguns segundos antes de abrir o navegador"
Start-Sleep -Seconds 7

Write-Step "Abrindo $frontendUrl"
Start-Process $frontendUrl

Write-Host ""
Write-Host "Pronto. As janelas do backend e frontend ficam abertas para mostrar os logs." -ForegroundColor Green
Write-Host "Backend esperado:  $backendUrl"
Write-Host "Frontend esperado: $frontendUrl"
Write-Host ""
Write-Host "Para encerrar, feche as janelas abertas ou pressione Ctrl+C dentro de cada uma."
