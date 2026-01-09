# FileSearchEngine 🔍

Um mecanismo de busca de arquivos desenvolvido em ASP.NET Core 8.0 que permite indexar e pesquisar o conteúdo de documentos de texto.

## 📋 Funcionalidades

- Upload de Arquivos: Envio de arquivos de texto (.txt) para o servidor.
- Indexação: Processamento automático do conteúdo dos arquivos para torná-los pesquisáveis.
- Busca por Conteúdo: Pesquisa por palavras-chave dentro dos arquivos, não apenas pelo nome.
- Download: Recuperação do arquivo original a partir dos resultados da busca.

## 🚀 Como Rodar

### Pré-requisitos
- .NET 8.0 SDK (https://dotnet.microsoft.com/download/dotnet/8.0) instalado.

### Passo a Passo

1. Clone este repositório:
``
git clone https://github.com/seu-usuario/file-search-engine.git
``
2. Localize a pasta do projeto:
``
cd file-search-engine
``
3. Restaure as dependências:
``
dotnet restore
``

4. Execute a aplicação:
``
dotnet run --project FileSearchEngine
``

7. Acesse no navegador:
   O sistema estará rodando em http://localhost:5056.

## 💻 Como Usar

1. Acesse a página inicial.
2. Clique no botão "Upload Files" e selecione seus arquivos .txt.
3. Aguarde a confirmação de upload.
4. Digite uma palavra ou frase na barra de busca para encontrar em quais arquivos ela aparece.

## 🛠 Tecnologias

- Linguagem: C#
- Framework: .NET 8.0 (ASP.NET Core)
- Frontend: Razor Pages, HTML, CSS, JavaScript

---
Desenvolvido para a disciplina de Análise e Projeto de Sistemas (2025.2).
