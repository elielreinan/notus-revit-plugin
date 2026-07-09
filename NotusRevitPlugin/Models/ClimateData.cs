namespace NotusRevitPlugin.Models
{
    public class ClimateData
    {
        public string City { get; set; } = "Não informado";
        public string State { get; set; } = "";
        public string Source { get; set; } = "Padrão técnico";
        public double ExternalDryBulbC { get; set; } = 35;
        public double ExternalWetBulbC { get; set; } = 27;
        public double ExternalRelativeHumidity { get; set; } = 65;
        public double InternalDesignTempC { get; set; } = 24;
        public double InternalRelativeHumidity { get; set; } = 50;
        public string NormativeReference { get; set; } = "ABNT NBR 16401 / ASHRAE 62.1 - valores parametrizados para pré-dimensionamento";

        public string Summary
        {
            get
            {
                string local = string.IsNullOrWhiteSpace(State) ? City : City + "/" + State;
                return local + " | Externo " + ExternalDryBulbC.ToString("0.#") + " °C, UR " + ExternalRelativeHumidity.ToString("0") + "% | Interno " + InternalDesignTempC.ToString("0.#") + " °C, UR " + InternalRelativeHumidity.ToString("0") + "%";
            }
        }
    }
}


