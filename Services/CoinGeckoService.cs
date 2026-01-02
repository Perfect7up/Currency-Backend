using System.Net.Http.Json;
using Backend.Models;

namespace Backend.Services;

public interface ICoinService
{
    Task<List<Coin>> GetCoinsAsync(int page, int perPage);
    Task<Coin?> GetCoinByIdAsync(string id);
    Task<List<Coin>> SearchCoinsAsync(string query);
    Task<List<Coin>> GetTrendingCoinsAsync();
    Task<List<Coin>> GetLiveCoinsAsync(int perPage);
    Task<List<PriceHistory>> GetPriceHistoryAsync(string coinId, int days);
}

public class CoinGeckoService : ICoinService
{
    private readonly HttpClient _httpClient;

    public CoinGeckoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Backend-Crypto-App");
    }

    public async Task<List<Coin>> GetCoinsAsync(int page, int perPage)
    {
        var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page={perPage}&page={page}";
        var response = await _httpClient.GetFromJsonAsync<List<CoinGeckoDto>>(url);
        return MapDtoToCoin(response);
    }

    public async Task<Coin?> GetCoinByIdAsync(string id)
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
            MarketCap = (long)x.market_data.market_cap.usd,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<List<Coin>> SearchCoinsAsync(string query)
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
    }

    public async Task<List<Coin>> GetTrendingCoinsAsync()
    {
        var url = "https://api.coingecko.com/api/v3/search/trending";
        var response = await _httpClient.GetFromJsonAsync<CoinGeckoTrendingDto>(url);

        return response?.coins.Select(x => new Coin
        {
            Id = x.item.id,
            Symbol = x.item.symbol.ToUpper(),
            Name = x.item.name,
            Image = x.item.small,
            MarketCapRank = x.item.market_cap_rank
        }).ToList() ?? new List<Coin>();
    }

    public async Task<List<Coin>> GetLiveCoinsAsync(int perPage)
        => await GetCoinsAsync(1, perPage);

    public async Task<List<PriceHistory>> GetPriceHistoryAsync(string coinId, int days)
    {
        var url = $"https://api.coingecko.com/api/v3/coins/{coinId.ToLower()}/market_chart?vs_currency=usd&days={days}&interval=daily";
        var response = await _httpClient.GetFromJsonAsync<CoinHistoryDto>(url);

        return response?.prices.Select(p => new PriceHistory
        {
            CoinId = coinId,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)p[0]).UtcDateTime,
            Price = (decimal)p[1]
        }).ToList() ?? new List<PriceHistory>();
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
            LastUpdated = DateTime.UtcNow
        }).ToList() ?? new List<Coin>();
    }
}

public record CoinGeckoDto(string id, string symbol, string name, string image, decimal current_price, double market_cap, int market_cap_rank);

public record CoinHistoryDto(List<List<double>> prices);

public record CoinGeckoSearchDto(List<CoinGeckoSearchItemDto> coins);
public record CoinGeckoSearchItemDto(string id, string name, string symbol, string thumb, int? market_cap_rank);

public record CoinGeckoTrendingDto(List<TrendingItemWrapper> coins);
public record TrendingItemWrapper(TrendingCoinItem item);
public record TrendingCoinItem(string id, string name, string symbol, string small, int market_cap_rank);

public record CoinDetailDto(string id, string symbol, string name, ImageDto image, MarketDataDto market_data);
public record ImageDto(string large);
public record MarketDataDto(CurrentPriceDto current_price, MarketCapDto market_cap, double market_cap_rank);
public record CurrentPriceDto(decimal usd);
public record MarketCapDto(decimal usd);