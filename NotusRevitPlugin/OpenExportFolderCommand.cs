using System;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class OpenExportFolderCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                string folder = "";
                string exportPath = App.GetExportCsvPath();
                if (!string.IsNullOrWhiteSpace(exportPath))
                {
                    folder = Path.GetDirectoryName(exportPath) ?? "";
                }

                if (string.IsNullOrWhiteSpace(folder))
                {
                    folder = App.GetExportCsvFolder();
                }

                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    TaskDialog.Show("Notus", "Não foi possível localizar a pasta de exportação.");
                    return Result.Failed;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível abrir a pasta de exportação.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


