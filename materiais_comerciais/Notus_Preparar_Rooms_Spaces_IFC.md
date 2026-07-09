# Notus - Preparar Rooms, Spaces e IFC

## Melhor caso
O modelo tem Rooms ou MEP Spaces com area valida. Use:
1. Ambientes > Ler Ambientes.
2. Parametros > Criar Parametros.
3. Calculo HVAC > Calcular HVAC.

## Modelo com vinculos
O Notus le ambientes em vinculos carregados. Esses ambientes podem entrar na revisao/calculo, mas resultados so sao gravados em elementos editaveis do modelo ativo.

## IFC com IfcSpace
Quando o IFC veio com IfcSpace, o Notus tenta detectar ambientes equivalentes automaticamente.

## IFC sem IfcSpace
Quando o IFC veio sem ambientes exportados:
1. Use Ambientes > Diagnostico IFC.
2. Use Ambientes > Assistente IFC.
3. Crie Ambientes Notus manuais a partir de pisos/lajes/elementos IFC.
4. Revise nome, area, pe-direito, ocupacao e tipo de uso.
5. Calcule normalmente.

## Recomendacao de exportacao IFC
Sempre que possivel, exporte zonas/ambientes como IfcSpace. Isso melhora leitura automatica, diagnostico e confiabilidade do calculo.
