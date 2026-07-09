namespace NotusRevitPlugin.Models
{
    public class HvacResult
    {
        public double TotalBTUh { get; set; }
        public double TotalTR { get; set; }
        public double SensibleBTUh { get; set; }
        public double LatentBTUh { get; set; }
        public double SensibleTR { get; set; }
        public double SHR { get; set; }
        public double SupplyAirflowM3h { get; set; }
        public double ExternalAirflowM3h { get; set; }
        public double AirChangesHour { get; set; }
        public double PeopleSensibleBTUh { get; set; }
        public double PeopleLatentBTUh { get; set; }
        public double LightingBTUh { get; set; }
        public double EquipmentBTUh { get; set; }
        public double EnvelopeBTUh { get; set; }
        public double SolarBTUh { get; set; }
        public double WallRoofBTUh { get; set; }
        public double InfiltrationBTUh { get; set; }
        public double VentilationSensibleBTUh { get; set; }
        public double VentilationLatentBTUh { get; set; }
        public double HumidityLatentBTUh { get; set; }
        public double ExternalByNormM3h { get; set; }
        public double ExternalByAchM3h { get; set; }
        public string EquipmentSelected { get; set; } = "";
        public string Method { get; set; } = "";
        public string ClimateSummary { get; set; } = "";
        public string InternalExternalSummary { get; set; } = "";
        public string NormativeReference { get; set; } = "";
        public string Notes { get; set; } = "";
        public string MemorialSummary { get; set; } = "";
    }
}

