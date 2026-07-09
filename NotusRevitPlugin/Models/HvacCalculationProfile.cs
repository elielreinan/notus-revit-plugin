namespace NotusRevitPlugin.Models
{
    public class HvacCalculationProfile
    {
        public string Name { get; set; } = "ABNT simplificado";
        public double DefaultAreaLoadBTUhM2 { get; set; } = 600;
        public double SensibleBTUhPerPerson { get; set; } = 245;
        public double LatentBTUhPerPerson { get; set; } = 205;
        public double RpLsPerPerson { get; set; } = 2.5;
        public double RaLsPerM2 { get; set; } = 0.3;
        public double SupplyAirTempC { get; set; } = 13;
        public double SafetyFactor { get; set; } = 1.10;
        public double MinimumAch { get; set; } = 2.0;
        public double DefaultInternalRh { get; set; } = 50;
        public double DefaultExternalRh { get; set; } = 65;
        public string NormativeReference { get; set; } = "ABNT NBR 16401 / ASHRAE 62.1 - parâmetros simplificados configuráveis";
    }
}


