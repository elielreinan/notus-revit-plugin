# 🚀 Notus Revit Plugin

> Add-in para Autodesk Revit 2025 que auxilia no pré-dimensionamento de sistemas HVAC, na análise de ambientes e na geração de relatórios técnicos.

> ⚠️ **Projeto pausado.** Não há previsão de novas versões.

![License](https://img.shields.io/badge/license-n%C3%A3o_especificada-lightgrey.svg)
![Status](https://img.shields.io/badge/status-projeto_pausado-red.svg)
![Version](https://img.shields.io/badge/version-1.0.0-orange.svg)

---

## 📌 Índice
- [Visão Geral](#-visão-geral)
- [Tecnologias](#-tecnologias)
- [Como Executar](#-como-executar)
- [Como Contribuir](#-como-contribuir)
- [Licença](#-licença)

---

## 💡 Visão Geral

O Notus integra ferramentas de análise e pré-dimensionamento HVAC ao Autodesk Revit 2025, ajudando a revisar ambientes do modelo e documentar resultados. **O desenvolvimento do projeto está pausado.**

- ✨ **Análise de ambientes:** leitura de Rooms, MEP Spaces e ambientes em modelos vinculados.
- ⚡ **Pré-dimensionamento HVAC:** estimativas de carga térmica, vazões e indicadores técnicos, com alertas para revisão.
- 🎨 **Relatórios e exportação:** exportação de resultados para CSV, Excel e memorial em PDF.
- 🔎 **Suporte a IFC:** diagnóstico de ambientes IFC e assistente para criar ambientes Notus manuais.

Os resultados são estimativas para pré-dimensionamento e devem ser avaliados pelo responsável técnico antes de qualquer uso em obra.

---

## 🛠️ Tecnologias

![C#](https://img.shields.io/badge/C%23-68217A?style=for-the-badge&logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Autodesk Revit](https://img.shields.io/badge/Autodesk_Revit-0696D7?style=for-the-badge&logo=autodesk&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)

---

## 🚀 Como Executar

### Pré-requisitos

- Windows
- Autodesk Revit 2025
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)

O projeto referencia `RevitAPI.dll` e `RevitAPIUI.dll` no caminho padrão de instalação do Revit 2025 (`C:\Program Files\Autodesk\Revit 2025`).

### 🔧 Passo a Passo

```powershell
# 1. Clone este repositório
git clone https://github.com/elielreinan/notus-revit-plugin.git

# 2. Acesse a pasta do projeto
cd notus-revit-plugin

# 3. Gere o plugin e o instalador
.\build_instalador_profissional.bat
```

O instalador e os arquivos para instalação manual serão gerados na pasta `dist`. Execute o instalador e reinicie o Revit 2025 para carregar o add-in.

---

## 🤝 Como Contribuir

Como o projeto está pausado, não há previsão de novas versões. Caso queira propor uma alteração, abra uma issue ou pull request descrevendo o objetivo e como validou a mudança.

---

## 📄 Licença

Este repositório não especifica uma licença de uso. Consulte os responsáveis pelo projeto antes de reutilizar, modificar ou distribuir o código.
