namespace Backend.Models;

public class MarketOverview
{
    public int Id { get; set; }
    public decimal TotalMarketCap { get; set; }
    public decimal TotalVolume { get; set; }
    public double BtcDominance { get; set; }
    public decimal BtcPrice { get; set; }
    public decimal EthPrice { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}