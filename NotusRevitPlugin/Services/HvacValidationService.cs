using System;
using System.Collections.Generic;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Services
{
    public static class HvacValidationService
    {
        public static List<string> Validate(RoomInput input, HvacResult result)
        {
            List<string> alerts = new List<string>();
            if (input == null)
            {
                alerts.Add("Entrada de ambiente indisponivel");
                return alerts;
            }

            if (input.AreaM2 <= 0) alerts.Add("Area zerada ou ausente");
            if (input.VolumeM3 <= 0) alerts.Add("Volume zerado ou ausente");
            if (input.AreaM2 > 0 && input.AreaM2 < 2) alerts.Add("Area muito pequena para ambiente HVAC");
            if (input.AreaM2 > 1000) alerts.Add("Area muito alta; conferir se o elemento representa um unico ambiente");
            if (input.HeightM > 0 && input.HeightM < 2.20) alerts.Add("Pe-direito muito baixo");
            if (input.HeightM > 6.0) alerts.Add("Pe-direito muito alto; conferir volume e tipo de ambiente");

            if (input.AreaM2 > 0 && input.HeightM > 0 && input.VolumeM3 > 0)
            {
                double expectedVolume = input.AreaM2 * input.HeightM;
                double ratio = Math.Abs(input.VolumeM3 - expectedVolume) / Math.Max(1, expectedVolume);
                if (ratio > 0.20) alerts.Add("Volume diferente de area x pe-direito em mais de 20%");
            }

            if (input.Occupants <= 0) alerts.Add("Ocupacao nao informada; criterio padrao sera usado no calculo");
            if (input.Occupants > 0 && input.AreaM2 > 0)
            {
                double areaPerPerson = input.AreaM2 / input.Occupants;
                if (areaPerPerson < 1.0) alerts.Add("Densidade de ocupacao muito alta");
                if (areaPerPerson > 80.0) alerts.Add("Densidade de ocupacao muito baixa");
            }

            if (input.LightingWatts <= 0) alerts.Add("Iluminacao nao informada; criterio padrao pode dominar a carga");
            if (input.EquipmentWatts <= 0) alerts.Add("Equipamentos nao informados; criterio padrao pode dominar a carga");

            if (result != null)
            {
                if (result.TotalBTUh > 250000) alerts.Add("BTU/h muito alto; conferir se ha agregacao de ambientes");
                if (result.TotalTR > 20) alerts.Add("TR muito alto para ambiente individual");
                if (input.AreaM2 > 0)
                {
                    double btuPerM2 = result.TotalBTUh / input.AreaM2;
                    if (btuPerM2 < 250) alerts.Add("Carga por m2 muito baixa; conferir dados internos e envoltoria");
                    if (btuPerM2 > 1600) alerts.Add("Carga por m2 muito alta; conferir orientacao, ocupacao e geometria");
                }
                if (result.AirChangesHour < 0.5 || result.AirChangesHour > 30) alerts.Add("ACH fora de faixa comum");
                if (result.ExternalAirflowM3h < 10 && input.AreaM2 > 0) alerts.Add("Vazao externa muito baixa");
                if (result.SHR < 0.55 || result.SHR > 1.00) alerts.Add("SHR fora de faixa comum");
            }

            if (input.IsIfcFallback) alerts.Add("Origem IFC/manual: conferir area, altura e limites do ambiente");
            if (input.IsLinkedElement) alerts.Add("Ambiente de vinculo: calculado para revisao, sem gravar parametros no arquivo vinculado");
            if (!string.IsNullOrWhiteSpace(input.DataQualityNote)) alerts.Add(input.DataQualityNote);

            return Deduplicate(alerts);
        }

        private static List<string> Deduplicate(List<string> items)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string item in items)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                if (seen.Add(item)) result.Add(item);
            }
            return result;
        }
    }
}
