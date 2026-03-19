namespace ScraperCore.Models
{
    public class Quote
    {
        public int Id { get; set; }
        public required string Text { get; set; }
        public required string Author { get; set; }
        public string? Tags { get; set; } 
        public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    }
}
