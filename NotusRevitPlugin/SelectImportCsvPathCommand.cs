using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class SelectImportCsvPathCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Title = "Selecionar CSV calculado Notus",
                    Filter = "Arquivo CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Multiselect = false,
                    CheckFileExists = true
                };

                bool? result = dialog.ShowDialog();
                if (result != true)
                {
                    return Result.Cancelled;
                }

                App.SetImportCsvPath(dialog.FileName);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível selecionar o CSV de importação.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


