Notus - Revit 2025
=======================

Notus e um add-in para pre-dimensionamento HVAC no Autodesk Revit 2025.

Marca: Notus
Versao: 1.0.0
Arquivo principal: NotusRevitPlugin.dll
Aba no Revit: Notus
Instalador: Instalador_Notus_Revit2025.exe

Recursos principais
-------------------
- Leitura de Rooms e MEP Spaces do modelo ativo.
- Leitura de ambientes em arquivos vinculados carregados.
- Diagnostico IFC com contagem de Rooms, Spaces, IfcSpaces, links e elementos IFC.
- Assistente IFC para criar Ambientes Notus manuais quando o IFC veio sem IfcSpace.
- Calculo de BTU/h, TR, SHR, vazao insuflada, vazao externa e ACH.
- Alertas tecnicos para area, volume, pe-direito, ocupacao, carga por m2, IFC e vinculos.
- Exportacao CSV e Excel.
- Memorial PDF Notus com capa, resumo, premissas, tabela por ambiente e alertas.
- Logs e diagnostico para suporte.

Fluxo recomendado
-----------------
1. Instale pelo EXE e reinicie o Revit 2025.
2. Abra o projeto e va ate a aba Notus.
3. Use Ambientes > Ler Ambientes.
4. Se o IFC nao tiver ambientes, use Ambientes > Assistente IFC.
5. Revise Ambientes Notus manuais quando necessario.
6. Use Parametros > Criar Parametros ou clique direto em Calcular HVAC.
7. Revise os alertas antes de gravar.
8. Exporte CSV, Excel ou Memorial PDF.

Observacao tecnica
------------------
Os resultados sao estimativos para pre-dimensionamento. Validar criterios, normas, cargas internas, envoltoria, renovacao de ar e resultados com o responsavel tecnico antes de emissao final ou uso em obra.

Compatibilidade
---------------
Os parametros internos Notus_* foram preservados para compatibilidade com projetos ja calculados em versoes anteriores.
