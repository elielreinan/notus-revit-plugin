using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Services
{
    public static class HvacSettingsService
    {
        private const string FolderName = "Notus";
        private const string FileName = "notus_settings.ini";

        public static string GetSettingsFolder()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName);
            Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetSettingsPath()
        {
            return Path.Combine(GetSettingsFolder(), FileName);
        }

        public static HvacSettings Load()
        {
            HvacSettings settings = new HvacSettings();
            string path = GetSettingsPath();
            if (!File.Exists(path))
            {
                Save(settings);
                return settings;
            }

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int idx = line.IndexOf('=');
                if (idx <= 0) continue;
                string key = line.Substring(0, idx).Trim();
                string value = line.Substring(idx + 1).Trim();
                Apply(settings, key, value);
            }

            return settings;
        }

        public static void Save(HvacSettings settings)
        {
            Directory.CreateDirectory(GetSettingsFolder());
            List<string> lines = new List<string>
            {
                "# Notus - configurações editáveis",
                "# Altere os valores, salve o arquivo e reinicie o Revit para carregar os novos padrões.",
                "# Os critérios são parametrizados para pré-dimensionamento e devem ser validados pelo responsável técnico.",
                "DefaultCity=" + settings.DefaultCity,
                "DefaultState=" + settings.DefaultState,
                "DefaultNorm=" + settings.DefaultNorm,
                "DefaultEnvironmentType=" + settings.DefaultEnvironmentType,
                "DefaultInternalTempC=" + ToText(settings.DefaultInternalTempC),
                "DefaultInternalRelativeHumidity=" + ToText(settings.DefaultInternalRelativeHumidity),
                "DefaultExternalTempC=" + ToText(settings.DefaultExternalTempC),
                "DefaultExternalRelativeHumidity=" + ToText(settings.DefaultExternalRelativeHumidity),
                "DefaultLightingWattsPerM2=" + ToText(settings.DefaultLightingWattsPerM2),
                "DefaultEquipmentWattsPerM2=" + ToText(settings.DefaultEquipmentWattsPerM2),
                "DefaultOccupancyM2PerPerson=" + ToText(settings.DefaultOccupancyM2PerPerson),
                "DefaultVentilationLsPerPerson=" + ToText(settings.DefaultVentilationLsPerPerson),
                "DefaultVentilationLsPerM2=" + ToText(settings.DefaultVentilationLsPerM2),
                "DefaultMinimumAch=" + ToText(settings.DefaultMinimumAch),
                "DefaultExportFolder=" + settings.DefaultExportFolder,
                "ConfirmBeforeOverwrite=" + (settings.ConfirmBeforeOverwrite ? "true" : "false"),
                "UseProjectLocation=" + (settings.UseProjectLocation ? "true" : "false")
            };
            File.WriteAllLines(GetSettingsPath(), lines);
        }

        public static void OpenSettingsFolder()
        {
            Directory.CreateDirectory(GetSettingsFolder());
            if (!File.Exists(GetSettingsPath())) Save(new HvacSettings());
            Process.Start(new ProcessStartInfo(GetSettingsFolder()) { UseShellExecute = true });
        }

        private static void Apply(HvacSettings s, string key, string value)
        {
            string k = key.Trim().ToLowerInvariant();
            if (k == "defaultcity") s.DefaultCity = value;
            else if (k == "defaultstate") s.DefaultState = value;
            else if (k == "defaultnorm") s.DefaultNorm = value;
            else if (k == "defaultenvironmenttype") s.DefaultEnvironmentType = value;
            else if (k == "defaultinternaltempc") s.DefaultInternalTempC = Parse(value, s.DefaultInternalTempC);
            else if (k == "defaultinternalrelativehumidity") s.DefaultInternalRelativeHumidity = Parse(value, s.DefaultInternalRelativeHumidity);
            else if (k == "defaultexternaltempc") s.DefaultExternalTempC = Parse(value, s.DefaultExternalTempC);
            else if (k == "defaultexternalrelativehumidity") s.DefaultExternalRelativeHumidity = Parse(value, s.DefaultExternalRelativeHumidity);
            else if (k == "defaultlightingwattsperm2") s.DefaultLightingWattsPerM2 = Parse(value, s.DefaultLightingWattsPerM2);
            else if (k == "defaultequipmentwattsperm2") s.DefaultEquipmentWattsPerM2 = Parse(value, s.DefaultEquipmentWattsPerM2);
            else if (k == "defaultoccupancym2perperson") s.DefaultOccupancyM2PerPerson = Parse(value, s.DefaultOccupancyM2PerPerson);
            else if (k == "defaultventilationlsperperson") s.DefaultVentilationLsPerPerson = Parse(value, s.DefaultVentilationLsPerPerson);
            else if (k == "defaultventilationlsperm2") s.DefaultVentilationLsPerM2 = Parse(value, s.DefaultVentilationLsPerM2);
            else if (k == "defaultminimumach") s.DefaultMinimumAch = Parse(value, s.DefaultMinimumAch);
            else if (k == "defaultexportfolder") s.DefaultExportFolder = value;
            else if (k == "confirmbeforeoverwrite") s.ConfirmBeforeOverwrite = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1" || value.Equals("sim", StringComparison.OrdinalIgnoreCase);
            else if (k == "useprojectlocation") s.UseProjectLocation = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1" || value.Equals("sim", StringComparison.OrdinalIgnoreCase);
        }

        private static double Parse(string value, double fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            value = value.Replace(",", ".");
            double result;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            return fallback;
        }

        private static string ToText(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}


