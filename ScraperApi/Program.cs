using Microsoft.EntityFrameworkCore;
using ScraperCore.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure SQLite DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var dbPath = builder.Configuration.GetValue<string>("DatabasePath", "scraper.db");
    
    // In Docker, we'll map a volume to /app/data so the database is persistent
    string fullPath = Path.IsPathRooted(dbPath) ? dbPath : Path.Combine(AppContext.BaseDirectory, dbPath!);
    
    options.UseSqlite($"Data Source={fullPath}");
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Scraper API", Version = "v1", Description = "API to expose data collected by ScraperWorker." });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction()) // Ativando no docker pro user ver
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
