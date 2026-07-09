using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Forms;
using NotusRevitPlugin.Models;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class EditManualEnvironmentCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                Element selected = uidoc.Selection.GetElementIds().Select(id => doc.GetElement(id)).FirstOrDefault(ManualEnvironmentElementService.IsManualEnvironment);
                if (selected == null)
                {
                    TaskDialog.Show("Notus", "Selecione um Ambiente HVAC Manual já criado e execute novamente.\n\nDica: o ambiente manual fica como Generic Model/DirectShape com parâmetros Notus_Manual_.");
                    return Result.Cancelled;
                }

                ManualEnvironmentData data = ManualEnvironmentElementService.ReadData(selected);
                using (ManualEnvironmentForm form = new ManualEnvironmentForm(data))
                {
                    if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK) return Result.Cancelled;
                    data = form.Data;
                }

                using (Transaction t = new Transaction(doc, "Editar Ambiente HVAC Manual"))
                {
                    t.Start();
                    HvacParameterService.EnsureParameters(doc, commandData.Application.Application);
                    ManualEnvironmentElementService.UpdateManualElement(selected, data);
                    t.Commit();
                }

                TaskDialog.Show("Notus", "Ambiente HVAC Manual atualizado e salvo no projeto.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro ao editar Ambiente Manual", ex);
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível editar o Ambiente HVAC Manual.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


