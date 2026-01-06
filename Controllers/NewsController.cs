using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;
    private readonly ILogger<NewsController> _logger;

    public NewsController(INewsService newsService, ILogger<NewsController> logger)
    {
        _newsService = newsService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/news?page=1&limit=10
    /// Returns a paginated list of news articles.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NewsArticle>>> GetAllNews([FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        try
        {
            var news = await _newsService.GetNewsAsync(page, limit);
            return Ok(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching paginated news (Page: {Page}, Limit: {Limit})", page, limit);
            return StatusCode(500, "Internal server error while retrieving news.");
        }
    }

    /// <summary>
    /// GET /api/news/featured
    /// Returns a list of featured articles.
    /// </summary>
    [HttpGet("featured")]
    public async Task<ActionResult<List<NewsArticle>>> GetFeatured()
    {
        try
        {
            var featured = await _newsService.GetFeaturedNewsAsync();
            return Ok(featured);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching featured news articles.");
            return StatusCode(500, "Internal server error while retrieving featured articles.");
        }
    }

    /// <summary>
    /// GET /api/news/{id}
    /// Returns a single article by its database ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NewsArticle>> GetById(int id)
    {
        try
        {
            var article = await _newsService.GetNewsByIdAsync(id);

            if (article == null)
            {
                _logger.LogWarning("News article with ID {Id} not found.", id);
                return NotFound(new { message = $"Article with ID {id} not found." });
            }

            return Ok(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching news article with ID {Id}", id);
            return StatusCode(500, "Internal server error while retrieving the article.");
        }
    }
}