using Backend.Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

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

    private static readonly SemaphoreSlim _apiLock = new SemaphoreSlim(1, 1);

    private readonly TimeSpan _shortCache = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _mediumCache = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _longCache = TimeSpan.FromMinutes(60);
    private readonly TimeSpan _staleCache = TimeSpan.FromHours(24);

    public CoinGeckoService(HttpClient httpClient, IMemoryCache cache, ILogger<CoinGeckoService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoTracker-Backend-v1");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<List<Coin>> GetCoinsAsync(int page, int perPage)
    {
        string cacheKey = $"coins_list_{page}_{perPage}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync<List<CoinGeckoDto>, List<Coin>>(
            cacheKey,
            staleCacheKey,
            _shortCache,
            async () => await _httpClient.GetFromJsonAsync<List<CoinGeckoDto>>($"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page={perPage}&page={page}&price_change_percentage=24h"),
            dto => MapDtoToCoin(dto)
        ) ?? new List<Coin>();
    }

    public async Task<Coin?> GetCoinByIdAsync(string id)
    {
        string cacheKey = $"coin_basic_{id.ToLower()}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync<CoinDetailDto, Coin>(
            cacheKey,
            staleCacheKey,
            _shortCache,
            async () => await _httpClient.GetFromJsonAsync<CoinDetailDto>($"https://api.coingecko.com/api/v3/coins/{id.ToLower()}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false"),
            x => x == null ? null : new Coin
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
            }
        );
    }

    public async Task<Coin?> GetCoinDetailsAsync(string id)
    {
        string cacheKey = $"coin_full_details_{id.ToLower()}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync<CoinDetailDto, Coin>(
            cacheKey,
            staleCacheKey,
            _mediumCache,
            async () => await _httpClient.GetFromJsonAsync<CoinDetailDto>($"https://api.coingecko.com/api/v3/coins/{id.ToLower()}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false"),
            x => x == null ? null : new Coin
            {
                Id = x.id,
                Symbol = x.symbol.ToUpper(),
                Name = x.name,
                Image = x.image.large,
                Description = x.description?.en ?? "",
                CurrentPrice = x.market_data.current_price.usd,
                MarketCapRank = (int)x.market_data.market_cap_rank,
                PriceChangePercentage24h = x.market_data.price_change_percentage_24h,
                LastUpdated = DateTime.UtcNow
            }
        );
    }

    public async Task<MarketStats?> GetMarketStatsAsync(string id)
    {
        string cacheKey = $"market_stats_{id.ToLower()}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync<CoinDetailDto, MarketStats>(
            cacheKey,
            staleCacheKey,
            _shortCache,
            async () => await _httpClient.GetFromJsonAsync<CoinDetailDto>($"https://api.coingecko.com/api/v3/coins/{id.ToLower()}?localization=false&tickers=false&market_data=true"),
            x =>
            {
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
            }
        );
    }

    public async Task<List<PriceHistory>> GetPriceHistoryAsync(string coinId, int days)
    {
        string cacheKey = $"history_{coinId.ToLower()}_{days}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync<CoinHistoryDto, List<PriceHistory>>(
            cacheKey,
            staleCacheKey,
            _mediumCache,
            async () => await _httpClient.GetFromJsonAsync<CoinHistoryDto>($"https://api.coingecko.com/api/v3/coins/{coinId.ToLower()}/market_chart?vs_currency=usd&days={days}"),
            dto => dto?.prices.Select(p => new PriceHistory
            {
                CoinId = coinId,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)p[0]).UtcDateTime,
                Price = (decimal)p[1]
            }).ToList() ?? new List<PriceHistory>()
        ) ?? new List<PriceHistory>();
    }

    public async Task<List<PriceHistory>> GetPriceHistoryByPeriodAsync(string id, string period)
    {
        int days = period.ToLower() switch { "1h" => 1, "24h" => 1, "7d" => 7, "30d" => 30, "1y" => 365, _ => 7 };
        return await GetPriceHistoryAsync(id, days);
    }

    public async Task<List<Coin>> SearchCoinsAsync(string query)
    {
        string cacheKey = $"search_{query.ToLower()}";
        string staleCacheKey = $"{cacheKey}_stale";

        return await GetWithFallbackAsync<CoinGeckoSearchDto, List<Coin>>(
            cacheKey,
            staleCacheKey,
            _longCache,
            async () => await _httpClient.GetFromJsonAsync<CoinGeckoSearchDto>($"https://api.coingecko.com/api/v3/search?query={query}"),
            dto => dto?.coins.Select(x => new Coin
            {
                Id = x.id,
                Symbol = x.symbol.ToUpper(),
                Name = x.name,
                Image = x.thumb,
                MarketCapRank = x.market_cap_rank ?? 0
            }).ToList() ?? new List<Coin>()
        ) ?? new List<Coin>();
    }

    public async Task<List<Coin>> GetTrendingCoinsAsync()
    {
        const string cacheKey = "trending_coins_full";
        const string staleCacheKey = "trending_coins_full_stale";

        // Logic fix: Fetch trending list AND full market data inside the fetch function
        return await GetWithFallbackAsync<List<CoinGeckoDto>, List<Coin>>(
            cacheKey,
            staleCacheKey,
            _shortCache,
            async () =>
            {
                var trending = await _httpClient.GetFromJsonAsync<CoinGeckoTrendingDto>("https://api.coingecko.com/api/v3/search/trending");
                if (trending == null || !trending.coins.Any()) return new List<CoinGeckoDto>();

                var ids = string.Join(",", trending.coins.Select(x => x.item.id));
                return await _httpClient.GetFromJsonAsync<List<CoinGeckoDto>>($"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&ids={ids}&order=market_cap_desc&sparkline=false");
            },
            dto => MapDtoToCoin(dto)
        ) ?? new List<Coin>();
    }

    public async Task<List<Coin>> GetLiveCoinsAsync(int perPage) => await GetCoinsAsync(1, perPage);

    // --- Core Generic Logic ---

    private async Task<TOut?> GetWithFallbackAsync<TIn, TOut>(
        string cacheKey,
        string staleCacheKey,
        TimeSpan freshCacheDuration,
        Func<Task<TIn?>> fetchFunc,
        Func<TIn?, TOut?> mapper) where TIn : class where TOut : class
    {
        if (_cache.TryGetValue<TOut>(cacheKey, out var cachedData)) return cachedData;

        await _apiLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue<TOut>(cacheKey, out cachedData)) return cachedData;

            var rawData = await RetryAsync(fetchFunc);
            var processedData = mapper(rawData);

            if (processedData != null)
            {
                _cache.Set(cacheKey, processedData, freshCacheDuration);
                _cache.Set(staleCacheKey, processedData, _staleCache);
            }
            return processedData;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API failure for {Key}. Error: {Msg}", cacheKey, ex.Message);
            return _cache.TryGetValue<TOut>(staleCacheKey, out var staleData) ? staleData : null;
        }
        finally
        {
            await Task.Delay(1200); // Throttling for Free Tier
            _apiLock.Release();
        }
    }

    private async Task<T?> RetryAsync<T>(Func<Task<T?>> operation, int maxRetries = 3) where T : class
    {
        int retryCount = 0;
        int delayMs = 3000;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.TooManyRequests && retryCount < maxRetries)
                {
                    retryCount++;
                    _logger.LogCritical("429 Rate Limit Hit. Waiting {ms}ms...", delayMs);
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                    continue;
                }
                throw;
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
public record MarketDataDto(CurrentPriceDto current_price, Dictionary<string, decimal> market_cap, double market_cap_rank, double price_change_percentage_24h, Dictionary<string, decimal> total_volume, Dictionary<string, decimal> high_24h, Dictionary<string, decimal> low_24h, double circulating_supply, double? total_supply, double? max_supply);
public record CurrentPriceDto(decimal usd);