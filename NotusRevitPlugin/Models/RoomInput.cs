namespace NotusRevitPlugin.Models
{
    public class RoomInput
    {
        public long ElementIdValue { get; set; }
        public string UniqueId { get; set; } = "";
        public string ElementKind { get; set; } = "";
        public string Number { get; set; } = "";
        public string Name { get; set; } = "";
        public string Level { get; set; } = "";
        public double AreaM2 { get; set; }
        public double HeightM { get; set; }
        public double VolumeM3 { get; set; }
        public int Occupants { get; set; }
        public double LightingWatts { get; set; }
        public double EquipmentWatts { get; set; }
        public string EnvironmentType { get; set; } = "Escritorio";
        public string SolarOrientation { get; set; } = "Norte";
        public double InternalTempC { get; set; } = 24;
        public double ExternalTempC { get; set; } = 35;
        public double InternalRelativeHumidity { get; set; } = 50;
        public double ExternalRelativeHumidity { get; set; } = 65;
        public string ProjectCity { get; set; } = "";
        public string ProjectState { get; set; } = "";
        public string ClimateSource { get; set; } = "";
        public string NormativeReference { get; set; } = "";
        public bool IsIfcFallback { get; set; }
        public bool IsLinkedElement { get; set; }
        public string SourceDocumentTitle { get; set; } = "";
        public string DataQualityNote { get; set; } = "";
    }
}
