@echo off
chcp 65001 >nul

echo ==========================================
echo Notus - Instalador Plugin Revit 2025
echo ==========================================
echo.

set "DLL_NAME=NotusRevitPlugin.dll"
set "ADDIN_NAME=Notus.addin"
set "REVIT_VERSION=2025"
set "ADDIN_DIR=%APPDATA%\Autodesk\Revit\Addins\%REVIT_VERSION%"
set "CURRENT_DIR=%~dp0"

echo Verificando arquivo DLL...

if not exist "%CURRENT_DIR%%DLL_NAME%" (
    echo.
    echo ERRO: Arquivo %DLL_NAME% nao encontrado.
    echo Coloque este instalador na mesma pasta da DLL.
    echo.
    pause
    exit /b 1
)

echo Criando pasta do Revit Addins...
if not exist "%ADDIN_DIR%" (
    mkdir "%ADDIN_DIR%"
)

echo Copiando DLL...
copy /Y "%CURRENT_DIR%%DLL_NAME%" "%ADDIN_DIR%\%DLL_NAME%" >nul

echo Copiando icones da Ribbon...
if exist "%CURRENT_DIR%Assets" (
    xcopy /E /I /Y "%CURRENT_DIR%Assets" "%ADDIN_DIR%\Assets" >nul
)

echo Copiando arquivo de parametros compartilhados...
if exist "%CURRENT_DIR%Notus_SharedParameters.txt" (
    copy /Y "%CURRENT_DIR%Notus_SharedParameters.txt" "%ADDIN_DIR%\Notus_SharedParameters.txt" >nul
)

echo Criando arquivo .addin...

(
echo ^<?xml version="1.0" encoding="utf-8"?^>
echo ^<RevitAddIns^>
echo   ^<AddIn Type="Application"^>
echo     ^<Name^>Notus^</Name^>
echo     ^<Assembly^>%ADDIN_DIR%\%DLL_NAME%^</Assembly^>
echo     ^<AddInId^>8D83E0A4-8D57-4C8D-B3F2-AC9D8E123456^</AddInId^>
echo     ^<FullClassName^>NotusRevitPlugin.App^</FullClassName^>
echo     ^<VendorId^>NORLAX^</VendorId^>
echo     ^<VendorDescription^>Notus</VendorDescription^>
echo   ^</AddIn^>
echo ^</RevitAddIns^>
) > "%ADDIN_DIR%\%ADDIN_NAME%"

echo.
echo ==========================================
echo Plugin instalado com sucesso!
echo ==========================================
echo.
echo Agora feche e abra o Revit 2025.
echo O plugin deve aparecer como Notus.
echo.
pause

