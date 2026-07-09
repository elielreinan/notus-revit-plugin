$ErrorActionPreference = "Stop"

Write-Host "=========================================="
Write-Host "Notus | Build final Revit 2025"
Write-Host "=========================================="

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Plugin = Join-Path $Root "NotusRevitPlugin\NotusRevitPlugin.csproj"
$Setup = Join-Path $Root "NotusSetup\NotusSetup.csproj"
$Output = Join-Path $Root "dist"
$Manual = Join-Path $Output "manual_install"
$PluginOut = Join-Path $Root "NotusRevitPlugin\bin\Release\net8.0-windows"
$InstallerName = "Instalador_Notus_Revit2025.exe"

Remove-Item -Recurse -Force $Output -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Output | Out-Null
New-Item -ItemType Directory -Force $Manual | Out-Null

Write-Host "Compilando plugin Revit 2025..."
dotnet build $Plugin -c Release

Write-Host "Gerando instalador profissional com assets embutidos..."
dotnet publish $Setup -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $Output

$Installer = Join-Path $Output $InstallerName
if (!(Test-Path $Installer)) {
    throw "Instalador esperado nao encontrado: $Installer"
}

Write-Host "Montando pasta dist/manual_install limpa..."
Copy-Item (Join-Path $PluginOut "NotusRevitPlugin.dll") (Join-Path $Manual "NotusRevitPlugin.dll") -Force
Copy-Item (Join-Path $Root "NotusRevitPlugin\instalar_plugin_revit2025.bat") (Join-Path $Manual "instalar_plugin_revit2025.bat") -Force
Copy-Item (Join-Path $Root "shared_parameters\Notus_SharedParameters.txt") (Join-Path $Manual "Notus_SharedParameters.txt") -Force
Copy-Item (Join-Path $Root "NotusRevitPlugin\Assets") (Join-Path $Manual "Assets") -Recurse -Force
Copy-Item (Join-Path $Root "README_FINAL.txt") (Join-Path $Output "README_FINAL.txt") -Force
Copy-Item (Join-Path $Root "README_FINAL.txt") (Join-Path $Manual "README_FINAL.txt") -Force
Copy-Item (Join-Path $Root "notus_logo.ico") (Join-Path $Output "notus_logo.ico") -Force
Copy-Item (Join-Path $Root "CHANGELOG.md") (Join-Path $Output "CHANGELOG.md") -Force
if (Test-Path (Join-Path $Root "materiais_comerciais")) {
    Copy-Item (Join-Path $Root "materiais_comerciais") (Join-Path $Output "materiais_comerciais") -Recurse -Force
}

$addinExample = @'
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Notus</Name>
    <Assembly>%APPDATA%\Autodesk\Revit\Addins\2025\NotusRevitPlugin.dll</Assembly>
    <AddInId>8D83E0A4-8D57-4C8D-B3F2-AC9D8E123456</AddInId>
    <FullClassName>NotusRevitPlugin.App</FullClassName>
    <VendorId>NORLAX</VendorId>
    <VendorDescription>Notus | Calculo HVAC inteligente para Revit 2025</VendorDescription>
  </AddIn>
</RevitAddIns>
'@
Set-Content -Path (Join-Path $Manual "Notus.addin.example") -Value $addinExample -Encoding UTF8

Write-Host ""
Write-Host "Concluido. Arquivos finais em:" -ForegroundColor Green
Write-Host $Output
Write-Host "Instalador: $Installer" -ForegroundColor Green
Write-Host ""


