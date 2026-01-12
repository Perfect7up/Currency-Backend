using Backend.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace Backend.Services;

public interface IToolsService
{
    Task<decimal> ConvertCurrencyAsync(string fromId, string toCurrency, decimal amount);
    Task<List<Coin>> CompareCoinsAsync(string ids);
}

public class ToolsService : IToolsService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ToolsService> _logger;
    private readonly string _apiKey;

    public ToolsService(HttpClient httpClient, IMemoryCache cache, ILogger<ToolsService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey = config["CoinGecko:ApiKey"] ?? "";

        _httpClient.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoTrackerTools");

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-cg-demo-api-key", _apiKey);
        }
    }

    public async Task<decimal> ConvertCurrencyAsync(string fromId, string toCurrency, decimal amount)
    {
        string cacheKey = $"convert_{fromId}_{toCurrency}_{amount}";

        if (!_cache.TryGetValue(cacheKey, out decimal result))
        {
            try
            {
                var url = $"simple/price?ids={fromId.ToLower()}&vs_currencies={toCurrency.ToLower()}";
                var response = await _httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(url);

                if (response != null && response.ContainsKey(fromId.ToLower()))
                {
                    var price = response[fromId.ToLower()][toCurrency.ToLower()];
                    result = price * amount;
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(2));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting {Amount} {FromId} to {ToCurrency}", amount, fromId, toCurrency);
                return 0;
            }
        }
        return result;
    }

    public async Task<List<Coin>> CompareCoinsAsync(string ids)
    {
        // FIX 1: Sanitize IDs. CoinGecko fails if there are spaces like "bitcoin, ethereum"
        var cleanIds = string.Join(",", ids.Split(',')
            .Select(s => s.Trim().ToLower())
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        if (string.IsNullOrEmpty(cleanIds)) return new List<Coin>();

        string cacheKey = $"compare_{cleanIds}";

        if (!_cache.TryGetValue(cacheKey, out List<Coin>? coins))
        {
            try
            {
                // Use the sanitized cleanIds
                var url = $"coins/markets?vs_currency=usd&ids={cleanIds}&order=market_cap_desc&sparkline=false";

                // FIX 2: Use the specific Markets DTO
                var response = await _httpClient.GetFromJsonAsync<List<CoinGeckoMarketsDto>>(url);

                if (response == null) return new List<Coin>();

                coins = response.Select(x => new Coin
                {
                    Id = x.id,
                    Symbol = x.symbol?.ToUpper() ?? string.Empty,
                    Name = x.name ?? string.Empty,
                    Image = x.image ?? string.Empty,
                    CurrentPrice = x.current_price ?? 0,
                    // FIX 3: Safe casting for MarketCap
                    MarketCap = x.market_cap ?? 0,
                    MarketCapRank = x.market_cap_rank ?? 0,
                    PriceChangePercentage24h = x.price_change_percentage_24h ?? 0,
                    LastUpdated = DateTime.UtcNow
                }).ToList();

                _cache.Set(cacheKey, coins, TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing coins for IDs: {Ids}", cleanIds);
                return new List<Coin>(); // If it fails, it returns empty, causing your "zero" issue
            }
        }
        return coins ?? new List<Coin>();
    }
}