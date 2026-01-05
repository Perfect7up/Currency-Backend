using System.Net.Http.Json;
using Backend.Models;

namespace Backend.Services;

public interface IMarketService
{
    Task<MarketOverview> GetMarketOverviewAsync();
    Task<List<Coin>> GetTopGainersAsync(int limit);
    Task<List<Coin>> GetTopLosersAsync(int limit);
    Task<List<Coin>> GetTrendingAsync(int limit);
}

public class MarketService : IMarketService
{
    private readonly HttpClient _httpClient;

    public MarketService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Backend-Crypto-App");
    }

    public async Task<MarketOverview> GetMarketOverviewAsync()
    {
        var globalUrl = "https://api.coingecko.com/api/v3/global";
        var globalRes = await _httpClient.GetFromJsonAsync<GlobalResponse>(globalUrl);

        var priceUrl = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum&vs_currencies=usd";
        var priceRes = await _httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(priceUrl);

        return new MarketOverview
        {
            TotalMarketCap = globalRes?.data.total_market_cap.GetValueOrDefault("usd") ?? 0,
            MarketCapChange = globalRes?.data.market_cap_change_percentage_24h_usd ?? 0,
            TotalVolume = globalRes?.data.total_volume.GetValueOrDefault("usd") ?? 0,
            VolumeChange = 0,
            BtcDominance = globalRes?.data.market_cap_percentage.GetValueOrDefault("btc") ?? 0,
            BtcPrice = priceRes?.GetValueOrDefault("bitcoin")?.GetValueOrDefault("usd") ?? 0,
            EthPrice = priceRes?.GetValueOrDefault("ethereum")?.GetValueOrDefault("usd") ?? 0,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<List<Coin>> GetTopGainersAsync(int limit)
    {
        var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=price_change_percentage_24h_desc&per_page={limit}&page=1&price_change_percentage=24h";
        return await FetchAndMapCoins(url);
    }

    public async Task<List<Coin>> GetTopLosersAsync(int limit)
    {
        var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=price_change_percentage_24h_asc&per_page={limit}&page=1&price_change_percentage=24h";
        return await FetchAndMapCoins(url);
    }

    public async Task<List<Coin>> GetTrendingAsync(int limit)
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