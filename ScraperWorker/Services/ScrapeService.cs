using HtmlAgilityPack;
using ScraperCore.Models;

namespace ScraperWorker.Services
{
    public class ScrapeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ScrapeService> _logger;

        public ScrapeService(HttpClient httpClient, ILogger<ScrapeService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<Quote>> GetQuotesAsync(CancellationToken cancellationToken)
        {
            var quotes = new List<Quote>();
            var url = "http://quotes.toscrape.com/";

            try
            {
                _logger.LogInformation("Carregando página: {Url}", url);
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var quoteNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='quote']");

                if (quoteNodes != null)
                {
                    foreach (var node in quoteNodes)
                    {
                        var textNode = node.SelectSingleNode(".//span[@class='text']");
                        var authorNode = node.SelectSingleNode(".//small[@class='author']");
                        var tagNodes = node.SelectNodes(".//div[@class='tags']/a[@class='tag']");

                        var text = textNode?.InnerText ?? string.Empty;
                        
                        // Remover aspas duplas do text
                        if (text.StartsWith("&#39;") || text.StartsWith("“"))
                            text = text.Substring(1);
                        if (text.EndsWith("&#39;") || text.EndsWith("”"))
                            text = text.Substring(0, text.Length - 1);
                        
                        var author = authorNode?.InnerText ?? string.Empty;
                        var tags = tagNodes != null ? string.Join(", ", tagNodes.Select(t => t.InnerText)) : string.Empty;

                        quotes.Add(new Quote
                        {
                            Text = System.Net.WebUtility.HtmlDecode(text),
                            Author = System.Net.WebUtility.HtmlDecode(author),
                            Tags = tags
                        });
                    }
                }
                
                _logger.LogInformation("Foram extraídas {Count} quotes da página {Url}.", quotes.Count, url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar o web scraping.");
                throw;
            }

            return quotes;
        }
    }
}
