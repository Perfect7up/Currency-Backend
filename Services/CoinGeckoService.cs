using Backend.Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services;

public interface ICoinService
{
    Task<List<Coin>> GetCoinsAsync(int page, int perPage);
    Task<Coin?> GetCoinByIdAsync(string id);
    Task<List<Coin>> SearchCoinsAsync(string query);
    Task<List<Coin>> GetTrendingCoinsAsync();
    Task<List<Coin>> GetLiveCoinsAsync(int perPage);
    Task<List<PriceHistory>> GetPriceHistoryAsync(string coinId, int days);
    Task<Coin?> GetCoinDetailsAsync(string id);
    Task<List<PriceHistory>> GetPriceHistoryByPeriodAsync(string id, string period);
    Task<MarketStats?> GetMarketStatsAsync(string id);
}

public class CoinGeckoService : ICoinService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CoinGeckoService> _logger;

    private readonly TimeSpan _shortCache = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _mediumCache = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _longCache = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _staleCache = TimeSpan.FromHours(1);

    public CoinGeckoService(HttpClient httpClient, IMemoryCache cache, ILogger<CoinGeckoService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Backend-Crypto-App");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<List<Coin>> GetCoinsAsync(int page, int perPage)
    {
        string cacheKey = $"coins_list_{page}_{perPage}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync(cacheKey, staleCacheKey, _shortCache, async () =>
        {
            var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page={perPage}&page={page}&price_change_percentage=24h";
            var response = await _httpClient.GetFromJsonAsync<List<CoinGeckoDto>>(url);
            return MapDtoToCoin(response);
        }) ?? new List<Coin>();
    }

    public async Task<Coin?> GetCoinByIdAsync(string id)
    {
        string cacheKey = $"coin_basic_{id.ToLower()}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackNullableAsync(cacheKey, staleCacheKey, _shortCache, async () =>
        {
            var url = $"https://api.coingecko.com/api/v3/coins/{id.ToLower()}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false";
            var x = await _httpClient.GetFromJsonAsync<CoinDetailDto>(url);
            if (x == null) return null;

            return new Coin
            {
                Id = x.id,
                Symbol = x.symbol.ToUpper(),
                Name = x.name,
                Image = x.image.large,
                CurrentPrice = x.market_data.current_price.usd,
                MarketCapRank = (int)x.market_data.market_cap_rank,
                MarketCap = (long)(x.market_data.market_cap.ContainsKey("usd") ? x.market_data.market_cap["usd"] : 0),
                PriceChangePercentage24h = x.market_data.price_change_percentage_24h,
                LastUpdated = DateTime.UtcNow
            };
        });
    }

    public async Task<Coin?> GetCoinDetailsAsync(string id)
    {
        string cacheKey = $"coin_full_details_{id.ToLower()}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackNullableAsync(cacheKey, staleCacheKey, _mediumCache, async () =>
        {
            var url = $"https://api.coingecko.com/api/v3/coins/{id.ToLower()}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false";
            var x = await _httpClient.GetFromJsonAsync<CoinDetailDto>(url);
            if (x == null) return null;

            var coin = await GetCoinByIdAsync(id);
            if (coin != null && x.description?.en != null)
            {
                coin.Description = x.description.en;
            }
            return coin;
        });
    }

    public async Task<MarketStats?> GetMarketStatsAsync(string id)
    {
        string cacheKey = $"market_stats_{id.ToLower()}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackNullableAsync(cacheKey, staleCacheKey, _shortCache, async () =>
        {
            var url = $"https://api.coingecko.com/api/v3/coins/{id.ToLower()}?localization=false&tickers=false&market_data=true";
            var x = await _httpClient.GetFromJsonAsync<CoinDetailDto>(url);
            if (x == null) return null;

            var md = x.market_data;
            return new MarketStats
            {
                CoinId = x.id,
                CurrentPrice = md.current_price.usd,
                MarketCap = (long)(md.market_cap.ContainsKey("usd") ? md.market_cap["usd"] : 0),
                MarketCapRank = (int)md.market_cap_rank,
                TotalVolume = md.total_volume.ContainsKey("usd") ? md.total_volume["usd"] : 0,
                High24h = md.high_24h.ContainsKey("usd") ? md.high_24h["usd"] : 0,
                Low24h = md.low_24h.ContainsKey("usd") ? md.low_24h["usd"] : 0,
                CirculatingSupply = md.circulating_supply,
                TotalSupply = md.total_supply ?? 0,
                MaxSupply = md.max_supply,
                PriceChangePercentage24h = md.price_change_percentage_24h
            };
        });
    }

    public async Task<List<PriceHistory>> GetPriceHistoryByPeriodAsync(string id, string period)
    {
        string days = period.ToLower() switch
        {
            "1h" => "1",
            "24h" => "1",
            "7d" => "7",
            "30d" => "30",
            "1y" => "365",
            _ => "7"
        };

        return await GetPriceHistoryAsync(id, int.Parse(days));
    }

    public async Task<List<PriceHistory>> GetPriceHistoryAsync(string coinId, int days)
    {
        string cacheKey = $"history_{coinId.ToLower()}_{days}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync(cacheKey, staleCacheKey, _mediumCache, async () =>
        {
            var url = $"https://api.coingecko.com/api/v3/coins/{coinId.ToLower()}/market_chart?vs_currency=usd&days={days}";
            var response = await _httpClient.GetFromJsonAsync<CoinHistoryDto>(url);

            return response?.prices.Select(p => new PriceHistory
            {
                CoinId = coinId,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)p[0]).UtcDateTime,
                Price = (decimal)p[1]
            }).ToList() ?? new List<PriceHistory>();
        }) ?? new List<PriceHistory>();
    }

    public async Task<List<Coin>> SearchCoinsAsync(string query)
    {
        string cacheKey = $"search_{query.ToLower()}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync(cacheKey, staleCacheKey, _longCache, async () =>
        {
            var url = $"https://api.coingecko.com/api/v3/search?query={query}";
            var response = await _httpClient.GetFromJsonAsync<CoinGeckoSearchDto>(url);

            return response?.coins.Select(x => new Coin
            {
                Id = x.id,
                Symbol = x.symbol.ToUpper(),
                Name = x.name,
                Image = x.thumb,
                MarketCapRank = x.market_cap_rank ?? 0
            }).ToList() ?? new List<Coin>();
        }) ?? new List<Coin>();
    }

    public async Task<List<Coin>> GetTrendingCoinsAsync()
    {
        const string cacheKey = "trending_coins_full";
        const string staleCacheKey = "trending_coins_full_stale";

        return await GetWithFallbackAsync(cacheKey, staleCacheKey, _shortCache, async () =>
        {
            var url = "https://api.coingecko.com/api/v3/search/trending";
            var response = await _httpClient.GetFromJsonAsync<CoinGeckoTrendingDto>(url);
            if (response == null || !response.coins.Any()) return new List<Coin>();

            var ids = string.Join(",", response.coins.Select(x => x.item.id));
            var marketUrl = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&ids={ids}&order=market_cap_desc&sparkline=false";
            var marketData = await _httpClient.GetFromJsonAsync<List<CoinGeckoDto>>(marketUrl);
            return MapDtoToCoin(marketData);
        }) ?? new List<Coin>();
    }

    public async Task<List<Coin>> GetLiveCoinsAsync(int perPage) => await GetCoinsAsync(1, perPage);

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

    private List<Coin> MapDtoToCoin(List<CoinGeckoDto>? response)
    {
        return response?.Select(x => new Coin
        {
            Id = x.id,
            Symbol = x.symbol.ToUpper(),
            Name = x.name,
            Image = x.image,
            CurrentPrice = x.current_price,
            MarketCap = (long)x.market_cap,
            MarketCapRank = x.market_cap_rank,
            PriceChangePercentage24h = x.price_change_percentage_24h,
            LastUpdated = DateTime.UtcNow
        }).ToList() ?? new List<Coin>();
    }
}

public record CoinGeckoDto(string id, string symbol, string name, string image, decimal current_price, double market_cap, int market_cap_rank, double price_change_percentage_24h);
public record CoinHistoryDto(List<List<double>> prices);
public record CoinGeckoSearchDto(List<CoinGeckoSearchItemDto> coins);
public record CoinGeckoSearchItemDto(string id, string name, string symbol, string thumb, int? market_cap_rank);
public record CoinGeckoTrendingDto(List<TrendingItemWrapper> coins);
public record TrendingItemWrapper(TrendingCoinItem item);
public record TrendingCoinItem(string id, string name, string symbol, string small, int market_cap_rank);
public record CoinDetailDto(string id, string symbol, string name, DescriptionDto? description, ImageDto image, MarketDataDto market_data);
public record DescriptionDto(string? en);
public record ImageDto(string large);
public record MarketDataDto(
    CurrentPriceDto current_price,
    Dictionary<string, decimal> market_cap,
    double market_cap_rank,
    double price_change_percentage_24h,
    Dictionary<string, decimal> total_volume,
    Dictionary<string, decimal> high_24h,
    Dictionary<string, decimal> low_24h,
    double circulating_supply,
    double? total_supply,
    double? max_supply
);
public record CurrentPriceDto(decimal usd);