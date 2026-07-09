using System;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    // Comando acessível em Configurações > Mais Ações > Desinstalar plugin.
    // Não desinstala "por dentro" do próprio processo do Revit (o arquivo .dll do plugin está
    // carregado em memória e o Windows não deixa apagar um arquivo em uso) — em vez disso,
    // ele chama o desinstalador oficial (Notus_Uninstall.exe) que o instalador já copia
    // para a pasta do add-in, e orienta o usuário a fechar o Revit para concluir a remoção.
    [Transaction(TransactionMode.Manual)]
    public class UninstallPluginCommand : IExternalCommand
    {
        private const string UninstallerFileName = "Notus_Uninstall.exe";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                TaskDialog confirm = new TaskDialog("Notus")
                {
                    MainInstruction = "Desinstalar o Notus?",
                    MainContent =
                        "Isso vai remover o plugin, os ícones da ribbon e o registro do add-in no Revit 2025.\n\n" +
                        "Importante: como o Revit está com o plugin carregado agora, você precisa FECHAR o Revit " +
                        "logo depois de confirmar para que a remoção dos arquivos seja concluída. Se algum arquivo " +
                        "ficar bloqueado, é só abrir novamente 'Adicionar ou remover programas' > 'Notus - Oficial' " +
                        "e desinstalar de lá, ou rodar o desinstalador de novo.\n\n" +
                        "Seus parâmetros e resultados já gravados no projeto Revit (.rvt) NÃO são apagados.",
                    MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                    CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                    DefaultButton = TaskDialogResult.No
                };
                if (confirm.Show() != TaskDialogResult.Yes) return Result.Cancelled;

                string addinDir = Path.GetDirectoryName(typeof(UninstallPluginCommand).Assembly.Location);
                string uninstallerPath = string.IsNullOrWhiteSpace(addinDir) ? null : Path.Combine(addinDir, UninstallerFileName);

                if (string.IsNullOrWhiteSpace(uninstallerPath) || !File.Exists(uninstallerPath))
                {
                    TaskDialog.Show(
                        "Notus",
                        "Não encontrei o desinstalador (" + UninstallerFileName + ") na pasta do plugin.\n\n" +
                        "Isso costuma acontecer em instalações manuais antigas (sem o instalador oficial).\n\n" +
                        "Para remover, feche o Revit e:\n" +
                        "1. Abra 'Adicionar ou remover programas' no Windows e procure 'Notus - Oficial'; ou\n" +
                        "2. Delete manualmente a pasta:\n" + addinDir);
                    return Result.Cancelled;
                }

                Process.Start(new ProcessStartInfo(uninstallerPath, "/uninstall") { UseShellExecute = true });

                HvacLogService.Info("Desinstalação solicitada pelo usuário via menu Configurações.");

                TaskDialog.Show(
                    "Notus",
                    "O desinstalador foi aberto em outra janela.\n\n" +
                    "Feche o Revit agora para concluir a remoção dos arquivos do plugin.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro ao acionar desinstalação", ex);
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível iniciar a desinstalação.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


