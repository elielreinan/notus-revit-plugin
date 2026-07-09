using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CreateParametersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            try
            {
                int count;
                using (Transaction transaction = new Transaction(doc, "Criar parâmetros Notus"))
                {
                    transaction.Start();
                    count = HvacParameterService.EnsureParameters(doc, uiapp.Application);
                    transaction.Commit();
                }

                TaskDialog.Show(
                    "Notus",
                    "Parâmetros criados/confirmados: " + count +
                    "\nRooms e Spaces estão prontos para receber cálculo HVAC direto no Revit.\n\n" +
                    "Parâmetros principais:\n" +
                    "- Notus_Total_BTU\n" +
                    "- Notus_TR\n" +
                    "- Notus_SHR\n" +
                    "- Notus_Supply_m3h\n" +
                    "- Notus_External_m3h\n" +
                    "- Notus_AirChanges_h");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível criar/confirmar os parâmetros Notus.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


