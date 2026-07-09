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
    public class SendDiagnosticsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                string folder = HvacLogService.GetLogFolder();
                string package = Path.Combine(folder, "diagnostico_notus_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;
                IfcDiagnosticReport ifc = new IfcEnvironmentService().BuildDiagnosticReport(doc, doc.ActiveView);

                File.WriteAllText(package,
                    "Notus - Diagnóstico\r\n" +
                    "Data: " + DateTime.Now + "\r\n" +
                    "Projeto: " + doc.Title + "\r\n" +
                    "Revit: " + uiapp.Application.VersionName + " | " + uiapp.Application.VersionNumber + "\r\n" +
                    "Plugin: Notus - Oficial\r\n" +
                    "PluginFolder: " + App.GetPluginFolder() + "\r\n" +
                    "Settings: " + HvacSettingsService.GetSettingsPath() + "\r\n" +
                    "Log: " + HvacLogService.GetLogPath() + "\r\n\r\n" +
                    "===== Diagnóstico IFC =====\r\n" + ifc.ToDisplayText() + "\r\n\r\n" +
                    "===== Log =====\r\n" +
                    (File.Exists(HvacLogService.GetLogPath()) ? File.ReadAllText(HvacLogService.GetLogPath()) : "Sem log."));
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
                TaskDialog.Show("Notus", "Pacote de diagnóstico gerado.\n\nEnvie este arquivo ao suporte:\n" + package);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}



