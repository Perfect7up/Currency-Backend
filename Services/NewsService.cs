using Backend.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace Backend.Services;

public interface INewsService
{
    Task<List<NewsArticle>> GetNewsAsync(int page, int limit);
    Task<List<NewsArticle>> GetFeaturedNewsAsync();
    Task<NewsArticle?> GetNewsByIdAsync(int id);
}

public class NewsService : INewsService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NewsService> _logger;
    private readonly string _apiKey;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

    public NewsService(HttpClient httpClient, IMemoryCache cache, ILogger<NewsService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey = config["CryptoCompare:ApiKey"] ?? "";

        _httpClient.BaseAddress = new Uri("https://min-api.cryptocompare.com/data/v2/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoTrackerApp");

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Apikey", _apiKey);
        }
    }

    public async Task<List<NewsArticle>> GetNewsAsync(int page, int limit)
    {
        string cacheKey = $"news_cryptocompare_p{page}";
        if (!_cache.TryGetValue(cacheKey, out List<NewsArticle>? articles))
        {
            try
            {
                var url = "news/?lang=EN";
                var response = await _httpClient.GetFromJsonAsync<CryptoCompareResponse>(url);

                if (response?.Data == null) return new List<NewsArticle>();

                articles = response.Data.Select(x => new NewsArticle
                {
                    Id = int.TryParse(x.id, out int id) ? id : x.id.GetHashCode(),
                    Title = x.title,
                    Summary = x.body,
                    Content = x.body,
                    ImageUrl = x.imageurl,
                    Source = x.source_info.name,
                    Url = x.url,
                    IsFeatured = false,
                    PublishedAt = DateTimeOffset.FromUnixTimeSeconds(x.published_on).UtcDateTime
                })
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

                _cache.Set(cacheKey, articles, _cacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch news from CryptoCompare");
                return new List<NewsArticle>();
            }
        }
        return articles ?? new List<NewsArticle>();
    }

    public async Task<List<NewsArticle>> GetFeaturedNewsAsync()
    {
        var news = await GetNewsAsync(1, 5);
        foreach (var item in news) item.IsFeatured = true;
        return news;
    }

    public async Task<NewsArticle?> GetNewsByIdAsync(int id)
    {
        var news = await GetNewsAsync(1, 50);
        return news.FirstOrDefault(x => x.Id == id);
    }
}

public record CryptoCompareResponse(List<CryptoCompareNewsItem> Data);
public record CryptoCompareNewsItem(
    string id,
    string title,
    string body,
    string url,
    string imageurl,
    long published_on,
    SourceInfo source_info
);
public record SourceInfo(string name, string lang);