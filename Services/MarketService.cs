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
        // 1. Get Global Market Data
        var globalUrl = "https://api.coingecko.com/api/v3/global";
        var globalRes = await _httpClient.GetFromJsonAsync<GlobalResponse>(globalUrl);

        // 2. Get Specific BTC and ETH Prices
        var priceUrl = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum&vs_currencies=usd";
        var priceRes = await _httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(priceUrl);

        return new MarketOverview
        {
            TotalMarketCap = globalRes?.data.total_market_cap["usd"] ?? 0,
            TotalVolume = globalRes?.data.total_volume["usd"] ?? 0,
            BtcDominance = globalRes?.data.market_cap_percentage["btc"] ?? 0,
            BtcPrice = priceRes?["bitcoin"]["usd"] ?? 0,
            EthPrice = priceRes?["ethereum"]["usd"] ?? 0,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<List<Coin>> GetTopGainersAsync(int limit)
    {
        var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=price_change_percentage_24h_desc&per_page={limit}&page=1&sparkline=false";
        return await FetchAndMapCoins(url);
    }

    public async Task<List<Coin>> GetTopLosersAsync(int limit)
    {
        var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=price_change_percentage_24h_asc&per_page={limit}&page=1&sparkline=false";
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
            LogoUrl = x.item.small,
            MarketCapRank = x.item.market_cap_rank
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
            LogoUrl = x.image,
            CurrentPrice = x.current_price,
            MarketCapRank = x.market_cap_rank,
            LastUpdated = DateTime.UtcNow
        }).ToList() ?? new List<Coin>();
    }
}

// --- DTOs specifically for Market Service ---
public record GlobalResponse(GlobalData data);
public record GlobalData(
    Dictionary<string, decimal> total_market_cap,
    Dictionary<string, decimal> total_volume,
    Dictionary<string, double> market_cap_percentage
);
public record MarketCoinDto(string id, string symbol, string name, string image, decimal current_price, int market_cap_rank);
public record MarketTrendingDto(List<MarketTrendingItemWrapper> coins);
public record MarketTrendingItemWrapper(MarketTrendingCoinItem item);
public record MarketTrendingCoinItem(string id, string name, string symbol, string small, int market_cap_rank);