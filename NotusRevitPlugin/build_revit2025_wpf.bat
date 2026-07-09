@echo off
chcp 65001 >nul

echo ==========================================
echo Notus - Build Plugin Revit 2025 Ribbon ComboBox
echo ==========================================
echo.

if not exist "C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll" (
    echo ERRO: RevitAPI.dll do Revit 2025 nao encontrada.
    echo Instale o Revit 2025 ou ajuste o caminho no .csproj.
    pause
    exit /b 1
)

if not exist "C:\Program Files\Autodesk\Revit 2025\RevitAPIUI.dll" (
    echo ERRO: RevitAPIUI.dll do Revit 2025 nao encontrada.
    echo Instale o Revit 2025 ou ajuste o caminho no .csproj.
    pause
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERRO: .NET SDK nao encontrado.
    echo Instale o .NET 8 SDK ou .NET 10 SDK e tente novamente.
    pause
    exit /b 1
)

rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul

dotnet build NotusRevitPlugin.csproj -c Release

if errorlevel 1 (
    echo.
    echo ERRO: Falha ao compilar o plugin.
    pause
    exit /b 1
)

echo.
echo Build concluido com sucesso.
echo DLL gerada em:
echo bin\Release\net8.0-windows\NotusRevitPlugin.dll
echo.
pause

