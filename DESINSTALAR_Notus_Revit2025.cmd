@echo off
chcp 65001 >nul
echo Removendo Notus Revit 2025...
set "ADDIN_DIR=%APPDATA%\Autodesk\Revit\Addins\2025"
del /f /q "%ADDIN_DIR%\Notus.addin" 2>nul
del /f /q "%ADDIN_DIR%\NotusRevitPlugin.dll" 2>nul
del /f /q "%ADDIN_DIR%\Notus_SharedParameters.txt" 2>nul
del /f /q "%ADDIN_DIR%\Notus_Uninstall.exe" 2>nul
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Notus_Revit2025" /f 2>nul
echo Plugin removido. Feche e abra o Revit 2025.
pause

