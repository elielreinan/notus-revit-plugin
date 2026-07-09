using System;
using Autodesk.Revit.DB;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Services
{
    public class ClimateService
    {
        public ClimateData GetClimateForProject(Document doc)
        {
            HvacSettings settings = HvacSettingsService.Load();
            string city = GetProjectText(doc, "HVAC_Cidade", "Cidade", "City", "Município", "Municipio", "Localidade", "Location");
            string state = GetProjectText(doc, "HVAC_UF", "UF", "Estado", "State", "Province");

            if (string.IsNullOrWhiteSpace(city)) city = settings.DefaultCity;
            if (string.IsNullOrWhiteSpace(state)) state = settings.DefaultState;

            ClimateData climate;
            ClimateData byCoordinates = settings.UseProjectLocation ? TryFromSiteLocation(doc, settings) : null;
            if (byCoordinates != null && (string.IsNullOrWhiteSpace(city) || city.Equals("Usar localização do projeto", StringComparison.OrdinalIgnoreCase)))
            {
                climate = byCoordinates;
            }
            else
            {
                climate = FromCity(city, state, settings);
            }

            climate.InternalDesignTempC = settings.DefaultInternalTempC;
            climate.InternalRelativeHumidity = settings.DefaultInternalRelativeHumidity;
            if (settings.DefaultExternalTempC > 0) climate.ExternalDryBulbC = settings.DefaultExternalTempC;
            if (settings.DefaultExternalRelativeHumidity > 0) climate.ExternalRelativeHumidity = settings.DefaultExternalRelativeHumidity;
            climate.Source = climate.Source + " + configurações Notus";
            return climate;
        }

        private ClimateData TryFromSiteLocation(Document doc, HvacSettings settings)
        {
            try
            {
                SiteLocation site = doc.SiteLocation;
                if (site == null) return null;
                double lat = site.Latitude;
                double lon = site.Longitude;
                if (Math.Abs(lat) <= Math.PI) lat = lat * 180.0 / Math.PI;
                if (Math.Abs(lon) <= Math.PI) lon = lon * 180.0 / Math.PI;
                if (Math.Abs(lat) < 0.001 && Math.Abs(lon) < 0.001) return null;
                if (lat < -2 && lat > -20 && lon < -30 && lon > -50) return Create("Local do Projeto", "BR", 34, 75, "Coordenadas do Site Location - região tropical/NE aproximada");
                if (lat <= -20 && lon < -35 && lon > -55) return Create("Local do Projeto", "BR", 33, 60, "Coordenadas do Site Location - região sudeste/sul aproximada");
                return Create("Local do Projeto", "", settings.DefaultExternalTempC, settings.DefaultExternalRelativeHumidity, "Coordenadas do Site Location - padrão configurado");
            }
            catch { return null; }
        }

        private ClimateData FromCity(string city, string state, HvacSettings settings)
        {
            string key = Normalize(city + " " + state);
            if (key.Contains("salvador") || key.Contains("bahia") || key.Contains(" ba")) return Create("Salvador", "BA", 34, 75, "Tabela interna Notus - Salvador/BA");
            if (key.Contains("rio de janeiro") || key.Contains(" rj")) return Create("Rio de Janeiro", "RJ", 35, 70, "Tabela interna Notus - Rio de Janeiro/RJ");
            if (key.Contains("sao paulo") || key.Contains("são paulo") || key.Contains(" sp")) return Create("São Paulo", "SP", 32, 60, "Tabela interna Notus - São Paulo/SP");
            if (key.Contains("brasilia") || key.Contains("brasília") || key.Contains(" df")) return Create("Brasília", "DF", 33, 45, "Tabela interna Notus - Brasília/DF");
            if (key.Contains("recife") || key.Contains(" pe")) return Create("Recife", "PE", 34, 78, "Tabela interna Notus - Recife/PE");
            if (key.Contains("fortaleza") || key.Contains(" ce")) return Create("Fortaleza", "CE", 35, 72, "Tabela interna Notus - Fortaleza/CE");
            if (key.Contains("manaus") || key.Contains(" am")) return Create("Manaus", "AM", 35, 82, "Tabela interna Notus - Manaus/AM");
            if (key.Contains("belo horizonte") || key.Contains(" bh") || key.Contains(" mg")) return Create("Belo Horizonte", "MG", 32, 55, "Tabela interna Notus - Belo Horizonte/MG");
            if (key.Contains("curitiba") || key.Contains(" pr")) return Create("Curitiba", "PR", 30, 60, "Tabela interna Notus - Curitiba/PR");
            if (key.Contains("porto alegre") || key.Contains(" rs")) return Create("Porto Alegre", "RS", 32, 58, "Tabela interna Notus - Porto Alegre/RS");
            string displayCity = string.IsNullOrWhiteSpace(city) ? settings.DefaultCity : city;
            return Create(displayCity, state, settings.DefaultExternalTempC, settings.DefaultExternalRelativeHumidity, "Padrão configurável - informe cidade/UF no projeto para refinar");
        }

        private ClimateData Create(string city, string state, double externalDryBulb, double rh, string source)
        {
            return new ClimateData
            {
                City = city,
                State = state ?? "",
                Source = source,
                ExternalDryBulbC = externalDryBulb,
                ExternalWetBulbC = Math.Max(20, externalDryBulb - 7),
                ExternalRelativeHumidity = rh,
                InternalDesignTempC = 24,
                InternalRelativeHumidity = 50,
                NormativeReference = "ABNT NBR 16401 / ASHRAE 62.1 - dados climáticos parametrizados para pré-dimensionamento; validar no projeto executivo"
            };
        }

        private string GetProjectText(Document doc, params string[] names)
        {
            if (doc == null || doc.ProjectInformation == null) return "";
            foreach (string name in names)
            {
                Parameter parameter = doc.ProjectInformation.LookupParameter(name);
                if (parameter == null || !parameter.HasValue) continue;
                string value = parameter.StorageType == StorageType.String ? parameter.AsString() : parameter.AsValueString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            return "";
        }

        private string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string normalized = text.ToLowerInvariant();
            normalized = normalized.Replace("á", "a").Replace("à", "a").Replace("â", "a").Replace("ã", "a");
            normalized = normalized.Replace("é", "e").Replace("ê", "e");
            normalized = normalized.Replace("í", "i");
            normalized = normalized.Replace("ó", "o").Replace("ô", "o").Replace("õ", "o");
            normalized = normalized.Replace("ú", "u").Replace("ç", "c");
            return normalized;
        }
    }
}


