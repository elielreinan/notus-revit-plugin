using System;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class DetectIfcEnvironmentsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                IfcDiagnosticReport report = new IfcEnvironmentService().BuildDiagnosticReport(doc, doc.ActiveView);
                string text = report.ToDisplayText();

                string logPath = Path.Combine(HvacLogService.GetLogFolder(), "Notus_Diagnostico_IFC_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                File.WriteAllText(logPath, text);

                TaskDialog dialog = new TaskDialog("Notus - Diagnóstico IFC")
                {
                    MainInstruction = "Diagnóstico IFC avançado concluído",
                    MainContent = text.Length > 1800 ? text.Substring(0, 1800) + "\n\nRelatório completo salvo em:\n" + logPath : text + "\n\nRelatório salvo em:\n" + logPath,
                    CommonButtons = TaskDialogCommonButtons.Close
                };
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Abrir relatório completo", "Abre o arquivo TXT gerado com categorias, contagens e conclusão.");
                if (dialog.Show() == TaskDialogResult.CommandLink1)
                {
                    Process.Start(new ProcessStartInfo(logPath) { UseShellExecute = true });
                }
                HvacLogService.Info("Diagnóstico IFC gerado: " + logPath);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro no diagnóstico IFC", ex);
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível executar o diagnóstico IFC.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


