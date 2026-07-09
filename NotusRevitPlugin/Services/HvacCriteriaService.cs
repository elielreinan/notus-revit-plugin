using System;
using System.Collections.Generic;
using System.Linq;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Services
{
    public static class HvacCriteriaService
    {
        private static readonly List<EnvironmentTypeCriteria> Criteria = new List<EnvironmentTypeCriteria>
        {
            new EnvironmentTypeCriteria { Name = "Escritório", M2PerPerson = 10, LightingWPerM2 = 12, EquipmentWPerM2 = 15, RenewalLsPerPerson = 2.5, RenewalLsPerM2 = 0.30, InternalTempC = 24, InternalRh = 50, SafetyFactor = 1.10, MinimumAch = 2.0 },
            new EnvironmentTypeCriteria { Name = "Hospital", M2PerPerson = 8, LightingWPerM2 = 14, EquipmentWPerM2 = 20, RenewalLsPerPerson = 5.0, RenewalLsPerM2 = 0.60, InternalTempC = 23, InternalRh = 50, SafetyFactor = 1.18, MinimumAch = 6.0 },
            new EnvironmentTypeCriteria { Name = "Loja", M2PerPerson = 4, LightingWPerM2 = 18, EquipmentWPerM2 = 18, RenewalLsPerPerson = 3.8, RenewalLsPerM2 = 0.45, InternalTempC = 24, InternalRh = 50, SafetyFactor = 1.12, MinimumAch = 3.0 },
            new EnvironmentTypeCriteria { Name = "Sala técnica", M2PerPerson = 20, LightingWPerM2 = 8, EquipmentWPerM2 = 80, RenewalLsPerPerson = 2.5, RenewalLsPerM2 = 0.20, InternalTempC = 23, InternalRh = 50, SafetyFactor = 1.20, MinimumAch = 2.0 },
            new EnvironmentTypeCriteria { Name = "Auditório", M2PerPerson = 1.5, LightingWPerM2 = 12, EquipmentWPerM2 = 8, RenewalLsPerPerson = 3.8, RenewalLsPerM2 = 0.40, InternalTempC = 24, InternalRh = 50, SafetyFactor = 1.15, MinimumAch = 4.0 },
            new EnvironmentTypeCriteria { Name = "Restaurante", M2PerPerson = 1.5, LightingWPerM2 = 16, EquipmentWPerM2 = 35, RenewalLsPerPerson = 5.0, RenewalLsPerM2 = 0.90, InternalTempC = 24, InternalRh = 50, SafetyFactor = 1.18, MinimumAch = 8.0 },
            new EnvironmentTypeCriteria { Name = "Sala de aula", M2PerPerson = 2.0, LightingWPerM2 = 12, EquipmentWPerM2 = 10, RenewalLsPerPerson = 3.8, RenewalLsPerM2 = 0.35, InternalTempC = 24, InternalRh = 50, SafetyFactor = 1.12, MinimumAch = 4.0 },
            new EnvironmentTypeCriteria { Name = "Banheiro", M2PerPerson = 8, LightingWPerM2 = 10, EquipmentWPerM2 = 5, RenewalLsPerPerson = 0.0, RenewalLsPerM2 = 1.00, InternalTempC = 24, InternalRh = 55, SafetyFactor = 1.10, MinimumAch = 10.0 },
            new EnvironmentTypeCriteria { Name = "Cozinha", M2PerPerson = 3, LightingWPerM2 = 18, EquipmentWPerM2 = 120, RenewalLsPerPerson = 5.0, RenewalLsPerM2 = 1.20, InternalTempC = 24, InternalRh = 55, SafetyFactor = 1.20, MinimumAch = 12.0 },
            new EnvironmentTypeCriteria { Name = "Depósito", M2PerPerson = 30, LightingWPerM2 = 8, EquipmentWPerM2 = 5, RenewalLsPerPerson = 2.5, RenewalLsPerM2 = 0.20, InternalTempC = 25, InternalRh = 55, SafetyFactor = 1.08, MinimumAch = 1.5 }
        };

        public static IEnumerable<string> GetCriteriaNames()
        {
            return Criteria.Select(c => c.Name);
        }

        public static EnvironmentTypeCriteria Get(string environmentType)
        {
            string n = Normalize(environmentType);
            EnvironmentTypeCriteria exact = Criteria.FirstOrDefault(c => Normalize(c.Name) == n);
            if (exact != null) return exact;
            EnvironmentTypeCriteria contains = Criteria.FirstOrDefault(c => n.Contains(Normalize(c.Name)) || Normalize(c.Name).Contains(n));
            return contains ?? Criteria[0];
        }

        private static string Normalize(string text)
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


