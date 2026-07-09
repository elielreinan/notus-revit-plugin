@echo off
chcp 65001 >nul
setlocal

set "PLUGIN_DIR=%~dp0"
set "ROOT=%PLUGIN_DIR%..\"

echo =====================================================
echo Notus - Gerar EXE com icone da logo
echo =====================================================
echo.
echo O instalador agora e gerado por um projeto .NET para permitir
echo configuracao de icone no EXE via notus_logo.ico.
echo.

call "%ROOT%build_2025.bat"
if errorlevel 1 exit /b 1

echo.
echo Instalador gerado em:
echo %ROOT%dist\Instalador_Notus_Revit2025.exe
echo.
pause
endlocal


