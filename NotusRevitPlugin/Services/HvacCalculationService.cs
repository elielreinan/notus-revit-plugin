using System;
using System.Globalization;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Services
{
    public class HvacCalculationService
    {
        private const double WattToBTUh = 3.412141633;
        private const double LsToM3h = 3.6;
        private const double BTUhPerTR = 12000.0;
        private const double BTUhToKw = 0.00029307107;
        private const double AirDensityKgM3 = 1.20;
        private const double AirCpKjKgK = 1.006;

        public HvacResult Calculate(RoomInput input, string methodPreset)
        {
            HvacSettings settings = HvacSettingsService.Load();
            if (string.IsNullOrWhiteSpace(methodPreset)) methodPreset = settings.DefaultNorm;
            if (string.IsNullOrWhiteSpace(input.EnvironmentType)) input.EnvironmentType = settings.DefaultEnvironmentType;

            HvacCalculationProfile profile = BuildProfile(methodPreset, input.EnvironmentType, settings);
            EnvironmentTypeCriteria criteria = HvacCriteriaService.Get(input.EnvironmentType);

            double areaM2 = Math.Max(0, input.AreaM2);
            double heightM = input.HeightM > 0 ? input.HeightM : 2.8;
            double volumeM3 = input.VolumeM3 > 0 ? input.VolumeM3 : areaM2 * heightM;
            int occupants = input.Occupants > 0 ? input.Occupants : EstimateOccupants(areaM2, input.EnvironmentType, settings);

            double internalTempC = input.InternalTempC > 0 ? input.InternalTempC : (criteria.InternalTempC > 0 ? criteria.InternalTempC : settings.DefaultInternalTempC);
            double externalTempC = input.ExternalTempC > 0 ? input.ExternalTempC : settings.DefaultExternalTempC;
            double internalRh = Clamp(input.InternalRelativeHumidity > 0 ? input.InternalRelativeHumidity : (criteria.InternalRh > 0 ? criteria.InternalRh : settings.DefaultInternalRelativeHumidity), 35, 65);
            double externalRh = Clamp(input.ExternalRelativeHumidity > 0 ? input.ExternalRelativeHumidity : settings.DefaultExternalRelativeHumidity, 35, 95);
            double supplyAirTempC = profile.SupplyAirTempC;

            double baseAreaLoad = areaM2 * GetAreaLoadBTUhM2(input.EnvironmentType, profile);
            baseAreaLoad *= GetTemperatureCorrection(internalTempC, externalTempC);

            double orientationCorrection = GetSolarOrientationCorrection(input.SolarOrientation);
            double envelopeBTUh = baseAreaLoad * 0.55;
            double solarBTUh = baseAreaLoad * 0.25 * orientationCorrection;
            double wallRoofBTUh = baseAreaLoad * 0.20;
            double infiltrationBTUh = Math.Max(0, volumeM3 * Math.Max(0, externalTempC - internalTempC) * 0.60);

            double peopleSensible = occupants * profile.SensibleBTUhPerPerson;
            double peopleLatent = occupants * profile.LatentBTUhPerPerson;
            double lightingWatts = input.LightingWatts > 0 ? input.LightingWatts : areaM2 * (criteria.LightingWPerM2 > 0 ? criteria.LightingWPerM2 : settings.DefaultLightingWattsPerM2);
            double equipmentWatts = input.EquipmentWatts > 0 ? input.EquipmentWatts : areaM2 * (criteria.EquipmentWPerM2 > 0 ? criteria.EquipmentWPerM2 : settings.DefaultEquipmentWattsPerM2);
            double lightingSensible = Math.Max(0, lightingWatts) * WattToBTUh;
            double equipmentSensible = Math.Max(0, equipmentWatts) * WattToBTUh;

            double externalM3hByNorm = ((profile.RpLsPerPerson * occupants) + (profile.RaLsPerM2 * areaM2)) * LsToM3h;
            double externalM3hByAch = volumeM3 > 0 ? profile.MinimumAch * volumeM3 : 0;
            double externalM3h = Math.Max(externalM3hByNorm, externalM3hByAch);

            double ventilationSensible = GetVentilationSensibleBTUh(externalM3h, internalTempC, externalTempC);
            double humidityLatent = GetLatentAreaAllowanceBTUh(areaM2, input.EnvironmentType, internalTempC, externalTempC, internalRh, externalRh);
            double ventilationLatent = GetVentilationLatentBTUh(externalM3h, internalRh, externalRh, input.EnvironmentType);

            double sensibleBTUh = envelopeBTUh + solarBTUh + wallRoofBTUh + infiltrationBTUh + peopleSensible + lightingSensible + equipmentSensible + ventilationSensible;
            double latentBTUh = peopleLatent + humidityLatent + ventilationLatent;

            double totalBTUh = (sensibleBTUh + latentBTUh) * profile.SafetyFactor;
            sensibleBTUh *= profile.SafetyFactor;
            latentBTUh *= profile.SafetyFactor;

            double totalTR = totalBTUh / BTUhPerTR;
            double sensibleTR = sensibleBTUh / BTUhPerTR;
            double shr = totalBTUh > 0 ? sensibleBTUh / totalBTUh : 0;

            double deltaTC = Math.Max(6, internalTempC - supplyAirTempC);
            double sensibleKw = sensibleBTUh * BTUhToKw;
            double supplyM3h = sensibleKw / (AirDensityKgM3 * AirCpKjKgK * deltaTC) * 3600.0;
            supplyM3h = Math.Max(supplyM3h, externalM3h);

            double ach = volumeM3 > 0 ? supplyM3h / volumeM3 : 0;
            string memorial = BuildMemorial(areaM2, occupants, envelopeBTUh, solarBTUh, wallRoofBTUh, infiltrationBTUh, peopleSensible, peopleLatent, lightingSensible, equipmentSensible, ventilationSensible, ventilationLatent, humidityLatent, externalM3hByNorm, externalM3hByAch, profile);

            return new HvacResult
            {
                TotalBTUh = Round(totalBTUh, 0),
                TotalTR = Round(totalTR, 2),
                SensibleBTUh = Round(sensibleBTUh, 0),
                LatentBTUh = Round(latentBTUh, 0),
                SensibleTR = Round(sensibleTR, 2),
                SHR = Round(shr, 2),
                SupplyAirflowM3h = Round(supplyM3h, 0),
                ExternalAirflowM3h = Round(externalM3h, 0),
                AirChangesHour = Round(ach, 2),
                PeopleSensibleBTUh = Round(peopleSensible * profile.SafetyFactor, 0),
                PeopleLatentBTUh = Round(peopleLatent * profile.SafetyFactor, 0),
                LightingBTUh = Round(lightingSensible * profile.SafetyFactor, 0),
                EquipmentBTUh = Round(equipmentSensible * profile.SafetyFactor, 0),
                EnvelopeBTUh = Round(envelopeBTUh * profile.SafetyFactor, 0),
                SolarBTUh = Round(solarBTUh * profile.SafetyFactor, 0),
                WallRoofBTUh = Round(wallRoofBTUh * profile.SafetyFactor, 0),
                InfiltrationBTUh = Round(infiltrationBTUh * profile.SafetyFactor, 0),
                VentilationSensibleBTUh = Round(ventilationSensible * profile.SafetyFactor, 0),
                VentilationLatentBTUh = Round(ventilationLatent * profile.SafetyFactor, 0),
                HumidityLatentBTUh = Round(humidityLatent * profile.SafetyFactor, 0),
                ExternalByNormM3h = Round(externalM3hByNorm, 0),
                ExternalByAchM3h = Round(externalM3hByAch, 0),
                EquipmentSelected = SelectEquipment(totalBTUh),
                Method = profile.Name,
                ClimateSummary = BuildClimateSummary(input, externalTempC, externalRh),
                InternalExternalSummary = "Interno " + FormatNumber(internalTempC, 1) + " °C / UR " + FormatNumber(internalRh, 0) + "% | Externo " + FormatNumber(externalTempC, 1) + " °C / UR " + FormatNumber(externalRh, 0) + "%",
                NormativeReference = string.IsNullOrWhiteSpace(input.NormativeReference) ? profile.NormativeReference : input.NormativeReference,
                Notes = BuildNotes(input, occupants, profile, externalM3hByNorm, externalM3hByAch),
                MemorialSummary = memorial
            };
        }

        public static string FormatNumber(double value, int decimals = 2)
        {
            return value.ToString("N" + decimals, new CultureInfo("pt-BR"));
        }

        private HvacCalculationProfile BuildProfile(string methodPreset, string environmentType, HvacSettings settings)
        {
            string preset = Normalize(methodPreset);
            HvacCalculationProfile profile = new HvacCalculationProfile();

            if (preset.Contains("ashrae"))
            {
                profile.Name = "ASHRAE simplificado";
                profile.DefaultAreaLoadBTUhM2 = 580;
                profile.SensibleBTUhPerPerson = 245;
                profile.LatentBTUhPerPerson = 205;
                profile.RpLsPerPerson = 2.5;
                profile.RaLsPerM2 = 0.30;
                profile.SafetyFactor = 1.08;
                profile.MinimumAch = 2.0;
                profile.NormativeReference = "ASHRAE 62.1 / pré-dimensionamento parametrizado";
            }
            else if (preset.Contains("conservador"))
            {
                profile.Name = "Conservador";
                profile.DefaultAreaLoadBTUhM2 = 700;
                profile.SensibleBTUhPerPerson = 275;
                profile.LatentBTUhPerPerson = 230;
                profile.RpLsPerPerson = 3.8;
                profile.RaLsPerM2 = 0.45;
                profile.SafetyFactor = 1.18;
                profile.MinimumAch = 3.0;
                profile.NormativeReference = "Preset conservador interno Notus; validar com normas aplicáveis";
            }
            else
            {
                profile.Name = "ABNT simplificado";
                profile.DefaultAreaLoadBTUhM2 = 620;
                profile.SensibleBTUhPerPerson = 245;
                profile.LatentBTUhPerPerson = 205;
                profile.RpLsPerPerson = 2.5;
                profile.RaLsPerM2 = 0.30;
                profile.SafetyFactor = 1.10;
                profile.MinimumAch = 2.0;
                profile.NormativeReference = "ABNT NBR 16401 - pré-dimensionamento parametrizado";
            }

            EnvironmentTypeCriteria criteria = HvacCriteriaService.Get(environmentType);
            if (criteria.RenewalLsPerPerson >= 0) profile.RpLsPerPerson = criteria.RenewalLsPerPerson;
            if (criteria.RenewalLsPerM2 >= 0) profile.RaLsPerM2 = criteria.RenewalLsPerM2;
            if (criteria.MinimumAch > 0) profile.MinimumAch = criteria.MinimumAch;
            if (criteria.SafetyFactor > 0) profile.SafetyFactor = Math.Max(profile.SafetyFactor, criteria.SafetyFactor);

            ApplyEnvironmentAdjustments(profile, environmentType);
            return profile;
        }

        private void ApplyEnvironmentAdjustments(HvacCalculationProfile profile, string environmentType)
        {
            string env = Normalize(environmentType);
            if (env.Contains("hospital") || env.Contains("exame") || env.Contains("clinica") || env.Contains("saude"))
            {
                profile.DefaultAreaLoadBTUhM2 += 180;
                profile.RpLsPerPerson = Math.Max(profile.RpLsPerPerson, 5.0);
                profile.RaLsPerM2 = Math.Max(profile.RaLsPerM2, 0.60);
                profile.MinimumAch = Math.Max(profile.MinimumAch, 6.0);
            }
            else if (env.Contains("sala tecnica") || env.Contains("sala técnica") || env.Contains("data") || env.Contains("servidor"))
            {
                profile.DefaultAreaLoadBTUhM2 += 320;
                profile.RaLsPerM2 = Math.Max(profile.RaLsPerM2, 0.20);
                profile.MinimumAch = Math.Max(profile.MinimumAch, 2.0);
            }
            else if (env.Contains("reuniao") || env.Contains("reunião") || env.Contains("meeting"))
            {
                profile.DefaultAreaLoadBTUhM2 += 80;
                profile.RpLsPerPerson = Math.Max(profile.RpLsPerPerson, 3.8);
            }
            else if (env.Contains("aula") || env.Contains("class"))
            {
                profile.DefaultAreaLoadBTUhM2 += 60;
                profile.RpLsPerPerson = Math.Max(profile.RpLsPerPerson, 3.8);
            }
            else if (env.Contains("loja") || env.Contains("retail") || env.Contains("comercial"))
            {
                profile.DefaultAreaLoadBTUhM2 += 120;
                profile.RpLsPerPerson = Math.Max(profile.RpLsPerPerson, 3.8);
                profile.RaLsPerM2 = Math.Max(profile.RaLsPerM2, 0.45);
            }
            else if (env.Contains("restaurante") || env.Contains("cozinha") || env.Contains("restaurant"))
            {
                profile.DefaultAreaLoadBTUhM2 += 250;
                profile.RpLsPerPerson = Math.Max(profile.RpLsPerPerson, 5.0);
                profile.RaLsPerM2 = Math.Max(profile.RaLsPerM2, 0.90);
                profile.MinimumAch = Math.Max(profile.MinimumAch, 8.0);
            }
            else if (env.Contains("resid") || env.Contains("apart") || env.Contains("quarto"))
            {
                profile.DefaultAreaLoadBTUhM2 = Math.Min(profile.DefaultAreaLoadBTUhM2, 520);
                profile.RaLsPerM2 = Math.Min(profile.RaLsPerM2, 0.25);
            }
        }

        private double GetAreaLoadBTUhM2(string environmentType, HvacCalculationProfile profile)
        {
            return Math.Max(350, profile.DefaultAreaLoadBTUhM2);
        }

        private int EstimateOccupants(double areaM2, string environmentType, HvacSettings settings)
        {
            if (areaM2 <= 0) return 1;
            string env = Normalize(environmentType);
            EnvironmentTypeCriteria criteria = HvacCriteriaService.Get(environmentType);
            double densityM2PerPerson = criteria.M2PerPerson > 0 ? criteria.M2PerPerson : (settings.DefaultOccupancyM2PerPerson > 0 ? settings.DefaultOccupancyM2PerPerson : 10);
            if (env.Contains("hospital") || env.Contains("exame") || env.Contains("clinica")) densityM2PerPerson = 8;
            else if (env.Contains("reuniao") || env.Contains("reunião") || env.Contains("meeting")) densityM2PerPerson = 2.5;
            else if (env.Contains("aula") || env.Contains("class")) densityM2PerPerson = 2.0;
            else if (env.Contains("loja") || env.Contains("retail") || env.Contains("comercial")) densityM2PerPerson = 4.0;
            else if (env.Contains("restaurante") || env.Contains("restaurant")) densityM2PerPerson = 1.5;
            else if (env.Contains("sala tecnica") || env.Contains("sala técnica") || env.Contains("servidor")) densityM2PerPerson = 20.0;
            else if (env.Contains("resid") || env.Contains("quarto") || env.Contains("apart")) densityM2PerPerson = 12.0;
            return Math.Max(1, (int)Math.Ceiling(areaM2 / densityM2PerPerson));
        }

        private double GetTemperatureCorrection(double internalTempC, double externalTempC)
        {
            double delta = Math.Max(0, externalTempC - internalTempC);
            double correction = 1.0 + Math.Max(0, delta - 8) * 0.015;
            return Math.Min(1.40, correction);
        }

        private double GetSolarOrientationCorrection(string orientation)
        {
            string text = Normalize(orientation);
            if (text.Contains("oeste") || text.Contains("west")) return 1.20;
            if (text.Contains("norte") || text.Contains("north")) return 1.15;
            if (text.Contains("leste") || text.Contains("east")) return 1.10;
            if (text.Contains("sul") || text.Contains("south")) return 1.05;
            return 1.10;
        }

        private double GetVentilationSensibleBTUh(double externalM3h, double internalTempC, double externalTempC)
        {
            double delta = Math.Max(0, externalTempC - internalTempC);
            return externalM3h * delta * 1.144;
        }

        private double GetVentilationLatentBTUh(double externalM3h, double internalRh, double externalRh, string environmentType)
        {
            double rhDelta = Math.Max(0, externalRh - internalRh);
            double latent = externalM3h * rhDelta * 1.65;
            string env = Normalize(environmentType);
            if (env.Contains("cozinha") || env.Contains("restaurante")) latent *= 1.25;
            if (env.Contains("hospital") || env.Contains("exame") || env.Contains("clinica")) latent *= 1.15;
            return latent;
        }

        private double GetLatentAreaAllowanceBTUh(double areaM2, string environmentType, double internalTempC, double externalTempC, double internalRh, double externalRh)
        {
            if (externalTempC <= internalTempC && externalRh <= internalRh) return 0;
            string env = Normalize(environmentType);
            double latentPerM2 = 35;
            if (env.Contains("hospital") || env.Contains("exame") || env.Contains("clinica")) latentPerM2 = 75;
            else if (env.Contains("restaurante") || env.Contains("cozinha")) latentPerM2 = 95;
            else if (env.Contains("loja") || env.Contains("comercial")) latentPerM2 = 55;
            return Math.Max(0, areaM2 * latentPerM2);
        }

        private string SelectEquipment(double totalBTUh)
        {
            if (totalBTUh <= 0) return "Não definido";
            double[] options = new double[] { 9000, 12000, 18000, 24000, 30000, 36000, 48000, 60000, 72000, 90000, 120000, 180000, 240000 };
            foreach (double option in options)
            {
                if (totalBTUh <= option) return FormatNumber(option, 0) + " BTU/h";
            }
            return FormatNumber(Math.Ceiling(totalBTUh / 12000.0), 0) + " TR modular";
        }

        private string BuildClimateSummary(RoomInput input, double externalTempC, double externalRh)
        {
            string local = string.IsNullOrWhiteSpace(input.ProjectCity) ? "Local não informado" : input.ProjectCity;
            if (!string.IsNullOrWhiteSpace(input.ProjectState)) local += "/" + input.ProjectState;
            return local + " | " + FormatNumber(externalTempC, 1) + " °C externo | UR " + FormatNumber(externalRh, 0) + "% | " + input.ClimateSource;
        }

        private string BuildNotes(RoomInput input, int occupants, HvacCalculationProfile profile, double externalM3hByNorm, double externalM3hByAch)
        {
            string note = "Perfil técnico baseado em critérios editáveis. Pré-dimensionamento. Ocupantes considerados: " + occupants + ". Renovação por pessoa/área: " + FormatNumber(externalM3hByNorm, 0) + " m³/h; mínimo por ACH: " + FormatNumber(externalM3hByAch, 0) + " m³/h.";
            if (input.IsIfcFallback)
            {
                note += " IFC/manual: ambiente estimado por geometria/DirectShape; conferir área, altura e limites.";
            }
            if (!string.IsNullOrWhiteSpace(input.DataQualityNote))
            {
                note += " " + input.DataQualityNote;
            }
            note += " Validar cargas, critérios e norma aplicável com o responsável técnico.";
            return note;
        }

        private string BuildMemorial(double areaM2, int occupants, double envelopeBTUh, double solarBTUh, double wallRoofBTUh, double infiltrationBTUh, double peopleSensible, double peopleLatent, double lightingSensible, double equipmentSensible, double ventilationSensible, double ventilationLatent, double humidityLatent, double externalByNorm, double externalByAch, HvacCalculationProfile profile)
        {
            return "Área " + FormatNumber(areaM2, 2) + " m²; ocupantes " + occupants +
                "; envoltória " + FormatNumber(envelopeBTUh, 0) + " BTU/h" +
                "; insolação/janelas " + FormatNumber(solarBTUh, 0) + " BTU/h" +
                "; paredes/cobertura " + FormatNumber(wallRoofBTUh, 0) + " BTU/h" +
                "; infiltração " + FormatNumber(infiltrationBTUh, 0) + " BTU/h" +
                "; pessoas sensível/latente " + FormatNumber(peopleSensible, 0) + "/" + FormatNumber(peopleLatent, 0) + " BTU/h" +
                "; iluminação " + FormatNumber(lightingSensible, 0) + " BTU/h" +
                "; equipamentos " + FormatNumber(equipmentSensible, 0) + " BTU/h" +
                "; renovação sensível/latente " + FormatNumber(ventilationSensible, 0) + "/" + FormatNumber(ventilationLatent, 0) + " BTU/h" +
                "; umidade latente " + FormatNumber(humidityLatent, 0) + " BTU/h" +
                "; ar externo norma/ACH " + FormatNumber(externalByNorm, 0) + "/" + FormatNumber(externalByAch, 0) + " m³/h" +
                "; fator segurança " + FormatNumber(profile.SafetyFactor, 2) + ".";
        }

        private double Clamp(double value, double min, double max) { return Math.Max(min, Math.Min(max, value)); }
        private double Round(double value, int decimals) { return Math.Round(value, decimals, MidpointRounding.AwayFromZero); }
        private string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string value = text.ToLowerInvariant();
            value = value.Replace("á", "a").Replace("à", "a").Replace("â", "a").Replace("ã", "a");
            value = value.Replace("é", "e").Replace("ê", "e");
            value = value.Replace("í", "i");
            value = value.Replace("ó", "o").Replace("ô", "o").Replace("õ", "o");
            value = value.Replace("ú", "u").Replace("ç", "c");
            return value;
        }
    }
}


