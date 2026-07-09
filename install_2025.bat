@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"

echo =====================================================
echo Notus - Instalar no Revit 2025
echo =====================================================
echo.

echo Se voce ja executou build_2025.bat, use preferencialmente:
echo   %ROOT%dist\Instalador_Notus_Revit2025.exe
echo.
echo Instalando pela rotina classica de arquivos locais...
echo.

pushd "%ROOT%NotusRevitPlugin"
call instalar_plugin_revit2025.bat
popd
endlocal

