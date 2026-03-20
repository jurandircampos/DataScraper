# DataScraper
Sistema automatizado de extração de dados

Este é um ecossistema C# (.NET 8) composto por um robô RPA para capturar dados externos (web scraping) e uma Web API RESTful para expor esses dados.

## 🎯 Visão Geral da Solução

O projeto foi construído separando responsabilidades através de uma Arquitetura em Três Camadas (projetos):

1. **ScraperCore**: Biblioteca de Classes C# (`Class Library`) que contém as entidades de domínio (modelo `Quote`) e a configuração de acesso a dados (Entity Framework Core e SQLite). Age como a camada comum compartilhada entre o Worker e a API.
2. **ScraperWorker**: Aplicativo Console rodando como Background Service (`Worker Service`). Contém a lógica de agendamento (Scraping periódico configurável por `appsettings`) e a lógica do robô utilizando `HtmlAgilityPack` para extração de dados DOM e `Polly` para garantir resiliência contra falhas de rede em requisições HTTP via `HttpClient`.
3. **ScraperApi**: Aplicativo Web (`ASP.NET Core Web API`) com Swagger configurado. Serve exclusivamente para a exposição em formato JSON dos dados pré-processados e armazenados no SQLite pelo RPA.

### Tecnologias Utilizadas
- **Linguagem e Framework**: C# com .NET 8
- **ORM e Banco de Dados**: Entity Framework Core com SQLite (embutido)
- **Web Scraping**: HtmlAgilityPack
- **Resiliência e Políticas de Rede**: Polly (`Microsoft.Extensions.Http.Polly`)
- **Containerização**: Docker e Docker Compose

---

## 🚀 Instruções de Execução (Docker Compose)

A solução foi projetada de forma autossuficiente e containerizada.

1. Tenha o **Docker** e o **Docker Compose** instalados na sua máquina.
2. Clone este repositório e abra um terminal na raiz (`/DataScraper`) onde se encontra o arquivo `docker-compose.yml`.
3. Execute o seguinte comando:
   ```bash
   docker-compose up -d --build
   ```
4. Dois containers serão subidos:
   - `data_scraper_worker`: Começará imediatamente aplicar as *Migrations* do Entity Framework na base de dados (se não existirem) e realizará sua primeira tarefa de varredura.
   - `data_scraper_api`: Estará disponível para receber requisições. O banco de dados fica sincronizado via volume local mapeado para `/app/data/scraper.db`.

5. Acesse a API pelo Swagger através do seu navegador:
   - **http://localhost:8081/swagger**

### Limpando a execução
Para desligar os containers e limpar a infraestrutura:
```bash
docker-compose down
```

---

## 🏗️ Decisões Arquiteturais e Resiliência

### 1. Banco de Dados SQLite em Volume Compartilhado
Para um desafio Sênior equilibrando complexidade vs. manutenibilidade fácil de validar, optei por usar **SQLite**. Ao invés de dependermos de um *terceiro container* na stack com um banco relacional pesado (como SQL Server ou PostgreSQL), o SQLite vive perfeitamente como um arquivo `.db`. Pelo `docker-compose.yml`, montei um `volume` onde as modificações persistem na pasta da máquina do host e este volume é injetado tanto na API quanto no Worker.
A aplicação/Worker se encarrega de rodar instancialmente um `_context.Database.MigrateAsync()` garantindo que o schema sempre está atualizado na inicialização.

### 2. Tratamento do Worker (RPA)
O Worker faz requisições a `http://quotes.toscrape.com/`. Para lidar com flutuações de rede da página-alvo, implementou-se `Polly` injetado diretamente no `IHttpClientFactory` através de extensão do ASP.NET. Em caso de *Transient Errors* (tais como erros HTTP série 5xx ou TimeOuts de rede), o HttpClient fará retentativas (`Retry Policy` com `WaitAndRetryAsync`) através de *Backoff Exponencial*, sem derrubar a *thread* principal do Worker. O processo em background não para; ele descansa.

### 3. Extração (Parsing)
Utilizamos `HtmlAgilityPack` para rodar seletores de *XPath* robustos, sendo menos vulnerável a pequenas flutuações de design, diferentemente do uso de manipulação crua de strings por RegEx.

---

## 🔮 O que eu melhoraria se tivesse mais tempo

1. **Mensageria Assíncrona (RabbitMQ/Kafka)**: Eventos de "Novo Item Raspado" poderiam ser publicados pelo Worker no RabbitMQ ao invés de inserções diretas no banco de dados da aplicação Web. A API (ou outro Worker consumidor associado a ela) faria a subscrição destes eventos para efetivar as gravações no banco da API e quiçá até atualizar um WebSocket no FrontEnd em tempo real.
2. **Criação de Interface Genérica (Abstração do Data Source)**: Criaria uma abstração `IScraperStrategy` na qual diferentes *Scrapers* (Quotes, Cotações Moeda, Ações B3) fossem ativados e resolvidos via injeção de dependência baseado em configurações e chaves ativas do `.json`, permitindo escalar os nós extração multi-site dentro do mesmo container/projeto.
3. **Testes Unitários e de Integração**: Criação de um quarto e quinto projeto (`ScraperTests.Unit`, `ScraperTests.Integration`) testando a estabilidade da interface do `WebScraperClient` (fazendo mock das requisições com `HttpMessageHandler`) e end-to-end com *TestContainers*.
4. **Log Centralizado (Ex: Serilog + Seq / ELK)**: Os logs atualmente estão dependentes da saída do Console dos Docker Containers. Redirecionaria estes *Structured Logs* para um Sink central.
