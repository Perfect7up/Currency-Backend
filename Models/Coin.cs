using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Coin
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public long MarketCap { get; set; }
    public int MarketCapRank { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PriceHistory
{
    public int Id { get; set; }
    public string CoinId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}

public class News
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class MarketStats
{
    public int Id { get; set; }
    public string CoinId { get; set; } = string.Empty;
}


public record CoinGeckoSearchDto(List<CoinGeckoSearchItemDto> coins);
public record CoinGeckoSearchItemDto(string id, string name, string symbol, string thumb, int? market_cap_rank);
public record CoinGeckoTrendingDto(List<TrendingItemWrapper> coins);
public record TrendingItemWrapper(TrendingCoinItem item);
public record TrendingCoinItem(string id, string name, string symbol, string small, int market_cap_rank, double price_btc);
public record CoinDetailDto(
    string id,
    string symbol,
    string name,
    DescriptionDto description,
    ImageDto image,
    MarketDataDto market_data
);
public record DescriptionDto(string en);
public record ImageDto(string large);
public record MarketDataDto(CurrentPriceDto current_price, double market_cap_rank, long market_cap_change_24h);
public record CurrentPriceDto(decimal usd);