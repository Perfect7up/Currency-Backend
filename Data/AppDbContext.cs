using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Coin> Coins { get; set; }
    public DbSet<PriceHistory> PriceHistories { get; set; }
    public DbSet<News> NewsItems { get; set; }
    public DbSet<MarketStats> MarketStatistics { get; set; }
}