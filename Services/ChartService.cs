using Backend.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace Backend.Services;

public interface IChartService
{
    Task<List<OhlcvPoint>> GetOhlcvAsync(string coinId, string period);
}

public class ChartService : IChartService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ChartService> _logger;
    private readonly string _apiKey;

    public ChartService(HttpClient httpClient, IMemoryCache cache, ILogger<ChartService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey = config["CryptoCompare:ApiKey"] ?? "";

        _httpClient.BaseAddress = new Uri("https://min-api.cryptocompare.com/data/v2/");
        if (!string.IsNullOrEmpty(_apiKey))
            _httpClient.DefaultRequestHeaders.Add("authorization", $"Apikey {_apiKey}");
    }

    public async Task<List<OhlcvPoint>> GetOhlcvAsync(string coinId, string period)
    {
        string symbol = GetSymbolFromId(coinId);
        string cacheKey = $"ohlcv_{symbol}_{period}";

        if (!_cache.TryGetValue(cacheKey, out List<OhlcvPoint>? data))
        {
            try
            {
                var (endpoint, limit) = period.ToLower() switch
                {
                    "1m" => ("histominute", 1440),
                    "5m" => ("histominute", 1000),
                    "1h" => ("histohour", 720),
                    "1d" => ("histoday", 500),
                    _ => ("histoday", 500)
                };

                var url = $"{endpoint}?fsym={symbol}&tsym=USD&limit={limit}";
                var response = await _httpClient.GetFromJsonAsync<CryptoCompareChartResponse>(url);

                data = response?.Data?.Data?.Select(x => new OhlcvPoint
                {
                    Time = x.time,
                    Open = x.open,
                    High = x.high,
                    Low = x.low,
                    Close = x.close,
                    Volume = x.volumeto
                }).ToList() ?? new List<OhlcvPoint>();

                _cache.Set(cacheKey, data, TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OHLCV for {Symbol}", symbol);
                return new List<OhlcvPoint>();
            }
        }
        return data ?? new();
    }

    private string GetSymbolFromId(string id) => id.ToLower() switch
    {
        "bitcoin" => "BTC",
        "ethereum" => "ETH",
        "solana" => "SOL",
        "cardano" => "ADA",
        "ripple" => "XRP",
        _ => id.ToUpper()
    };
}

public record CryptoCompareChartResponse(ChartDataContainer Data);
public record ChartDataContainer(List<CryptoCompareCandle> Data);
public record CryptoCompareCandle(long time, decimal open, decimal high, decimal low, decimal close, decimal volumeto);