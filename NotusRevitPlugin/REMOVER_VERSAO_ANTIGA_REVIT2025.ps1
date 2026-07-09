$ErrorActionPreference = "SilentlyContinue"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Notus - Remover versao antiga Revit 2025" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$AddinDir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2025"
$FilesToRemove = @(
    "Notus.addin",
    "NotusRevitPlugin.dll"
)

if (!(Test-Path $AddinDir)) {
    Write-Host "Pasta de Addins do Revit 2025 nao encontrada:" -ForegroundColor Yellow
    Write-Host $AddinDir
    Write-Host "Nada para remover."
    exit 0
}

foreach ($file in $FilesToRemove) {
    $path = Join-Path $AddinDir $file
    if (Test-Path $path) {
        Remove-Item $path -Force
        Write-Host "Removido: $path" -ForegroundColor Green
    }
}

$assets = Join-Path $AddinDir "Assets"
if (Test-Path $assets) {
    Remove-Item $assets -Recurse -Force
    Write-Host "Removido: $assets" -ForegroundColor Green
}

Write-Host ""
Write-Host "Versao antiga removida. Feche e abra o Revit 2025." -ForegroundColor Green

