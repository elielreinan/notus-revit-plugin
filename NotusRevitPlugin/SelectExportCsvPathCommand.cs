using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class SelectExportCsvPathCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                string defaultName = MakeSafeFileName(doc.Title) + "_Notus_Revit.csv";

                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = "Salvar CSV de exportação Notus",
                    Filter = "Arquivo CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
                    FileName = defaultName,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    AddExtension = true,
                    DefaultExt = ".csv",
                    OverwritePrompt = true
                };

                bool? result = dialog.ShowDialog();
                if (result != true)
                {
                    return Result.Cancelled;
                }

                App.SetExportCsvPath(dialog.FileName);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível selecionar o CSV de exportação.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        private string MakeSafeFileName(string text)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(c, '_');
            }
            return text;
        }
    }
}


