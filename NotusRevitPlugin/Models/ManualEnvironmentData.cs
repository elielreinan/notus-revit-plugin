namespace NotusRevitPlugin.Models
{
    public class ManualEnvironmentData
    {
        public string Name { get; set; } = "Ambiente HVAC Manual";
        public string Level { get; set; } = "";
        public double AreaM2 { get; set; } = 0;
        public double HeightM { get; set; } = 2.8;
        public double VolumeM3 { get; set; } = 0;
        public int Occupants { get; set; } = 1;
        public string EnvironmentType { get; set; } = "Escritório";
        public double InternalTempC { get; set; } = 24;
        public double InternalRelativeHumidity { get; set; } = 50;
        public double RenewalAirM3h { get; set; } = 0;
        public string Observation { get; set; } = "";
        public long SourceElementId { get; set; } = 0;
        public string SourceCategory { get; set; } = "";
    }
}


