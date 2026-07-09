namespace NotusRevitPlugin.Models
{
    public class EnvironmentTypeCriteria
    {
        public string Name { get; set; } = "Escritório";
        public double M2PerPerson { get; set; } = 10;
        public double LightingWPerM2 { get; set; } = 12;
        public double EquipmentWPerM2 { get; set; } = 15;
        public double RenewalLsPerPerson { get; set; } = 2.5;
        public double RenewalLsPerM2 { get; set; } = 0.30;
        public double InternalTempC { get; set; } = 24;
        public double InternalRh { get; set; } = 50;
        public double SafetyFactor { get; set; } = 1.10;
        public double MinimumAch { get; set; } = 2.0;
        public string Note { get; set; } = "Perfil técnico baseado em critérios editáveis.";
    }
}


