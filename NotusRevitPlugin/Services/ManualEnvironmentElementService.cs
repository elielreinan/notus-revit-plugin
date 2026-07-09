using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Services
{
    public static class ManualEnvironmentElementService
    {
        public static ManualEnvironmentData BuildDefaultFromElement(Document doc, Element source)
        {
            HvacSettings settings = HvacSettingsService.Load();
            IfcEnvironmentService ifc = new IfcEnvironmentService();
            double area = 20;
            double height = 2.8;
            string level = "";
            string category = "";
            long sourceId = 0;
            string name = "Ambiente HVAC Manual";

            if (source != null)
            {
                sourceId = source.Id.Value;
                name = "Ambiente HVAC Manual - " + SafeName(source);
                category = source.Category != null ? source.Category.Name : "";
                area = Math.Max(0.01, ifc.EstimateAreaM2FromBoundingBox(source));
                height = Math.Max(0.5, ifc.EstimateHeightMFromBoundingBox(source));
                level = ifc.GetLevelName(source);
            }

            EnvironmentTypeCriteria criteria = HvacCriteriaService.Get(settings.DefaultEnvironmentType);
            double volume = area * height;
            int occupants = area > 0 && criteria.M2PerPerson > 0 ? Math.Max(1, (int)Math.Ceiling(area / criteria.M2PerPerson)) : 1;
            double renewal = ((criteria.RenewalLsPerPerson * occupants) + (criteria.RenewalLsPerM2 * area)) * 3.6;

            return new ManualEnvironmentData
            {
                Name = name,
                Level = level,
                AreaM2 = area,
                HeightM = height,
                VolumeM3 = volume,
                Occupants = occupants,
                EnvironmentType = criteria.Name,
                InternalTempC = criteria.InternalTempC > 0 ? criteria.InternalTempC : settings.DefaultInternalTempC,
                InternalRelativeHumidity = criteria.InternalRh > 0 ? criteria.InternalRh : settings.DefaultInternalRelativeHumidity,
                RenewalAirM3h = renewal,
                Observation = source == null ? "Ambiente manual cadastrado sem elemento de origem. Conferir limites, área e volume." : "Ambiente manual criado a partir de elemento IFC/arquitetura. Conferir limites, área e volume.",
                SourceElementId = sourceId,
                SourceCategory = category
            };
        }

        public static Element CreateManualElement(Document doc, ManualEnvironmentData data, Element source)
        {
            BoundingBoxXYZ box = source != null ? source.get_BoundingBox(null) : null;
            XYZ center = box != null ? new XYZ((box.Min.X + box.Max.X) / 2.0, (box.Min.Y + box.Max.Y) / 2.0, (box.Min.Z + box.Max.Z) / 2.0) : XYZ.Zero;
            Solid solid = CreateSolid(data, center);
            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.ApplicationId = "Notus";
            ds.ApplicationDataId = "ManualEnvironment_" + Guid.NewGuid().ToString("N");
            ds.Name = data.Name;
            ds.SetShape(new List<GeometryObject> { solid });
            WriteData(ds, data);
            return ds;
        }

        public static void UpdateManualElement(Element element, ManualEnvironmentData data)
        {
            if (element == null || data == null) return;
            try { element.Name = data.Name; } catch { }
            WriteData(element, data);
            try
            {
                DirectShape ds = element as DirectShape;
                if (ds != null)
                {
                    BoundingBoxXYZ box = element.get_BoundingBox(null);
                    XYZ center = box != null ? new XYZ((box.Min.X + box.Max.X) / 2.0, (box.Min.Y + box.Max.Y) / 2.0, (box.Min.Z + box.Max.Z) / 2.0) : XYZ.Zero;
                    ds.SetShape(new List<GeometryObject> { CreateSolid(data, center) });
                }
            }
            catch { }
        }

        public static ManualEnvironmentData ReadData(Element element)
        {
            return new ManualEnvironmentData
            {
                Name = GetText(element, "Notus_Manual_Name", "Name", "Nome"),
                Level = GetText(element, "Notus_Manual_Level", "Level", "Nível", "Pavimento"),
                AreaM2 = GetDouble(element, "Notus_Manual_Area_m2"),
                HeightM = GetDouble(element, "Notus_Manual_Height_m"),
                VolumeM3 = GetDouble(element, "Notus_Manual_Volume_m3"),
                Occupants = (int)Math.Max(0, GetDouble(element, "Notus_Manual_Occupants")),
                EnvironmentType = GetText(element, "Notus_Manual_Type"),
                InternalTempC = GetDouble(element, "Notus_Manual_InternalTempC"),
                InternalRelativeHumidity = GetDouble(element, "Notus_Manual_InternalRH"),
                RenewalAirM3h = GetDouble(element, "Notus_Manual_Renewal_m3h"),
                Observation = GetText(element, "Notus_Manual_Observation", "Notus_Notes"),
                SourceElementId = (long)Math.Max(0, GetDouble(element, "Notus_Manual_SourceElementId")),
                SourceCategory = GetText(element, "Notus_Manual_SourceCategory")
            };
        }

        public static bool IsManualEnvironment(Element element)
        {
            return element != null && !string.IsNullOrWhiteSpace(GetText(element, "Notus_Manual_Name", "Notus_Manual_Area_m2"));
        }

        private static Solid CreateSolid(ManualEnvironmentData data, XYZ center)
        {
            double areaM2 = Math.Max(0.01, data.AreaM2);
            double heightM = Math.Max(0.5, data.HeightM);
            double sideM = Math.Sqrt(areaM2);
            double half = UnitUtils.ConvertToInternalUnits(sideM / 2.0, UnitTypeId.Meters);
            double z = center.Z;
            double height = UnitUtils.ConvertToInternalUnits(heightM, UnitTypeId.Meters);
            XYZ p1 = new XYZ(center.X - half, center.Y - half, z);
            XYZ p2 = new XYZ(center.X + half, center.Y - half, z);
            XYZ p3 = new XYZ(center.X + half, center.Y + half, z);
            XYZ p4 = new XYZ(center.X - half, center.Y + half, z);
            CurveLoop loop = new CurveLoop();
            loop.Append(Line.CreateBound(p1, p2));
            loop.Append(Line.CreateBound(p2, p3));
            loop.Append(Line.CreateBound(p3, p4));
            loop.Append(Line.CreateBound(p4, p1));
            return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { loop }, XYZ.BasisZ, height);
        }

        public static void WriteData(Element e, ManualEnvironmentData d)
        {
            Set(e, "Notus_Manual_Name", d.Name);
            Set(e, "Notus_Manual_Level", d.Level);
            Set(e, "Notus_Manual_Area_m2", F(d.AreaM2, 2));
            Set(e, "Notus_Manual_Height_m", F(d.HeightM, 2));
            Set(e, "Notus_Manual_Volume_m3", F(d.VolumeM3, 2));
            Set(e, "Notus_Manual_Occupants", d.Occupants.ToString(CultureInfo.InvariantCulture));
            Set(e, "Notus_Manual_Type", d.EnvironmentType);
            Set(e, "Notus_Manual_InternalTempC", F(d.InternalTempC, 1));
            Set(e, "Notus_Manual_InternalRH", F(d.InternalRelativeHumidity, 0));
            Set(e, "Notus_Manual_Renewal_m3h", F(d.RenewalAirM3h, 0));
            Set(e, "Notus_Manual_Observation", d.Observation);
            Set(e, "Notus_Manual_SourceElementId", d.SourceElementId.ToString(CultureInfo.InvariantCulture));
            Set(e, "Notus_Manual_SourceCategory", d.SourceCategory);
            Set(e, "Notus_Notes", d.Observation);
        }

        private static void Set(Element element, string name, string value)
        {
            Parameter p = element.LookupParameter(name);
            if (p != null && !p.IsReadOnly) p.Set(value ?? "");
        }

        private static string GetText(Element element, params string[] names)
        {
            foreach (string name in names)
            {
                Parameter p = element.LookupParameter(name);
                if (p == null || !p.HasValue) continue;
                if (p.StorageType == StorageType.String) return p.AsString() ?? "";
                return p.AsValueString() ?? "";
            }
            return "";
        }

        private static double GetDouble(Element element, string name)
        {
            string text = GetText(element, name);
            if (string.IsNullOrWhiteSpace(text)) return 0;
            text = text.Replace("m²", "").Replace("m2", "").Replace("m³", "").Replace("m3", "").Replace("°C", "").Replace("%", "").Trim();
            if (text.Contains(",")) text = text.Replace(".", "").Replace(",", ".");
            double value;
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        private static string F(double value, int decimals) { return value.ToString("F" + decimals, CultureInfo.InvariantCulture); }
        private static string SafeName(Element element) { return string.IsNullOrWhiteSpace(element.Name) ? element.Id.Value.ToString(CultureInfo.InvariantCulture) : element.Name; }
    }
}


