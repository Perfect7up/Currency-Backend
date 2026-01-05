using System.Net.Http.Json;
using Backend.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services;

public interface IMarketService
{
    Task<MarketOverview?> GetMarketOverviewAsync();
    Task<List<Coin>> GetTopGainersAsync(int limit);
    Task<List<Coin>> GetTopLosersAsync(int limit);
    Task<List<Coin>> GetTrendingAsync(int limit);
}

public class MarketService : IMarketService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MarketService> _logger;
    private readonly TimeSpan _shortCache = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _staleCache = TimeSpan.FromHours(1);

    public MarketService(HttpClient httpClient, IMemoryCache cache, ILogger<MarketService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Backend-Crypto-App");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<MarketOverview?> GetMarketOverviewAsync()
    {
        const string cacheKey = "market_overview";
        const string staleCacheKey = "market_overview_stale";

        return await GetWithFallbackNullableAsync(cacheKey, staleCacheKey, _shortCache, async () =>
        {
            var globalUrl = "https://api.coingecko.com/api/v3/global";
            var globalRes = await _httpClient.GetFromJsonAsync<GlobalResponse>(globalUrl);

            var priceUrl = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum&vs_currencies=usd";
            var priceRes = await _httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(priceUrl);

            if (globalRes == null) return null;

            return new MarketOverview
            {
                TotalMarketCap = globalRes.data.total_market_cap.GetValueOrDefault("usd"),
                MarketCapChange = globalRes.data.market_cap_change_percentage_24h_usd,
                TotalVolume = globalRes.data.total_volume.GetValueOrDefault("usd"),
                VolumeChange = 0,
                BtcDominance = globalRes.data.market_cap_percentage.GetValueOrDefault("btc"),
                BtcPrice = priceRes?.GetValueOrDefault("bitcoin")?.GetValueOrDefault("usd") ?? 0,
                EthPrice = priceRes?.GetValueOrDefault("ethereum")?.GetValueOrDefault("usd") ?? 0,
                Timestamp = DateTime.UtcNow
            };
        });
    }

    public async Task<List<Coin>> GetTopGainersAsync(int limit)
    {
        string cacheKey = $"top_gainers_{limit}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync(cacheKey, staleCacheKey, _shortCache, async () =>
        {
            var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=price_change_percentage_24h_desc&per_page={limit}&page=1&price_change_percentage=24h";
            return await FetchAndMapCoins(url);
        }) ?? new List<Coin>();
    }

    public async Task<List<Coin>> GetTopLosersAsync(int limit)
    {
        string cacheKey = $"top_losers_{limit}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync(cacheKey, staleCacheKey, _shortCache, async () =>
        {
            var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=price_change_percentage_24h_asc&per_page={limit}&page=1&price_change_percentage=24h";
            return await FetchAndMapCoins(url);
        }) ?? new List<Coin>();
    }

    public async Task<List<Coin>> GetTrendingAsync(int limit)
    {
        string cacheKey = $"trending_{limit}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync(cacheKey, staleCacheKey, _shortCache, async () =>
        {
            var url = "https://api.coingecko.com/api/v3/search/trending";
            var response = await _httpClient.GetFromJsonAsync<MarketTrendingDto>(url);

            return response?.coins.Take(limit).Select(x => new Coin
            {
                Id = x.item.id,
                Symbol = x.item.symbol.ToUpper(),
                Name = x.item.name,
                Image = x.item.small,
                MarketCapRank = x.item.market_cap_rank,
                CurrentPrice = (decimal)(x.item.data?.price ?? 0),
                PriceChangePercentage24h = x.item.data?.price_change_percentage_24h?.GetValueOrDefault("usd") ?? 0,
                LastUpdated = DateTime.UtcNow
            }).ToList() ?? new List<Coin>();
        }) ?? new List<Coin>();
    }

    private async Task<List<Coin>> FetchAndMapCoins(string url)
    {
        var response = await _httpClient.GetFromJsonAsync<List<MarketCoinDto>>(url);
        return response?.Select(x => new Coin
        {
            Id = x.id,
            Symbol = x.symbol.ToUpper(),
            Name = x.name,
            Image = x.image,
            CurrentPrice = x.current_price,
            PriceChangePercentage24h = x.price_change_percentage_24h,
            MarketCapRank = x.market_cap_rank,
            LastUpdated = DateTime.UtcNow
        }).ToList() ?? new List<Coin>();
    }

    private async Task<T?> GetWithFallbackAsync<T>(
        string cacheKey,
        string staleCacheKey,
        TimeSpan freshCacheDuration,
        Func<Task<T?>> fetchFunc) where T : class
    {
        if (_cache.TryGetValue<T>(cacheKey, out var cachedData) && cachedData != null)
        {
            return cachedData;
        }

        try
        {
            var freshData = await RetryAsync(fetchFunc, maxRetries: 3);

            if (freshData != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(freshCacheDuration);
                _cache.Set(cacheKey, freshData, cacheOptions);

                var staleCacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_staleCache);
                _cache.Set(staleCacheKey, freshData, staleCacheOptions);
            }

            return freshData;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API call failed for {CacheKey}. Attempting to use stale cache.", cacheKey);

            if (_cache.TryGetValue<T>(staleCacheKey, out var staleData) && staleData != null)
            {
                _logger.LogInformation("Returning stale cached data for {CacheKey}", cacheKey);
                return staleData;
            }

            _logger.LogError(ex, "No stale cache available for {CacheKey}. Returning null or empty.", cacheKey);
            return null;
        }
    }

    private async Task<T?> GetWithFallbackNullableAsync<T>(
        string cacheKey,
        string staleCacheKey,
        TimeSpan freshCacheDuration,
        Func<Task<T?>> fetchFunc) where T : class
    {
        if (_cache.TryGetValue<T>(cacheKey, out var cachedData) && cachedData != null)
        {
            return cachedData;
        }

        try
        {
            var freshData = await RetryNullableAsync(fetchFunc, maxRetries: 3);

            if (freshData != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(freshCacheDuration);
                _cache.Set(cacheKey, freshData, cacheOptions);

                var staleCacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_staleCache);
                _cache.Set(staleCacheKey, freshData, staleCacheOptions);
            }

            return freshData;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API call failed for {CacheKey}. Attempting to use stale cache.", cacheKey);

            if (_cache.TryGetValue<T>(staleCacheKey, out var staleData) && staleData != null)
            {
                _logger.LogInformation("Returning stale cached data for {CacheKey}", cacheKey);
                return staleData;
            }

            _logger.LogError(ex, "No stale cache available for {CacheKey}. Returning null.", cacheKey);
            return null;
        }
    }

    private async Task<T?> RetryAsync<T>(Func<Task<T?>> operation, int maxRetries = 3) where T : class
    {
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromMilliseconds(500);

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, "API request failed. Retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);

                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
            catch (TaskCanceledException ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, "API request timed out. Retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);

                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }

    private async Task<T?> RetryNullableAsync<T>(Func<Task<T?>> operation, int maxRetries = 3) where T : class
    {
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromMilliseconds(500);

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, "API request failed. Retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);

                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
            catch (TaskCanceledException ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, "API request timed out. Retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);

                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }
}

public record GlobalResponse(GlobalData data);

public record GlobalData(
    Dictionary<string, decimal> total_market_cap,
    Dictionary<string, decimal> total_volume,
    Dictionary<string, double> market_cap_percentage,
    double market_cap_change_percentage_24h_usd
);

public record MarketCoinDto(
    string id,
    string symbol,
    string name,
    string image,
    decimal current_price,
    int market_cap_rank,
    double price_change_percentage_24h
);

public record MarketTrendingDto(List<MarketTrendingItemWrapper> coins);

public record MarketTrendingItemWrapper(MarketTrendingCoinItem item);

public record MarketTrendingCoinItem(
    string id,
    string name,
    string symbol,
    string small,
    int market_cap_rank,
    TrendingData? data
);

public record TrendingData(
    double price,
    Dictionary<string, double> price_change_percentage_24h
);