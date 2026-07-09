$ErrorActionPreference = "Stop"
$PluginRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = Split-Path -Parent $PluginRoot
$Package = Join-Path $PluginRoot "pacote_instalador_revit2025"
$BuildScript = Join-Path $Root "build_2025.bat"
$Exe = Join-Path $Root "dist\Instalador_Notus_Revit2025.exe"
$Dll = Join-Path $PluginRoot "bin\Release\net8.0-windows\NotusRevitPlugin.dll"

if (!(Test-Path $Exe) -or !(Test-Path $Dll)) {
    Write-Host "Build ainda nao encontrado. Compilando plugin e instalador..." -ForegroundColor Yellow
    & cmd /c "`"$BuildScript`""
    if ($LASTEXITCODE -ne 0) {
        throw "Falha no build. Codigo: $LASTEXITCODE"
    }
}

if (Test-Path $Package) {
    Remove-Item -Recurse -Force $Package
}
New-Item -ItemType Directory -Force $Package | Out-Null

Copy-Item $Exe (Join-Path $Package "Instalador_Notus_Revit2025.exe") -Force
Copy-Item $Dll (Join-Path $Package "NotusRevitPlugin.dll") -Force
Copy-Item (Join-Path $Root "notus_logo.ico") (Join-Path $Package "notus_logo.ico") -Force
Copy-Item (Join-Path $Root "README_FINAL.txt") (Join-Path $Package "README_FINAL.txt") -Force
Copy-Item (Join-Path $Root "shared_parameters\Notus_SharedParameters.txt") (Join-Path $Package "Notus_SharedParameters.txt") -Force
Copy-Item (Join-Path $PluginRoot "Assets") (Join-Path $Package "Assets") -Recurse -Force
Copy-Item (Join-Path $PluginRoot "instalar_plugin_revit2025.bat") (Join-Path $Package "instalar_plugin_revit2025.bat") -Force

Write-Host "Pacote criado em:" -ForegroundColor Green
Write-Host $Package
Write-Host ""
Write-Host "Use preferencialmente o EXE, pois ele ja recebe o icone da logo." -ForegroundColor Green


