namespace NotusRevitPlugin.Models
{
    public class HvacSettings
    {
        public string DefaultCity { get; set; } = "Salvador";
        public string DefaultState { get; set; } = "BA";
        public string DefaultNorm { get; set; } = "ABNT simplificado";
        public string DefaultEnvironmentType { get; set; } = "Escritório";
        public double DefaultInternalTempC { get; set; } = 24;
        public double DefaultInternalRelativeHumidity { get; set; } = 50;
        public double DefaultExternalTempC { get; set; } = 35;
        public double DefaultExternalRelativeHumidity { get; set; } = 65;
        public double DefaultLightingWattsPerM2 { get; set; } = 12;
        public double DefaultEquipmentWattsPerM2 { get; set; } = 15;
        public double DefaultOccupancyM2PerPerson { get; set; } = 10;
        public double DefaultVentilationLsPerPerson { get; set; } = 2.5;
        public double DefaultVentilationLsPerM2 { get; set; } = 0.30;
        public double DefaultMinimumAch { get; set; } = 2.0;
        public string DefaultExportFolder { get; set; } = "Desktop";
        public bool ConfirmBeforeOverwrite { get; set; } = true;
        public bool UseProjectLocation { get; set; } = true;
    }
}


