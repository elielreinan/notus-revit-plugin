@echo off
chcp 65001 >nul
setlocal

set "ROOT=%~dp0"
set "PLUGIN=%ROOT%NotusRevitPlugin\NotusRevitPlugin.csproj"
set "SETUP=%ROOT%NotusSetup\NotusSetup.csproj"
set "DIST=%ROOT%dist"
set "PLUGIN_OUT=%ROOT%NotusRevitPlugin\bin\Release\net8.0-windows"
set "PUBLISH_OUT=%ROOT%NotusSetup\bin\Release\net8.0-windows\win-x64\publish"
set "INSTALLER_NAME=Instalador_Notus_Revit2025.exe"


echo =====================================================
echo Notus - Build Revit 2025
echo =====================================================
echo.

if not exist "%PLUGIN%" (
    echo ERRO: Projeto do plugin nao encontrado: %PLUGIN%
    exit /b 1
)

if not exist "%SETUP%" (
    echo ERRO: Projeto do instalador nao encontrado: %SETUP%
    exit /b 1
)

echo Limpando dist anterior...
rmdir /s /q "%DIST%" 2>nul
mkdir "%DIST%" >nul
mkdir "%DIST%\manual_install" >nul
mkdir "%DIST%\manual_install\Assets" >nul

echo Compilando plugin Revit 2025...
dotnet build "%PLUGIN%" -c Release
if errorlevel 1 exit /b 1

echo.
echo Publicando instalador profissional standalone...
dotnet publish "%SETUP%" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true
if errorlevel 1 exit /b 1

if not exist "%PUBLISH_OUT%\%INSTALLER_NAME%" (
    echo ERRO: Instalador nao encontrado em:
    echo %PUBLISH_OUT%\%INSTALLER_NAME%
    echo Confira o AssemblyName em NotusSetup.csproj.
    exit /b 1
)

copy /Y "%PUBLISH_OUT%\%INSTALLER_NAME%" "%DIST%\%INSTALLER_NAME%" >nul
copy /Y "%PLUGIN_OUT%\NotusRevitPlugin.dll" "%DIST%\manual_install\NotusRevitPlugin.dll" >nul
copy /Y "%ROOT%NotusRevitPlugin\instalar_plugin_revit2025.bat" "%DIST%\manual_install\instalar_plugin_revit2025.bat" >nul
copy /Y "%ROOT%shared_parameters\Notus_SharedParameters.txt" "%DIST%\manual_install\Notus_SharedParameters.txt" >nul
xcopy /E /I /Y "%ROOT%NotusRevitPlugin\Assets" "%DIST%\manual_install\Assets" >nul
copy /Y "%ROOT%README_FINAL.txt" "%DIST%\README_FINAL.txt" >nul
copy /Y "%ROOT%README_FINAL.txt" "%DIST%\manual_install\README_FINAL.txt" >nul
copy /Y "%ROOT%notus_logo.ico" "%DIST%\notus_logo.ico" >nul

(
echo ^<?xml version="1.0" encoding="utf-8"?^>
echo ^<RevitAddIns^>
echo   ^<AddIn Type="Application"^>
echo     ^<Name^>Notus^</Name^>
echo     ^<Assembly^>%%APPDATA%%\Autodesk\Revit\Addins\2025\NotusRevitPlugin.dll^</Assembly^>
echo     ^<AddInId^>8D83E0A4-8D57-4C8D-B3F2-AC9D8E123456^</AddInId^>
echo     ^<FullClassName^>NotusRevitPlugin.App^</FullClassName^>
echo     ^<VendorId^>NORLAX^</VendorId^>
echo     ^<VendorDescription^>Notus ^| Calculo HVAC nativo no Revit 2025^</VendorDescription^>
echo   ^</AddIn^>
echo ^</RevitAddIns^>
) > "%DIST%\manual_install\Notus.addin.example"

echo.
echo =====================================================
echo Build concluido.
echo Instalador profissional:
echo %DIST%\%INSTALLER_NAME%
echo.
echo Pacote manual limpo:
echo %DIST%\manual_install
echo =====================================================
echo.
endlocal


