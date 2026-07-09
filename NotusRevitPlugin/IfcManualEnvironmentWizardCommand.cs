using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Models;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class IfcManualEnvironmentWizardCommand : IExternalCommand
    {
        private const int MaxAutoCandidates = 30;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                IfcEnvironmentService ifc = new IfcEnvironmentService();
                IfcDiagnosticReport report = ifc.BuildDiagnosticReport(doc, doc.ActiveView);
                List<Element> selected = GetSelectedCandidates(uidoc, ifc);
                List<Element> autoCandidates = GetAutoCandidates(doc, ifc);

                TaskDialog dialog = new TaskDialog(ProductInfo.FullName)
                {
                    MainInstruction = "Assistente IFC sem ambientes",
                    MainContent =
                        "Rooms/Spaces no modelo ativo: " + (report.Rooms + report.Spaces) + "\n" +
                        "Rooms/Spaces em vinculos: " + (report.LinkedRooms + report.LinkedSpaces) + "\n" +
                        "IfcSpaces encontrados: " + report.IfcSpaces + "\n" +
                        "Elementos selecionados compativeis: " + selected.Count + "\n" +
                        "Candidatos visiveis para ambiente manual: " + autoCandidates.Count + "\n\n" +
                        report.Conclusion + "\n\n" +
                        "Use este assistente quando o IFC veio sem IfcSpace/Room/Space. Os ambientes criados ficam no RVT ativo e podem ser editados antes do calculo.",
                    CommonButtons = TaskDialogCommonButtons.Cancel
                };

                if (selected.Count > 0)
                {
                    dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Criar a partir dos elementos selecionados", "Usa area em planta do elemento e pe-direito padrao quando necessario.");
                }
                if (autoCandidates.Count > 0)
                {
                    dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Criar a partir dos candidatos visiveis", "Cria ate " + MaxAutoCandidates + " ambientes manuais para pisos/lajes/elementos IFC provaveis.");
                }
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Somente mostrar orientacao", "Nao cria nada no modelo.");

                TaskDialogResult result = dialog.Show();
                if (result == TaskDialogResult.Cancel) return Result.Cancelled;

                if (result == TaskDialogResult.CommandLink3)
                {
                    ShowGuidance(report);
                    return Result.Succeeded;
                }

                List<Element> sources = result == TaskDialogResult.CommandLink1 ? selected : autoCandidates.Take(MaxAutoCandidates).ToList();
                if (sources.Count == 0)
                {
                    ShowGuidance(report);
                    return Result.Cancelled;
                }

                TaskDialog confirm = new TaskDialog(ProductInfo.FullName)
                {
                    MainInstruction = "Criar Ambientes Notus manuais?",
                    MainContent =
                        "Quantidade: " + sources.Count + "\n\n" +
                        "O Notus vai criar elementos Generic Model/DirectShape no modelo ativo com area estimada pela caixa do elemento. Revise nome, area, pe-direito e ocupacao antes do calculo final.",
                    CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel
                };
                if (confirm.Show() != TaskDialogResult.Ok) return Result.Cancelled;

                int created = 0;
                using (Transaction transaction = new Transaction(doc, "Notus - Criar ambientes IFC manuais"))
                {
                    transaction.Start();
                    HvacParameterService.EnsureParameters(doc, uiapp.Application);
                    int index = 1;
                    foreach (Element source in sources)
                    {
                        ManualEnvironmentData data = ManualEnvironmentElementService.BuildDefaultFromElement(doc, source);
                        NormalizeManualData(data, source, index);
                        ManualEnvironmentElementService.CreateManualElement(doc, data, source);
                        created++;
                        index++;
                    }
                    transaction.Commit();
                }

                TaskDialog.Show(ProductInfo.FullName,
                    "Ambientes Notus criados: " + created + "\n\n" +
                    "Proximo passo: use Editar Ambiente Manual nos casos duvidosos e depois Calcular HVAC. Para IFC sem IfcSpace, essa revisao e parte normal do fluxo tecnico.");
                HvacLogService.Info("Assistente IFC Notus criou ambientes manuais: " + created);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro no Assistente IFC Notus", ex);
                message = ex.Message;
                TaskDialog.Show(ProductInfo.FullName, "Nao foi possivel executar o Assistente IFC.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        private static List<Element> GetSelectedCandidates(UIDocument uidoc, IfcEnvironmentService ifc)
        {
            List<Element> result = new List<Element>();
            foreach (ElementId id in uidoc.Selection.GetElementIds())
            {
                Element element = uidoc.Document.GetElement(id);
                if (IsManualCandidate(element, ifc)) result.Add(element);
            }
            return Distinct(result);
        }

        private static List<Element> GetAutoCandidates(Document doc, IfcEnvironmentService ifc)
        {
            return Distinct(ifc.GetBroadIfcCandidates(doc, doc.ActiveView)
                .Where(e => IsManualCandidate(e, ifc))
                .OrderByDescending(ifc.EstimateAreaM2FromBoundingBox)
                .Take(MaxAutoCandidates * 2))
                .Take(MaxAutoCandidates)
                .ToList();
        }

        private static bool IsManualCandidate(Element element, IfcEnvironmentService ifc)
        {
            if (element == null || element.Category == null) return false;
            double area = ifc.EstimateAreaM2FromBoundingBox(element);
            if (area < 3 || area > 1200) return false;

            try
            {
                long categoryId = element.Category.Id.Value;
                if (categoryId == (long)BuiltInCategory.OST_Floors) return true;
                if (categoryId == (long)BuiltInCategory.OST_Ceilings) return true;
                if (categoryId == (long)BuiltInCategory.OST_GenericModel) return true;
                if (element is DirectShape) return true;
                if (element is ImportInstance) return true;
            }
            catch { }

            return ifc.IsIfcRelated(element) || ifc.LooksLikeIfcSpace(element);
        }

        private static void NormalizeManualData(ManualEnvironmentData data, Element source, int index)
        {
            string sourceName = SafeName(source);
            data.Name = "Ambiente Notus " + index.ToString("00", CultureInfo.InvariantCulture) + " - " + sourceName;
            if (data.AreaM2 <= 0) data.AreaM2 = 20;
            if (data.HeightM < 2.20 || IsHorizontalHost(source)) data.HeightM = 2.80;
            data.VolumeM3 = data.AreaM2 * data.HeightM;

            EnvironmentTypeCriteria criteria = HvacCriteriaService.Get(data.EnvironmentType);
            if (data.Occupants <= 0 && criteria.M2PerPerson > 0)
            {
                data.Occupants = Math.Max(1, (int)Math.Ceiling(data.AreaM2 / criteria.M2PerPerson));
            }
            data.Observation = "Criado pelo Assistente IFC do Notus. Conferir limites, area, pe-direito, ocupacao e tipo de uso antes do calculo final.";
        }

        private static bool IsHorizontalHost(Element source)
        {
            try
            {
                if (source == null || source.Category == null) return false;
                long categoryId = source.Category.Id.Value;
                return categoryId == (long)BuiltInCategory.OST_Floors || categoryId == (long)BuiltInCategory.OST_Ceilings || categoryId == (long)BuiltInCategory.OST_Roofs;
            }
            catch { return false; }
        }

        private static List<Element> Distinct(IEnumerable<Element> elements)
        {
            return elements
                .Where(e => e != null)
                .GroupBy(e => !string.IsNullOrWhiteSpace(e.UniqueId) ? e.UniqueId : e.Id.Value.ToString(CultureInfo.InvariantCulture))
                .Select(g => g.First())
                .ToList();
        }

        private static void ShowGuidance(IfcDiagnosticReport report)
        {
            TaskDialog.Show(ProductInfo.FullName,
                "Para automatizar melhor o IFC, exporte o modelo com zonas/ambientes como IfcSpace.\n\n" +
                "Quando isso nao for possivel, selecione pisos/lajes/volumes que representem ambientes e rode o Assistente IFC novamente. Depois revise cada Ambiente Notus manual antes de calcular.\n\n" +
                "Resumo: Rooms " + report.Rooms + ", Spaces " + report.Spaces + ", IfcSpaces " + report.IfcSpaces + ", Links " + report.RevitLinks + ".");
        }

        private static string SafeName(Element element)
        {
            try
            {
                if (element == null) return "Origem";
                string name = element.Name;
                if (string.IsNullOrWhiteSpace(name)) name = element.Category != null ? element.Category.Name : "Origem";
                return name.Length > 48 ? name.Substring(0, 48) : name;
            }
            catch { return "Origem"; }
        }
    }
}
