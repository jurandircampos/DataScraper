using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScraperCore.Data;
using ScraperCore.Models;

namespace ScraperApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuotesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Quotes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Quote>>> GetQuotes([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var quotes = await _context.Quotes
                .OrderByDescending(q => q.ScrapedAt) // Mais recentes primeiro
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalItems = await _context.Quotes.CountAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                Items = quotes
            });
        }

        // GET: api/Quotes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Quote>> GetQuote(int id)
        {
            var quote = await _context.Quotes.FindAsync(id);

            if (quote == null)
            {
                return NotFound();
            }

            return quote;
        }

        // GET: api/Quotes/author/{authorName}
        [HttpGet("author/{authorName}")]
        public async Task<ActionResult<IEnumerable<Quote>>> GetQuotesByAuthor(string authorName)
        {
            var quotes = await _context.Quotes
                .Where(q => EF.Functions.Like(q.Author, $"%{authorName}%"))
                .ToListAsync();

            if (!quotes.Any())
            {
                return NotFound();
            }

            return quotes;
        }

        // GET: api/Quotes/tag/{tag}
        [HttpGet("tag/{tag}")]
        public async Task<ActionResult<IEnumerable<Quote>>> GetQuotesByTag(string tag)
        {
            var quotes = await _context.Quotes
                .Where(q => EF.Functions.Like(q.Tags, $"%{tag}%"))
                .ToListAsync();

            if (!quotes.Any())
            {
                return NotFound();
            }

            return quotes;
        }
    }
}
