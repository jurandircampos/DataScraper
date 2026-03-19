using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using ScraperCore.Data;
using ScraperWorker;
using ScraperWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure SQLite DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var dbPath = builder.Configuration.GetValue<string>("DatabasePath", "scraper.db");
    
    // In Docker, we'll map a volume to /app/data so the database is persistent
    // Local run will just use the current directory
    string fullPath = Path.IsPathRooted(dbPath) ? dbPath : Path.Combine(AppContext.BaseDirectory, dbPath!);
    
    options.UseSqlite($"Data Source={fullPath}");
});

// Resiliency policies mapping
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

builder.Services.AddHttpClient<ScrapeService>()
    .AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
