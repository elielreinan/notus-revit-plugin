using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Services
{
    public static class HvacResultElementService
    {
        private const string ApplicationId = "Notus";
        private const string CarrierPrefix = "NotusReadOnlyResult_";

        public static int ReplaceReadOnlyResultElements(Document doc, IEnumerable<HvacCalculationPreview> previews)
        {
            if (doc == null) return 0;

            DeleteExistingCarriers(doc);

            int created = 0;
            foreach (HvacCalculationPreview preview in previews ?? Enumerable.Empty<HvacCalculationPreview>())
            {
                if (preview == null || preview.Input == null || preview.Result == null) continue;
                if (preview.Input.AreaM2 <= 0 || preview.Input.VolumeM3 <= 0) continue;

                DirectShape carrier = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                carrier.ApplicationId = ApplicationId;
                carrier.ApplicationDataId = CarrierPrefix + created.ToString("D4", CultureInfo.InvariantCulture);
                carrier.Name = BuildCarrierName(preview.Input);
                carrier.SetShape(new List<GeometryObject> { CreateMarkerSolid(created) });

                WriteResultData(carrier, preview.Input, preview.Result, preview.Alerts);
                SetParameter(carrier, "Notus_ResultCarrier", "Sim");
                SetParameter(carrier, "Notus_Manual_Observation", "Linha automatica criada no modelo ativo para tabela/grafico de ambiente em vinculo ou IFC somente leitura.");
                created++;
            }

            return created;
        }

        public static void WriteResultData(Element element, RoomInput input, HvacResult result, IList<string> alerts)
        {
            if (element == null || input == null || result == null) return;

            SetParameter(element, "Notus_Room_Number", input.Number);
            SetParameter(element, "Notus_Room_Name", input.Name);
            SetParameter(element, "Notus_Level", input.Level);
            SetParameter(element, "Notus_Area_m2", Format(input.AreaM2, 2));
            SetParameter(element, "Notus_Volume_m3", Format(input.VolumeM3, 2));
            SetParameter(element, "Notus_Occupants", input.Occupants.ToString(CultureInfo.InvariantCulture));
            SetParameter(element, "Notus_m2_per_TR", result.TotalTR > 0 ? Format(input.AreaM2 / result.TotalTR, 2) : "");
            SetParameter(element, "Notus_Source", BuildSource(input));

            SetParameter(element, "Notus_Manual_Name", input.Name);
            SetParameter(element, "Notus_Manual_Level", input.Level);
            SetParameter(element, "Notus_Manual_Area_m2", Format(input.AreaM2, 2));
            SetParameter(element, "Notus_Manual_Height_m", Format(input.HeightM, 2));
            SetParameter(element, "Notus_Manual_Volume_m3", Format(input.VolumeM3, 2));
            SetParameter(element, "Notus_Manual_Occupants", input.Occupants.ToString(CultureInfo.InvariantCulture));
            SetParameter(element, "Notus_Manual_Type", input.EnvironmentType);
            SetParameter(element, "Notus_Manual_InternalTempC", Format(input.InternalTempC, 1));
            SetParameter(element, "Notus_Manual_InternalRH", Format(input.InternalRelativeHumidity, 0));
            SetParameter(element, "Notus_Manual_SourceElementId", input.ElementIdValue.ToString(CultureInfo.InvariantCulture));
            SetParameter(element, "Notus_Manual_SourceCategory", input.ElementKind);

            SetParameter(element, "Notus_Total_BTU", Format(result.TotalBTUh, 0));
            SetParameter(element, "Notus_TR", Format(result.TotalTR, 2));
            SetParameter(element, "Notus_Sensible_BTU", Format(result.SensibleBTUh, 0));
            SetParameter(element, "Notus_Sensible_TR", Format(result.SensibleTR, 2));
            SetParameter(element, "Notus_Latent_BTU", Format(result.LatentBTUh, 0));
            SetParameter(element, "Notus_SHR", Format(result.SHR, 2));
            SetParameter(element, "Notus_Supply_m3h", Format(result.SupplyAirflowM3h, 0));
            SetParameter(element, "Notus_External_m3h", Format(result.ExternalAirflowM3h, 0));
            SetParameter(element, "Notus_AirChanges_h", Format(result.AirChangesHour, 2));
            SetParameter(element, "Notus_Equipment", result.EquipmentSelected);
            SetParameter(element, "Notus_Method", result.Method);
            SetParameter(element, "Notus_Climate", result.ClimateSummary);
            SetParameter(element, "Notus_InternalExternal", result.InternalExternalSummary);
            SetParameter(element, "Notus_NormRef", result.NormativeReference);
            SetParameter(element, "Notus_People_BTU", Format(result.PeopleSensibleBTUh + result.PeopleLatentBTUh, 0));
            SetParameter(element, "Notus_Lighting_BTU", Format(result.LightingBTUh, 0));
            SetParameter(element, "Notus_Equipment_BTU", Format(result.EquipmentBTUh, 0));
            SetParameter(element, "Notus_Envelope_BTU", Format(result.EnvelopeBTUh, 0));
            SetParameter(element, "Notus_Solar_BTU", Format(result.SolarBTUh, 0));
            SetParameter(element, "Notus_WallRoof_BTU", Format(result.WallRoofBTUh, 0));
            SetParameter(element, "Notus_Infiltration_BTU", Format(result.InfiltrationBTUh, 0));
            SetParameter(element, "Notus_Ventilation_BTU", Format(result.VentilationSensibleBTUh + result.VentilationLatentBTUh, 0));
            SetParameter(element, "Notus_Memorial", result.MemorialSummary);
            SetParameter(element, "Notus_Notes", result.Notes);
            SetParameter(element, "Notus_ValidationAlerts", alerts == null || alerts.Count == 0 ? "Sem alertas" : string.Join("; ", alerts));
            SetParameter(element, "Notus_CriteriaProfile", "Perfil tecnico baseado em criterios editaveis");
        }

        private static void DeleteExistingCarriers(Document doc)
        {
            List<ElementId> ids = new FilteredElementCollector(doc)
                .OfClass(typeof(DirectShape))
                .Cast<DirectShape>()
                .Where(IsResultCarrier)
                .Select(e => e.Id)
                .ToList();

            if (ids.Count > 0) doc.Delete(ids);
        }

        private static bool IsResultCarrier(DirectShape shape)
        {
            try
            {
                return shape != null
                    && string.Equals(shape.ApplicationId, ApplicationId, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(shape.ApplicationDataId)
                    && shape.ApplicationDataId.StartsWith(CarrierPrefix, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static Solid CreateMarkerSolid(int index)
        {
            double size = UnitUtils.ConvertToInternalUnits(0.05, UnitTypeId.Meters);
            double spacing = UnitUtils.ConvertToInternalUnits(0.08, UnitTypeId.Meters);
            int col = index % 20;
            int row = index / 20;
            XYZ origin = new XYZ(col * spacing, row * spacing, 0);

            XYZ p1 = origin;
            XYZ p2 = origin + new XYZ(size, 0, 0);
            XYZ p3 = origin + new XYZ(size, size, 0);
            XYZ p4 = origin + new XYZ(0, size, 0);

            CurveLoop loop = new CurveLoop();
            loop.Append(Line.CreateBound(p1, p2));
            loop.Append(Line.CreateBound(p2, p3));
            loop.Append(Line.CreateBound(p3, p4));
            loop.Append(Line.CreateBound(p4, p1));
            return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { loop }, XYZ.BasisZ, size);
        }

        private static string BuildCarrierName(RoomInput input)
        {
            string label = string.IsNullOrWhiteSpace(input.Number) ? input.Name : input.Number + " - " + input.Name;
            if (string.IsNullOrWhiteSpace(label)) label = input.ElementIdValue.ToString(CultureInfo.InvariantCulture);
            if (label.Length > 80) label = label.Substring(0, 80);
            return "Notus - Resultado IFC/Vinculo - " + label;
        }

        private static string BuildSource(RoomInput input)
        {
            if (input == null) return "";
            if (input.IsLinkedElement && !string.IsNullOrWhiteSpace(input.SourceDocumentTitle)) return "Vinculo: " + input.SourceDocumentTitle;
            if (input.IsIfcFallback) return "IFC/Generic Model";
            return "Modelo ativo";
        }

        private static void SetParameter(Element element, string name, string value)
        {
            Parameter parameter = element.LookupParameter(name);
            if (parameter == null || parameter.IsReadOnly) return;
            parameter.Set(value ?? "");
        }

        private static string Format(double value, int decimals)
        {
            return HvacCalculationService.FormatNumber(value, decimals);
        }
    }
}
