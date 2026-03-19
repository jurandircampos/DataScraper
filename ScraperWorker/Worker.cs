using Microsoft.EntityFrameworkCore;
using ScraperCore.Data;
using ScraperWorker.Services;

namespace ScraperWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _intervalInMinutes;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _intervalInMinutes = configuration.GetValue<int>("ScraperSettings:IntervalInMinutes", 5);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço ScraperWorker iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Scraping iniciado às: {time}", DateTimeOffset.Now);
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var scrapeService = scope.ServiceProvider.GetRequiredService<ScrapeService>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // 1. Aplicar migrações ao iniciar o banco (garante o DB atualizado via Worker)
                    await dbContext.Database.MigrateAsync(stoppingToken);

                    // 2. Extrair dados
                    var scrapedQuotes = await scrapeService.GetQuotesAsync(stoppingToken);

                    // 3. Salvar apenas os novos (evitando duplicidade baseado em Texto + Autor para simplicidade)
                    int addedCount = 0;
                    foreach (var quote in scrapedQuotes)
                    {
                        bool exists = dbContext.Quotes.Any(q => q.Text == quote.Text && q.Author == quote.Author);
                        if (!exists)
                        {
                            dbContext.Quotes.Add(quote);
                            addedCount++;
                        }
                    }

                    if (addedCount > 0)
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("{Count} novas quotes foram salvas no banco de dados.", addedCount);
                    }
                    else
                    {
                        _logger.LogInformation("Nenhuma quote nova encontrada nesta execução.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante a execução do ciclo de scraping.");
                }

                _logger.LogInformation("Aguardando {Minutes} minutos para a próxima execução...", _intervalInMinutes);
                await Task.Delay(TimeSpan.FromMinutes(_intervalInMinutes), stoppingToken);
            }
        }
    }
}
