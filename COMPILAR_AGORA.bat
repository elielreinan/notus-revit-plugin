@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

cd /d "%~dp0"
set "ROOT=%CD%\"

echo =====================================================
echo  HVAC Calc Pro - Compilacao Revit 2025
echo =====================================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo ERRO: .NET SDK nao encontrado.
  echo Instale o .NET SDK 8 e execute novamente.
  pause
  exit /b 1
)

set "PLUGIN="
set "SETUP="

for /f "delims=" %%F in ('dir /b /s "%ROOT%NotusRevitPlugin.csproj" 2^>nul') do (
  set "PLUGIN=%%F"
  goto :foundPlugin
)
:foundPlugin

for /f "delims=" %%F in ('dir /b /s "%ROOT%NotusSetup.csproj" 2^>nul') do (
  set "SETUP=%%F"
  goto :foundSetup
)
:foundSetup

if not defined PLUGIN (
  echo ERRO: NotusRevitPlugin.csproj nao encontrado.
  echo Verifique se o ZIP foi extraido completo.
  pause
  exit /b 1
)

if not defined SETUP (
  echo ERRO: NotusSetup.csproj nao encontrado.
  echo Verifique se o ZIP foi extraido completo.
  pause
  exit /b 1
)

if not exist "!PLUGIN!" (
  echo ERRO: caminho do projeto plugin invalido:
  echo !PLUGIN!
  pause
  exit /b 1
)

if not exist "!SETUP!" (
  echo ERRO: caminho do projeto instalador invalido:
  echo !SETUP!
  pause
  exit /b 1
)

echo Projeto plugin encontrado:
echo !PLUGIN!
echo.
echo Projeto instalador encontrado:
echo !SETUP!
echo.

echo Limpando pasta dist...
if exist "%ROOT%dist" rmdir /s /q "%ROOT%dist"
mkdir "%ROOT%dist"

set "PLUGIN_DIR="
for %%A in ("!PLUGIN!") do set "PLUGIN_DIR=%%~dpA"

echo.
echo [1/2] Compilando DLL do plugin...
dotnet build "!PLUGIN!" -c Release
if errorlevel 1 (
  echo.
  echo ERRO: Falha ao compilar a DLL do plugin.
  pause
  exit /b 1
)

echo.
echo Copiando pacote manual para dist\manual_install...
mkdir "%ROOT%dist\manual_install" >nul 2>&1
copy "!PLUGIN_DIR!bin\Release\net8.0-windows\NotusRevitPlugin.dll" "%ROOT%dist\manual_install\" >nul
if exist "!PLUGIN_DIR!Notus_SharedParameters.txt" copy "!PLUGIN_DIR!Notus_SharedParameters.txt" "%ROOT%dist\manual_install\" >nul
if exist "!PLUGIN_DIR!Assets" xcopy "!PLUGIN_DIR!Assets" "%ROOT%dist\manual_install\Assets\" /E /I /Y >nul

(
  echo ^<?xml version="1.0" encoding="utf-8"?^>
  echo ^<RevitAddIns^>
  echo   ^<AddIn Type="Application"^>
  echo     ^<Name^>Notus^</Name^>
  echo     ^<Assembly^>C:\ProgramData\Autodesk\Revit\Addins\2025\NotusRevitPlugin.dll^</Assembly^>
  echo     ^<AddInId^>8D83E0A4-8D57-4C8D-B3F2-AC9D8E123456^</AddInId^>
  echo     ^<FullClassName^>NotusRevitPlugin.App^</FullClassName^>
  echo     ^<VendorId^>NORLAX^</VendorId^>
  echo     ^<VendorDescription^>HVAC Calc Pro - Oficial Revit 2025^</VendorDescription^>
  echo   ^</AddIn^>
  echo ^</RevitAddIns^>
) > "%ROOT%dist\manual_install\Notus.addin"

echo.
echo [2/2] Gerando instalador EXE...
dotnet publish "!SETUP!" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o "%ROOT%dist"
if errorlevel 1 (
  echo.
  echo AVISO: A DLL compilou, mas o instalador EXE falhou.
  echo Voce ainda pode instalar manualmente usando dist\manual_install.
  explorer "%ROOT%dist"
  pause
  exit /b 1
)

echo.
echo =====================================================
echo PRONTO.
echo Veja o instalador e a instalacao manual em:
echo %ROOT%dist
echo =====================================================
explorer "%ROOT%dist"
pause

