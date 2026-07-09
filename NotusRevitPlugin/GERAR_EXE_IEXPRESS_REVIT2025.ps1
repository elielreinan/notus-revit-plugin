$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Notus - Gerar EXE Revit 2025" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Observacao: o IExpress nao e usado nesta versao porque o EXE precisa receber o icone notus_logo.ico." -ForegroundColor Yellow
Write-Host "Sera usado o projeto .NET NotusSetup com ApplicationIcon." -ForegroundColor Yellow
Write-Host ""

$PluginRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = Split-Path -Parent $PluginRoot
$BuildScript = Join-Path $Root "build_2025.bat"
$ExePath = Join-Path $Root "dist\Instalador_Notus_Revit2025.exe"

if (!(Test-Path $BuildScript)) {
    throw "Script de build nao encontrado: $BuildScript"
}

& cmd /c "`"$BuildScript`""
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao gerar instalador. Codigo: $LASTEXITCODE"
}

if (Test-Path $ExePath) {
    Write-Host "" 
    Write-Host "EXE gerado com sucesso:" -ForegroundColor Green
    Write-Host $ExePath -ForegroundColor Green
} else {
    throw "Nao encontrei o EXE final: $ExePath"
}


