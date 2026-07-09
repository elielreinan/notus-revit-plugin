using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Forms;
using NotusRevitPlugin.Models;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CreateManualEnvironmentCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                List<Element> selected = GetSelected(uidoc);

                Element source = selected.Count > 0 ? selected[0] : null;
                ManualEnvironmentData data = ManualEnvironmentElementService.BuildDefaultFromElement(doc, source);
                using (ManualEnvironmentForm form = new ManualEnvironmentForm(data))
                {
                    if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK) return Result.Cancelled;
                    data = form.Data;
                }

                int created = 0;
                using (Transaction t = new Transaction(doc, "Criar Ambiente HVAC Manual"))
                {
                    t.Start();
                    HvacParameterService.EnsureParameters(doc, uiapp.Application);
                    if (selected.Count == 0)
                    {
                        ManualEnvironmentElementService.CreateManualElement(doc, data, null);
                        created = 1;
                    }
                    else
                    {
                        int index = 1;
                        foreach (Element e in selected)
                        {
                            ManualEnvironmentData item = ManualEnvironmentElementService.BuildDefaultFromElement(doc, e);
                            item.Name = selected.Count == 1 ? data.Name : data.Name + " " + index;
                            item.Level = string.IsNullOrWhiteSpace(data.Level) ? item.Level : data.Level;
                            if (selected.Count == 1)
                            {
                                item.AreaM2 = data.AreaM2 > 0 ? data.AreaM2 : item.AreaM2;
                                item.HeightM = data.HeightM > 0 ? data.HeightM : item.HeightM;
                                item.VolumeM3 = data.VolumeM3 > 0 ? data.VolumeM3 : item.AreaM2 * item.HeightM;
                                item.Occupants = data.Occupants;
                            }
                            else
                            {
                                item.VolumeM3 = item.AreaM2 * item.HeightM;
                                item.Occupants = Math.Max(1, (int)Math.Ceiling(item.AreaM2 / Math.Max(1, HvacCriteriaService.Get(data.EnvironmentType).M2PerPerson)));
                            }
                            item.EnvironmentType = data.EnvironmentType;
                            item.InternalTempC = data.InternalTempC;
                            item.InternalRelativeHumidity = data.InternalRelativeHumidity;
                            item.RenewalAirM3h = data.RenewalAirM3h;
                            item.Observation = data.Observation;
                            ManualEnvironmentElementService.CreateManualElement(doc, item, e);
                            created++;
                            index++;
                        }
                    }
                    t.Commit();
                }

                TaskDialog.Show("Notus", "Ambiente(s) HVAC Manual salvo(s) no projeto: " + created + "\n\nAgora use Calcular HVAC. Os dados manuais ficam gravados no próprio RVT.");
                HvacLogService.Info("Ambientes manuais criados: " + created);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro ao criar Ambiente Manual", ex);
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível criar Ambiente HVAC Manual.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        private List<Element> GetSelected(UIDocument uidoc)
        {
            List<Element> result = new List<Element>();
            foreach (ElementId id in uidoc.Selection.GetElementIds())
            {
                Element e = uidoc.Document.GetElement(id);
                if (e != null && e.Category != null) result.Add(e);
            }
            return result;
        }
    }
}


