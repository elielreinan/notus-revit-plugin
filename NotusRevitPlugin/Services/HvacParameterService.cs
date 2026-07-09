using System;
using System.IO;
using System.Text;
using Autodesk.Revit.DB;

namespace NotusRevitPlugin.Services
{
    public static class HvacParameterService
    {
        private class ParamSpec
        {
            public string Guid { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        private static readonly ParamSpec[] Specs = new ParamSpec[]
        {
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001001", "Notus_Total_BTU", "Carga total BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001002", "Notus_TR", "Carga total TR Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001003", "Notus_Sensible_TR", "Carga sensível TR Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001015", "Notus_Sensible_BTU", "Carga sensível BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001011", "Notus_Latent_BTU", "Carga latente BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001004", "Notus_SHR", "Fator SHR Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001005", "Notus_Supply_m3h", "Vazão insuflada m3/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001006", "Notus_External_m3h", "Vazão externa m3/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001007", "Notus_AirChanges_h", "Trocas de ar por hora Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001008", "Notus_Equipment", "Equipamento sugerido Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001009", "Notus_Method", "Método/preset de cálculo Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001012", "Notus_Climate", "Condição climática considerada Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001013", "Notus_InternalExternal", "Condições internas e externas Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001014", "Notus_NormRef", "Referência normativa/preset Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001010", "Notus_Notes", "Observações técnicas Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001020", "Notus_People_BTU", "Carga por pessoas BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001021", "Notus_Lighting_BTU", "Carga por iluminação BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001022", "Notus_Equipment_BTU", "Carga por equipamentos BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001023", "Notus_Envelope_BTU", "Carga de envoltória BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001024", "Notus_Solar_BTU", "Carga solar/janelas BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001025", "Notus_WallRoof_BTU", "Carga paredes/cobertura BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001026", "Notus_Infiltration_BTU", "Carga de infiltração BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001027", "Notus_Ventilation_BTU", "Carga de renovação/ventilação BTU/h Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001028", "Notus_Memorial", "Memorial resumido de cálculo Notus"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001030", "Notus_Manual_Name", "Nome do ambiente HVAC manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001031", "Notus_Manual_Type", "Tipo do ambiente HVAC manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001032", "Notus_Manual_Area_m2", "Área m2 do ambiente HVAC manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001033", "Notus_Manual_Height_m", "Altura m do ambiente HVAC manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001034", "Notus_Manual_Volume_m3", "Volume m3 do ambiente HVAC manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001035", "Notus_Manual_Occupants", "Ocupantes do ambiente HVAC manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001036", "Notus_Manual_SourceElementId", "Elemento IFC/origem usado no ambiente manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001037", "Notus_Manual_Level", "Pavimento do ambiente HVAC manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001038", "Notus_Manual_InternalTempC", "Temperatura interna do ambiente manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001039", "Notus_Manual_InternalRH", "Umidade interna do ambiente manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001040", "Notus_Manual_Renewal_m3h", "Renovação de ar manual m3/h"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001041", "Notus_Manual_Observation", "Observação técnica do ambiente manual"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001042", "Notus_Manual_SourceCategory", "Categoria do elemento IFC/origem"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001043", "Notus_ValidationAlerts", "Alertas técnicos antes da gravação"),
            P("1f6d8f3e-1a17-4a13-9bb5-7dd770001044", "Notus_CriteriaProfile", "Perfil técnico baseado em critérios editáveis")
        };

        public static readonly string[] ResultParameterNames = Array.ConvertAll(Specs, s => s.Name);

        public static int EnsureParameters(Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            string assemblyFolder = Path.GetDirectoryName(typeof(HvacParameterService).Assembly.Location) ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sharedParameterPath = Path.Combine(assemblyFolder, "Notus_SharedParameters.txt");
            if (!File.Exists(sharedParameterPath)) File.WriteAllText(sharedParameterPath, BuildSharedParameterFileText(), Encoding.UTF8);

            // BUGFIX: app.SharedParametersFilename é uma configuração GLOBAL da sessão do Revit (a mesma
            // que aparece em Gerenciar > Parâmetros Compartilhados). Antes o plugin trocava esse valor e
            // nunca devolvia o arquivo original do usuário, então depois de usar o Notus qualquer
            // outro fluxo do próprio usuário (ou de outro add-in) que dependesse do arquivo de parâmetros
            // compartilhados "de sempre" passava a apontar silenciosamente para o arquivo do Notus.
            // Agora guardamos o valor anterior e restauramos no fim, aconteça o que acontecer.
            string previousSharedParametersFilename = null;
            try { previousSharedParametersFilename = app.SharedParametersFilename; } catch { }

            try
            {
                app.SharedParametersFilename = sharedParameterPath;
                DefinitionFile definitionFile = app.OpenSharedParameterFile();
                if (definitionFile == null) throw new InvalidOperationException("Não foi possível abrir o arquivo de parâmetros compartilhados: " + sharedParameterPath);

                DefinitionGroup group = definitionFile.Groups.get_Item("Notus") ?? definitionFile.Groups.Create("Notus");
                CategorySet categorySet = app.Create.NewCategorySet();
                InsertCategoryIfExists(doc, categorySet, BuiltInCategory.OST_Rooms);
                InsertCategoryIfExists(doc, categorySet, BuiltInCategory.OST_MEPSpaces);
                InsertCategoryIfExists(doc, categorySet, BuiltInCategory.OST_GenericModel);

                InstanceBinding binding = app.Create.NewInstanceBinding(categorySet);
                int confirmed = 0;
                foreach (ParamSpec spec in Specs)
                {
                    Definition definition = group.Definitions.get_Item(spec.Name);
                    if (definition == null)
                    {
                        ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(spec.Name, SpecTypeId.String.Text)
                        {
                            Description = spec.Description
                        };
                        definition = group.Definitions.Create(options);
                    }
                    if (!doc.ParameterBindings.Contains(definition)) doc.ParameterBindings.Insert(definition, binding, GroupTypeId.Mechanical);
                    else doc.ParameterBindings.ReInsert(definition, binding, GroupTypeId.Mechanical);
                    confirmed++;
                }
                return confirmed;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previousSharedParametersFilename))
                {
                    try { app.SharedParametersFilename = previousSharedParametersFilename; } catch { }
                }
            }
        }

        private static void InsertCategoryIfExists(Document doc, CategorySet categorySet, BuiltInCategory builtInCategory)
        {
            try { Category category = doc.Settings.Categories.get_Item(builtInCategory); if (category != null) categorySet.Insert(category); } catch { }
        }

        public static string BuildSharedParameterFileText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# This is a Revit shared parameter file.");
            sb.AppendLine("# Generated by Notus Oficial.");
            sb.AppendLine("*META\tVERSION\tMINVERSION");
            sb.AppendLine("META\t2\t1");
            sb.AppendLine("*GROUP\tID\tNAME");
            sb.AppendLine("GROUP\t1\tNotus");
            sb.AppendLine("*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\tHIDEWHENNOVALUE");
            foreach (ParamSpec spec in Specs)
            {
                sb.AppendLine("PARAM\t" + spec.Guid + "\t" + spec.Name + "\tTEXT\t\t1\t1\t" + spec.Description + "\t1\t0");
            }
            return sb.ToString();
        }

        private static ParamSpec P(string guid, string name, string description)
        {
            return new ParamSpec { Guid = guid, Name = name, Description = description };
        }
    }
}


