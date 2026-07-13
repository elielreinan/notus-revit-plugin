using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Services
{
    public class RevitRoomDataService
    {
        private readonly IfcEnvironmentService _ifcService = new IfcEnvironmentService();
        private readonly ClimateService _climateService = new ClimateService();

        public List<Element> GetSelectedOrAllRoomsAndSpaces(UIDocument uidoc)
        {
            Document doc = uidoc.Document;
            List<Element> selected = new List<Element>();

            try
            {
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                foreach (ElementId id in selectedIds)
                {
                    Element element = doc.GetElement(id);
                    RevitLinkInstance link = element as RevitLinkInstance;
                    if (link != null)
                    {
                        Document linkDoc = null;
                        try { linkDoc = link.GetLinkDocument(); } catch { }
                        if (linkDoc != null) AddEnvironmentElements(linkDoc, selected);
                        continue;
                    }

                    if (IsSupportedEnvironmentElement(element) && HasUsableArea(element))
                    {
                        selected.Add(element);
                    }
                }
            }
            catch
            {
                selected.Clear();
            }

            if (selected.Count > 0)
            {
                return DistinctElements(selected);
            }

            List<Element> all = new List<Element>();
            AddEnvironmentElements(doc, all);

            foreach (Document linkDoc in _ifcService.GetLinkedDocuments(doc))
            {
                AddEnvironmentElements(linkDoc, all);
            }

            return DistinctElements(all);
        }

        public RoomInput BuildInput(Document hostDoc, Element element)
        {
            ClimateData climate = _climateService.GetClimateForProject(hostDoc);
            Document elementDoc = element.Document ?? hostDoc;
            bool isLinkedElement = hostDoc != null && elementDoc != null && !object.ReferenceEquals(elementDoc, hostDoc);
            string sourceDocumentTitle = GetDocumentTitle(elementDoc);

            string number = GetParameterText(element, "Notus_Room_Number", "Number", "Numero", "Número", "Notus_Manual_SourceElementId", "IfcName", "Tag");
            string name = GetParameterText(element, "Notus_Room_Name", "Notus_Manual_Name", "Name", "Nome", "LongName", "Long Name", "IfcLongName");

            Room room = element as Room;
            if (room != null)
            {
                if (string.IsNullOrWhiteSpace(number)) number = room.Number;
                if (string.IsNullOrWhiteSpace(name)) name = room.Name;
            }

            if (string.IsNullOrWhiteSpace(name)) name = element.Name ?? "Ambiente";
            if (string.IsNullOrWhiteSpace(number)) number = element.Id.Value.ToString(CultureInfo.InvariantCulture);

            Element level = null;
            try { level = elementDoc.GetElement(element.LevelId); } catch { }
            double areaM2 = GetAreaM2(element);
            bool ifcFallback = !IsRoomOrSpace(element);

            if (areaM2 <= 0 && ifcFallback)
            {
                areaM2 = _ifcService.EstimateAreaM2FromBoundingBox(element);
            }

            double heightM = GetHeightM(element);
            if (heightM <= 0 && ifcFallback)
            {
                heightM = _ifcService.EstimateHeightMFromBoundingBox(element);
            }

            double volumeM3 = GetVolumeM3(element);
            if (volumeM3 <= 0)
            {
                volumeM3 = areaM2 * (heightM > 0 ? heightM : 2.8);
            }

            double internalTemp = GetDoubleParameter(element, climate.InternalDesignTempC, "Notus_Manual_InternalTempC", "HVAC_InternalTempC", "InternalTemp", "Temperatura Interna", "Temp Interna");
            double externalTemp = GetDoubleParameter(element, climate.ExternalDryBulbC, "HVAC_ExternalTempC", "ExternalTemp", "Temperatura Externa", "Temp Externa");

            return new RoomInput
            {
                ElementIdValue = element.Id.Value,
                UniqueId = element.UniqueId,
                ElementKind = GetElementKind(element, isLinkedElement),
                Number = number,
                Name = name,
                Level = !string.IsNullOrWhiteSpace(GetParameterText(element, "Notus_Manual_Level")) ? GetParameterText(element, "Notus_Manual_Level") : (level != null ? level.Name : _ifcService.GetLevelName(element)),
                AreaM2 = areaM2,
                HeightM = heightM > 0 ? heightM : 2.8,
                VolumeM3 = volumeM3,
                Occupants = GetIntParameter(element, 0, "Notus_Manual_Occupants", "HVAC_Occupants", "Occupants", "Ocupantes", "Pessoas", "Numero de Pessoas", "Número de Pessoas"),
                LightingWatts = GetWattsParameter(element, 0, "HVAC_LightingWatts", "LightingWatts", "Lighting W", "Iluminacao W", "Iluminação W", "Potencia Iluminacao", "Potência Iluminação"),
                EquipmentWatts = GetWattsParameter(element, 0, "HVAC_EquipmentWatts", "EquipmentWatts", "Equipment W", "Equipamentos W", "Potencia Equipamentos", "Potência Equipamentos"),
                EnvironmentType = GetWithDefault(GetParameterText(element, "Notus_Manual_Type", "HVAC_EnvironmentType", "EnvironmentType", "TipoAmbiente", "Tipo de Ambiente", "Uso", "Departamento", "LongName", "Long Name"), "Escritorio"),
                SolarOrientation = GetWithDefault(GetParameterText(element, "HVAC_SolarOrientation", "SolarOrientation", "Orientacao Solar", "Orientação Solar", "Fachada"), "Norte"),
                InternalTempC = internalTemp,
                ExternalTempC = externalTemp,
                InternalRelativeHumidity = GetDoubleParameter(element, climate.InternalRelativeHumidity, "Notus_Manual_InternalRH", "HVAC_InternalRH", "InternalRH", "Umidade Interna", "UR Interna"),
                ExternalRelativeHumidity = GetDoubleParameter(element, climate.ExternalRelativeHumidity, "HVAC_ExternalRH", "ExternalRH", "Umidade Externa", "UR Externa"),
                ProjectCity = climate.City,
                ProjectState = climate.State,
                ClimateSource = climate.Source,
                NormativeReference = climate.NormativeReference,
                IsIfcFallback = ifcFallback,
                IsLinkedElement = isLinkedElement,
                SourceDocumentTitle = sourceDocumentTitle,
                DataQualityNote = BuildDataQualityNote(ifcFallback, isLinkedElement)
            };
        }

        public bool IsRoomOrSpace(Element element)
        {
            if (element == null || element.Category == null) return false;
            long categoryId = element.Category.Id.Value;
            return categoryId == (long)BuiltInCategory.OST_Rooms || categoryId == (long)BuiltInCategory.OST_MEPSpaces;
        }

        public bool IsSupportedEnvironmentElement(Element element)
        {
            if (element == null) return false;
            if (IsNotusResultCarrier(element)) return false;
            if (IsRoomOrSpace(element)) return true;
            if (!string.IsNullOrWhiteSpace(GetParameterText(element, "Notus_Manual_Name", "Notus_Manual_Area_m2"))) return true;
            return _ifcService.LooksLikeIfcSpace(element);
        }

        private void AddEnvironmentElements(Document sourceDoc, List<Element> result)
        {
            if (sourceDoc == null || result == null) return;

            AddElementsFromCategory(sourceDoc, BuiltInCategory.OST_Rooms, result);
            AddElementsFromCategory(sourceDoc, BuiltInCategory.OST_MEPSpaces, result);

            try
            {
                result.AddRange(new FilteredElementCollector(sourceDoc)
                    .OfCategory(BuiltInCategory.OST_GenericModel)
                    .WhereElementIsNotElementType()
                    .Where(e => !IsNotusResultCarrier(e) && !string.IsNullOrWhiteSpace(GetParameterText(e, "Notus_Manual_Name", "Notus_Manual_Area_m2")) && HasUsableArea(e)));
            }
            catch { }

            try
            {
                result.AddRange(_ifcService.GetIfcSpaceLikeElements(sourceDoc).Where(e => !IsNotusResultCarrier(e) && HasUsableArea(e)));
            }
            catch { }
        }

        private void AddElementsFromCategory(Document sourceDoc, BuiltInCategory category, List<Element> result)
        {
            try
            {
                result.AddRange(new FilteredElementCollector(sourceDoc)
                    .OfCategory(category)
                    .WhereElementIsNotElementType()
                    .Where(HasUsableArea));
            }
            catch { }
        }

        private bool HasUsableArea(Element element)
        {
            if (element == null) return false;
            if (GetAreaM2(element) > 0) return true;
            return _ifcService.LooksLikeIfcSpace(element) && _ifcService.EstimateAreaM2FromBoundingBox(element) > 0;
        }

        private bool IsNotusResultCarrier(Element element)
        {
            if (element == null) return false;

            string marker = GetParameterText(element, "Notus_ResultCarrier");
            if (marker.Equals("Sim", StringComparison.OrdinalIgnoreCase) || marker.Equals("Yes", StringComparison.OrdinalIgnoreCase)) return true;

            DirectShape shape = element as DirectShape;
            if (shape == null) return false;

            try
            {
                return string.Equals(shape.ApplicationId, "Notus", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(shape.ApplicationDataId)
                    && shape.ApplicationDataId.StartsWith("NotusReadOnlyResult_", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private List<Element> DistinctElements(IEnumerable<Element> elements)
        {
            return elements
                .Where(e => e != null)
                .GroupBy(GetElementKey)
                .Select(g => g.First())
                .ToList();
        }

        private string GetElementKey(Element element)
        {
            string docKey = GetDocumentKey(element.Document);
            if (!string.IsNullOrWhiteSpace(element.UniqueId)) return docKey + "|" + element.UniqueId;
            return docKey + "|" + element.Id.Value.ToString(CultureInfo.InvariantCulture);
        }

        private string GetDocumentKey(Document doc)
        {
            if (doc == null) return "";
            try
            {
                if (!string.IsNullOrWhiteSpace(doc.PathName)) return doc.PathName;
            }
            catch { }
            return GetDocumentTitle(doc) + "#" + doc.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        private string GetDocumentTitle(Document doc)
        {
            try { return doc != null ? (doc.Title ?? "") : ""; }
            catch { return ""; }
        }

        private string BuildDataQualityNote(bool ifcFallback, bool isLinkedElement)
        {
            List<string> notes = new List<string>();
            if (ifcFallback) notes.Add("Ambiente originado de IFC/Generic Model/DirectShape; conferir se a geometria representa espaco fechado.");
            if (isLinkedElement) notes.Add("Ambiente lido de arquivo vinculado; o resultado nao e gravado no vinculo a partir do modelo ativo.");
            return string.Join(" ", notes);
        }

        private string GetElementKind(Element element, bool isLinkedElement)
        {
            if (element == null || element.Category == null) return isLinkedElement ? "Vinculo" : "";

            string kind;
            long categoryId = element.Category.Id.Value;
            if (categoryId == (long)BuiltInCategory.OST_Rooms) kind = "Room";
            else if (categoryId == (long)BuiltInCategory.OST_MEPSpaces) kind = "Space";
            else if (!string.IsNullOrWhiteSpace(GetParameterText(element, "Notus_Manual_Name", "Notus_Manual_Area_m2"))) kind = "Ambiente HVAC Manual";
            else if (_ifcService.LooksLikeIfcSpace(element)) kind = "IFC/GenericModel";
            else kind = element.Category.Name;

            return isLinkedElement ? kind + " (vinculo)" : kind;
        }

        private double GetAreaM2(Element element)
        {
            if (element == null) return 0;

            double manualArea = GetDoubleParameter(element, 0, "Notus_Manual_Area_m2");
            if (manualArea > 0) return manualArea;

            Parameter parameter = element.get_Parameter(BuiltInParameter.ROOM_AREA);
            if (parameter != null && parameter.HasValue)
            {
                return UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.SquareMeters);
            }

            parameter = element.LookupParameter("Area")
                ?? element.LookupParameter("Área")
                ?? element.LookupParameter("GrossArea")
                ?? element.LookupParameter("NetArea")
                ?? element.LookupParameter("Gross Floor Area")
                ?? element.LookupParameter("Net Floor Area")
                ?? element.LookupParameter("BaseQuantities.NetFloorArea")
                ?? element.LookupParameter("BaseQuantities.GrossFloorArea");
            if (parameter != null && parameter.HasValue)
            {
                if (parameter.StorageType == StorageType.Double)
                {
                    double raw = parameter.AsDouble();
                    double asM2 = UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.SquareMeters);
                    return asM2 > 0 ? asM2 : raw;
                }

                return ParseDouble(GetParameterRawText(parameter));
            }

            return 0;
        }

        private double GetHeightM(Element element)
        {
            if (element == null) return 2.8;

            double manualHeight = GetDoubleParameter(element, 0, "Notus_Manual_Height_m");
            if (manualHeight > 0) return manualHeight;

            Parameter parameter = element.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
            if (parameter != null && parameter.HasValue)
            {
                return UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.Meters);
            }

            parameter = element.LookupParameter("Unbounded Height")
                ?? element.LookupParameter("Altura nao delimitada")
                ?? element.LookupParameter("Altura não delimitada")
                ?? element.LookupParameter("Altura")
                ?? element.LookupParameter("Height")
                ?? element.LookupParameter("NominalHeight");
            if (parameter != null && parameter.HasValue)
            {
                if (parameter.StorageType == StorageType.Double)
                {
                    double raw = parameter.AsDouble();
                    double meters = UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.Meters);
                    return meters > 0 ? meters : raw;
                }

                return ParseDouble(GetParameterRawText(parameter));
            }

            return 2.8;
        }

        private double GetVolumeM3(Element element)
        {
            if (element == null) return 0;

            double manualVolume = GetDoubleParameter(element, 0, "Notus_Manual_Volume_m3");
            if (manualVolume > 0) return manualVolume;

            Parameter parameter = element.get_Parameter(BuiltInParameter.ROOM_VOLUME);
            if (parameter != null && parameter.HasValue)
            {
                return UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.CubicMeters);
            }

            parameter = element.LookupParameter("Volume") ?? element.LookupParameter("GrossVolume") ?? element.LookupParameter("NetVolume");
            if (parameter != null && parameter.HasValue)
            {
                if (parameter.StorageType == StorageType.Double)
                {
                    double raw = parameter.AsDouble();
                    double m3 = UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.CubicMeters);
                    return m3 > 0 ? m3 : raw;
                }

                return ParseDouble(GetParameterRawText(parameter));
            }

            return 0;
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

        private int GetIntParameter(Element element, int defaultValue, params string[] names)
        {
            foreach (string name in names)
            {
                Parameter parameter = element.LookupParameter(name);
                if (parameter == null || !parameter.HasValue) continue;
                if (parameter.StorageType == StorageType.Integer) return Math.Max(0, parameter.AsInteger());
                int parsed;
                string text = GetParameterRawText(parameter).Trim();
                if (int.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed) || int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) return Math.Max(0, parsed);
            }
            return defaultValue;
        }

        private double GetDoubleParameter(Element element, double defaultValue, params string[] names)
        {
            if (element == null) return defaultValue;
            foreach (string name in names)
            {
                Parameter parameter = element.LookupParameter(name);
                if (parameter == null || !parameter.HasValue) continue;
                if (parameter.StorageType == StorageType.Double) return ConvertParameterDoubleToDisplayUnit(parameter);
                double parsed = ParseDouble(GetParameterRawText(parameter).Replace("°C", "").Replace("C", "").Replace("%", ""));
                if (parsed != 0) return parsed;
            }
            return defaultValue;
        }

        private double ConvertParameterDoubleToDisplayUnit(Parameter parameter)
        {
            double raw = parameter.AsDouble();
            try
            {
                ForgeTypeId spec = parameter.Definition?.GetDataType();
                string specId = spec?.TypeId?.ToLowerInvariant() ?? string.Empty;
                if (specId.Contains("temperature"))
                {
                    return UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.Celsius);
                }
            }
            catch
            {
            }
            return raw;
        }

        private string GetParameterRawText(Parameter parameter)
        {
            if (parameter == null) return "";
            if (parameter.StorageType == StorageType.String) return parameter.AsString() ?? "";
            return parameter.AsValueString() ?? "";
        }

        private double GetWattsParameter(Element element, double defaultValue, params string[] names)
        {
            double value = GetDoubleParameter(element, defaultValue, names);
            return Math.Max(0, value);
        }

        private double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            text = text.Trim()
                .Replace("m²", "")
                .Replace("m2", "")
                .Replace("m³", "")
                .Replace("m3", "")
                .Trim();

            int lastComma = text.LastIndexOf(',');
            int lastDot = text.LastIndexOf('.');
            string normalized;
            if (lastComma > lastDot)
            {
                normalized = text.Replace(".", "").Replace(",", ".");
            }
            else if (lastDot > lastComma)
            {
                normalized = text.Replace(",", "");
            }
            else
            {
                normalized = text;
            }

            double parsed;
            if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) return parsed;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed)) return parsed;
            return 0;
        }

        private string GetWithDefault(string value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }
}

