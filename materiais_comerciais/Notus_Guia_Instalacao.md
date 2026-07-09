# Notus - Guia de instalacao

## Requisitos
- Autodesk Revit 2025 instalado.
- Windows 64 bits.
- Permissao para instalar add-ins no perfil do usuario.

## Instalacao pelo EXE
1. Feche o Revit.
2. Execute `Instalador_Notus_Revit2025.exe`.
3. Aguarde a conclusao.
4. Abra o Revit 2025.
5. A aba `Notus` deve aparecer na ribbon.

## Instalacao manual
1. Copie `NotusRevitPlugin.dll` para `%APPDATA%\Autodesk\Revit\Addins\2025`.
2. Copie a pasta `Assets` para a mesma pasta.
3. Use o exemplo `Notus.addin.example` como referencia.

## Desinstalacao
Use o atalho de desinstalacao criado no menu iniciar ou execute o instalador com `/uninstall`.

## Observacao
A DLL tecnica ainda se chama `NotusRevitPlugin.dll` para preservar compatibilidade. A marca exibida ao usuario e Notus.
