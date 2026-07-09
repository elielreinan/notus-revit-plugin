using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace NotusRevitPlugin.Services
{
    public class IfcDiagnosticReport
    {
        public int Rooms { get; set; }
        public int Spaces { get; set; }
        public int IfcSpaces { get; set; }
        public int LinkedRooms { get; set; }
        public int LinkedSpaces { get; set; }
        public int LinkedIfcSpaces { get; set; }
        public int GenericModels { get; set; }
        public int DirectShapes { get; set; }
        public int ImportInstances { get; set; }
        public int RevitLinks { get; set; }
        public int UnreadableLinks { get; set; }
        public int ElementsWithIfcGuid { get; set; }
        public int VisibleInCurrentView { get; set; }
        public Dictionary<string, int> CategoryCounts { get; private set; } = new Dictionary<string, int>();
        public Dictionary<string, int> LevelCounts { get; private set; } = new Dictionary<string, int>();
        public List<string> Samples { get; private set; } = new List<string>();
        public string Conclusion { get; set; } = "";

        public int TotalRooms { get { return Rooms + LinkedRooms; } }
        public int TotalSpaces { get { return Spaces + LinkedSpaces; } }
        public int TotalIfcSpaces { get { return IfcSpaces; } }

        public string ToDisplayText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Rooms encontrados no modelo ativo: " + Rooms);
            sb.AppendLine("MEP Spaces encontrados no modelo ativo: " + Spaces);
            sb.AppendLine("IfcSpaces encontrados: " + IfcSpaces);
            sb.AppendLine("Rooms em vinculos carregados: " + LinkedRooms);
            sb.AppendLine("MEP Spaces em vinculos carregados: " + LinkedSpaces);
            sb.AppendLine("IfcSpaces em vinculos carregados: " + LinkedIfcSpaces);
            sb.AppendLine("Generic Models encontrados: " + GenericModels);
            sb.AppendLine("DirectShapes encontrados: " + DirectShapes);
            sb.AppendLine("ImportInstances encontrados: " + ImportInstances);
            sb.AppendLine("Revit Links encontrados: " + RevitLinks);
            sb.AppendLine("Links nao acessiveis/carregados: " + UnreadableLinks);
            sb.AppendLine("Elementos com IFC GUID/parametros IFC: " + ElementsWithIfcGuid);
            sb.AppendLine("Elementos IFC/arquitetura analisados na vista atual: " + VisibleInCurrentView);
            sb.AppendLine();
            sb.AppendLine("Categorias encontradas:");
            foreach (var item in CategoryCounts.OrderByDescending(kv => kv.Value).Take(20)) sb.AppendLine("- " + item.Key + ": " + item.Value);
            if (LevelCounts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Distribuicao por pavimento:");
                foreach (var item in LevelCounts.OrderByDescending(kv => kv.Value).Take(20)) sb.AppendLine("- " + item.Key + ": " + item.Value);
            }
            if (Samples.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Amostras IFC/arquitetura:");
                foreach (string sample in Samples.Take(10)) sb.AppendLine("- " + sample);
            }
            sb.AppendLine();
            sb.AppendLine("Conclusao:");
            sb.AppendLine(Conclusion);
            return sb.ToString();
        }
    }

    public class IfcEnvironmentService
    {
        private static readonly BuiltInCategory[] ArchitecturalCategories = new BuiltInCategory[]
        {
            BuiltInCategory.OST_GenericModel,
            BuiltInCategory.OST_Walls,
            BuiltInCategory.OST_Floors,
            BuiltInCategory.OST_Doors,
            BuiltInCategory.OST_Windows,
            BuiltInCategory.OST_Ceilings,
            BuiltInCategory.OST_Roofs,
            BuiltInCategory.OST_CurtainWallPanels,
            BuiltInCategory.OST_Furniture,
            BuiltInCategory.OST_SpecialityEquipment
        };

        public List<Document> GetLinkedDocuments(Document doc)
        {
            List<Document> result = new List<Document>();
            if (doc == null) return result;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RevitLinkInstance link in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
            {
                Document linkDoc = null;
                try { linkDoc = link.GetLinkDocument(); } catch { }
                if (linkDoc == null) continue;

                string key = GetDocumentKey(linkDoc);
                if (seen.Add(key)) result.Add(linkDoc);
            }
            return result;
        }

        public List<Element> GetIfcSpaceLikeElements(Document doc)
        {
            if (doc == null) return new List<Element>();
            return GetBroadIfcCandidates(doc, null).Where(LooksLikeIfcSpace).ToList();
        }

        public List<Element> GetBroadIfcCandidates(Document doc, View view)
        {
            List<Element> result = new List<Element>();
            if (doc == null) return result;

            IEnumerable<Element> elements;
            try
            {
                elements = view != null
                    ? new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType().ToElements()
                    : new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements();
            }
            catch
            {
                elements = new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements();
            }

            foreach (Element element in elements)
            {
                if (element == null || element.Category == null) continue;
                if (IsIfcRelated(element) || IsArchitecturalFallback(element)) result.Add(element);
            }
            return result;
        }

        public IfcDiagnosticReport BuildDiagnosticReport(Document doc, View currentView)
        {
            IfcDiagnosticReport report = new IfcDiagnosticReport();
            if (doc == null)
            {
                report.Conclusion = "Documento Revit indisponivel.";
                return report;
            }

            report.Rooms = CountCategory(doc, BuiltInCategory.OST_Rooms);
            report.Spaces = CountCategory(doc, BuiltInCategory.OST_MEPSpaces);
            report.GenericModels = CountCategory(doc, BuiltInCategory.OST_GenericModel);
            report.DirectShapes = CountClass(doc, typeof(DirectShape));
            report.ImportInstances = CountClass(doc, typeof(ImportInstance));
            report.RevitLinks = CountClass(doc, typeof(RevitLinkInstance));

            List<Element> broad = GetBroadIfcCandidates(doc, currentView);
            report.VisibleInCurrentView = broad.Count;
            AddElementsToReport(report, broad, "");

            foreach (RevitLinkInstance link in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
            {
                Document linkDoc = null;
                try { linkDoc = link.GetLinkDocument(); } catch { }
                if (linkDoc == null)
                {
                    report.UnreadableLinks++;
                    continue;
                }

                report.LinkedRooms += CountCategory(linkDoc, BuiltInCategory.OST_Rooms);
                report.LinkedSpaces += CountCategory(linkDoc, BuiltInCategory.OST_MEPSpaces);

                List<Element> linkBroad = GetBroadIfcCandidates(linkDoc, null).Take(3000).ToList();
                int before = report.IfcSpaces;
                AddElementsToReport(report, linkBroad, "Link: ");
                report.LinkedIfcSpaces += Math.Max(0, report.IfcSpaces - before);
            }

            int totalRooms = report.Rooms + report.LinkedRooms;
            int totalSpaces = report.Spaces + report.LinkedSpaces;
            if (totalRooms > 0 || totalSpaces > 0 || report.IfcSpaces > 0)
            {
                report.Conclusion = "Foram encontrados ambientes no modelo ativo e/ou em vinculos carregados. O calculo pode usar Rooms, MEP Spaces ou elementos IFC equivalentes; ambientes em vinculos sao somente leitura no modelo ativo.";
            }
            else if (report.RevitLinks > 0 && report.UnreadableLinks == report.RevitLinks)
            {
                report.Conclusion = "Ha vinculos no modelo, mas nenhum documento vinculado esta carregado/acessivel para leitura. Recarregue os links no Revit e tente novamente.";
            }
            else if (report.ElementsWithIfcGuid > 0 || report.DirectShapes > 0 || report.ImportInstances > 0 || report.CategoryCounts.Count > 0)
            {
                report.Conclusion = "Modelo IFC arquitetonico sem ambientes exportados como IfcSpace/Room/Space. Para calculo automatico, exporte zonas/ambientes como IfcSpace; como alternativa, selecione pisos/lajes/elementos e crie Ambientes HVAC Manuais.";
            }
            else
            {
                report.Conclusion = "Nenhum Room, Space, IfcSpace, DirectShape, ImportInstance ou categoria arquitetonica compativel foi identificado na vista/modelo atual.";
            }
            return report;
        }

        public bool LooksLikeIfcSpace(Element element)
        {
            if (element == null) return false;
            string haystack = BuildText(element).ToLowerInvariant();
            return haystack.Contains("ifcspace")
                || haystack.Contains("ifc space")
                || haystack.Contains("space/")
                || haystack.Contains("zone")
                || haystack.Contains("zona")
                || haystack.Contains("ambiente")
                || haystack.Contains("sala ")
                || haystack.Contains("room");
        }

        public bool HasIfcGuid(Element element)
        {
            return !string.IsNullOrWhiteSpace(GetParameterText(element, "IFC GUID", "IfcGUID", "IfcGuid", "GUID IFC", "GlobalId", "IFCGlobalId"));
        }

        public bool IsIfcRelated(Element element)
        {
            if (element == null) return false;
            if (element is ImportInstance || element is DirectShape) return true;
            string text = BuildText(element).ToLowerInvariant();
            if (text.Contains("ifc") || text.Contains("ifcguid") || text.Contains("globalid")) return true;
            return HasIfcGuid(element);
        }

        public double EstimateAreaM2FromBoundingBox(Element element)
        {
            try
            {
                BoundingBoxXYZ box = element.get_BoundingBox(null);
                if (box == null) return 0;
                double dx = Math.Abs(box.Max.X - box.Min.X);
                double dy = Math.Abs(box.Max.Y - box.Min.Y);
                return UnitUtils.ConvertFromInternalUnits(dx * dy, UnitTypeId.SquareMeters);
            }
            catch { return 0; }
        }

        public double EstimateHeightMFromBoundingBox(Element element)
        {
            try
            {
                BoundingBoxXYZ box = element.get_BoundingBox(null);
                if (box == null) return 2.8;
                double dz = Math.Abs(box.Max.Z - box.Min.Z);
                double height = UnitUtils.ConvertFromInternalUnits(dz, UnitTypeId.Meters);
                return height > 0.5 ? height : 2.8;
            }
            catch { return 2.8; }
        }

        public string GetLevelName(Element element)
        {
            try
            {
                if (element == null || element.Document == null) return "";
                Element level = element.Document.GetElement(element.LevelId);
                if (level != null) return level.Name;
                Parameter p = element.LookupParameter("Level") ?? element.LookupParameter("Nivel") ?? element.LookupParameter("Nível") ?? element.LookupParameter("Pavimento") ?? element.LookupParameter("Building Storey");
                if (p != null && p.HasValue) return p.StorageType == StorageType.String ? p.AsString() : p.AsValueString();
            }
            catch { }
            return "";
        }

        private void AddElementsToReport(IfcDiagnosticReport report, IEnumerable<Element> elements, string categoryPrefix)
        {
            foreach (Element element in elements)
            {
                string category = categoryPrefix + (element.Category != null ? element.Category.Name : "Sem categoria");
                Increment(report.CategoryCounts, category);
                string level = GetLevelName(element);
                if (!string.IsNullOrWhiteSpace(level)) Increment(report.LevelCounts, level);
                if (HasIfcGuid(element)) report.ElementsWithIfcGuid++;
                bool isIfcSpace = LooksLikeIfcSpace(element);
                if (isIfcSpace) report.IfcSpaces++;
                if (report.Samples.Count < 15 && (IsIfcRelated(element) || isIfcSpace))
                {
                    report.Samples.Add(category + " | Id " + element.Id.Value.ToString(CultureInfo.InvariantCulture) + " | " + SafeName(element));
                }
            }
        }

        private bool IsArchitecturalFallback(Element element)
        {
            try
            {
                long id = element.Category.Id.Value;
                return ArchitecturalCategories.Any(c => id == (long)c);
            }
            catch { return false; }
        }

        private string BuildText(Element element)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(element.Name).Append(' ');
            if (element.Category != null) sb.Append(element.Category.Name).Append(' ');
            sb.Append(GetParameterText(element, "IfcEntity", "IFC Entity", "Entity", "ObjectType", "Object Type", "IfcExportAs", "ExportAs", "IfcType", "PredefinedType", "Name", "Nome", "LongName", "Long Name", "Reference", "IFC GUID", "IfcGUID", "IfcGuid", "GlobalId", "Family", "Type", "Tipo"));
            try
            {
                Element type = element.Document.GetElement(element.GetTypeId());
                if (type != null) sb.Append(' ').Append(type.Name);
            }
            catch { }
            return sb.ToString();
        }

        private string GetParameterText(Element element, params string[] names)
        {
            if (element == null) return "";
            foreach (string name in names)
            {
                Parameter parameter = element.LookupParameter(name);
                if (parameter == null || !parameter.HasValue) continue;
                if (parameter.StorageType == StorageType.String) return parameter.AsString() ?? "";
                return parameter.AsValueString() ?? "";
            }
            return "";
        }

        private int CountCategory(Document doc, BuiltInCategory category)
        {
            try { return new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType().GetElementCount(); }
            catch { return 0; }
        }

        private int CountClass(Document doc, Type type)
        {
            try { return new FilteredElementCollector(doc).OfClass(type).WhereElementIsNotElementType().GetElementCount(); }
            catch { return 0; }
        }

        private void Increment(Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) key = "Nao identificado";
            if (!dict.ContainsKey(key)) dict[key] = 0;
            dict[key]++;
        }

        private string GetDocumentKey(Document doc)
        {
            if (doc == null) return "";
            try
            {
                if (!string.IsNullOrWhiteSpace(doc.PathName)) return doc.PathName;
            }
            catch { }
            try { return (doc.Title ?? "") + "#" + doc.GetHashCode().ToString(CultureInfo.InvariantCulture); }
            catch { return doc.GetHashCode().ToString(CultureInfo.InvariantCulture); }
        }

        private string SafeName(Element element)
        {
            try { return string.IsNullOrWhiteSpace(element.Name) ? element.Id.Value.ToString(CultureInfo.InvariantCulture) : element.Name; }
            catch { return ""; }
        }
    }
}

