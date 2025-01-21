using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StockPriceLoader.Models;

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbSet<Company> Companies { get; set; }
    public DbSet<StockPrice> StockPrice { get; set; }
    public DbSet<BarData> MinuteBars { get; set; }  // Add this DbSet for BarData
    public DbSet<BarData> DailyBars { get; set; }  // Add this DbSet for BarData

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configuration.GetConnectionString("AppConnection");
        optionsBuilder.UseNpgsql(connectionString);  // Use PostgreSQL, as indicated
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the Company entity
        modelBuilder.Entity<Company>()
            .ToTable("companies");

        modelBuilder.Entity<Company>()
            .Property(c => c.Id)
            .HasColumnName("id")
            .IsRequired();

        modelBuilder.Entity<Company>()
            .Property(c => c.Ticker)
            .HasColumnName("ticker")
            .IsRequired()
            .HasMaxLength(10);

        modelBuilder.Entity<Company>()
            .Property(c => c.CompanyDescription)
            .HasColumnName("company_description")
            .IsRequired();

        modelBuilder.Entity<Company>()
            .Property(c => c.Sector)
            .HasColumnName("sector")
            .IsRequired()
            .HasMaxLength(100);

        // Configure StockPrice entity
        modelBuilder.Entity<StockPrice>()
            .ToTable("stock_prices");

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.Id)
            .HasColumnName("id")
            .IsRequired();

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.Ticker)
            .HasColumnName("ticker")
            .IsRequired()
            .HasMaxLength(10);

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.TradePrice)
            .HasColumnName("trade_price")
            .IsRequired();

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.TradeSize)
            .HasColumnName("trade_size")
            .IsRequired();

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.Exchange)
            .HasColumnName("exchange")
            .IsRequired()
            .HasMaxLength(10);

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.Tape)
            .HasColumnName("tape")
            .IsRequired()
            .HasMaxLength(1);

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.Conditions)
            .HasColumnName("conditions");

        modelBuilder.Entity<StockPrice>()
            .Property(sr => sr.UpdateStatus)
            .HasColumnName("update_status");

        // Configure BarData entity
        modelBuilder.Entity<BarData>()
            .ToTable("minute_bars");  // Table name in the database

        modelBuilder.Entity<BarData>()
            .Property(b => b.Id)
            .HasColumnName("id")
            .IsRequired();  // Primary Key, auto-generated

        modelBuilder.Entity<BarData>()
            .Property(b => b.Symbol)
            .HasColumnName("symbol")
            .IsRequired()
            .HasMaxLength(10);  // Max length for stock symbols like "AAPL", "MSFT"

        modelBuilder.Entity<BarData>()
            .Property(b => b.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();  // Timestamp in RFC-3339 format

        modelBuilder.Entity<BarData>()
            .Property(b => b.Open)
            .HasColumnName("open")
            .IsRequired();  // Opening price

        modelBuilder.Entity<BarData>()
            .Property(b => b.High)
            .HasColumnName("high")
            .IsRequired();  // Highest price

        modelBuilder.Entity<BarData>()
            .Property(b => b.Low)
            .HasColumnName("low")
            .IsRequired();  // Lowest price

        modelBuilder.Entity<BarData>()
            .Property(b => b.Close)
            .HasColumnName("close")
            .IsRequired();  // Closing price

        modelBuilder.Entity<BarData>()
            .Property(b => b.Volume)
            .HasColumnName("volume")
            .IsRequired();  // Trade volume

        modelBuilder.Entity<BarData>()
            .Property(b => b.TradeCount)
            .HasColumnName("trade_count")
            .IsRequired();  // Number of trades

        modelBuilder.Entity<BarData>()
            .Property(b => b.VW)
            .HasColumnName("vw")
            .IsRequired();  // Volume Weighted Average Price

        // Define unique constraint for Symbol and Timestamp
        modelBuilder.Entity<BarData>()
            .HasIndex(b => new { b.Symbol, b.Timestamp })
            .IsUnique();



        // Configure BarData entity
        modelBuilder.Entity<BarData>()
            .ToTable("daily_bars");  // Table name in the database

        modelBuilder.Entity<BarData>()
            .Property(b => b.Id)
            .HasColumnName("id")
            .IsRequired();  // Primary Key, auto-generated

        modelBuilder.Entity<BarData>()
            .Property(b => b.Symbol)
            .HasColumnName("symbol")
            .IsRequired()
            .HasMaxLength(10);  // Max length for stock symbols like "AAPL", "MSFT"

        modelBuilder.Entity<BarData>()
            .Property(b => b.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();  // Timestamp in RFC-3339 format

        modelBuilder.Entity<BarData>()
            .Property(b => b.Open)
            .HasColumnName("open")
            .IsRequired();  // Opening price

        modelBuilder.Entity<BarData>()
            .Property(b => b.High)
            .HasColumnName("high")
            .IsRequired();  // Highest price

        modelBuilder.Entity<BarData>()
            .Property(b => b.Low)
            .HasColumnName("low")
            .IsRequired();  // Lowest price

        modelBuilder.Entity<BarData>()
            .Property(b => b.Close)
            .HasColumnName("close")
            .IsRequired();  // Closing price

        modelBuilder.Entity<BarData>()
            .Property(b => b.Volume)
            .HasColumnName("volume")
            .IsRequired();  // Trade volume

        modelBuilder.Entity<BarData>()
            .Property(b => b.TradeCount)
            .HasColumnName("trade_count")
            .IsRequired();  // Number of trades

        modelBuilder.Entity<BarData>()
            .Property(b => b.VW)
            .HasColumnName("vw")
            .IsRequired();  // Volume Weighted Average Price

        // Define unique constraint for Symbol and Timestamp
        modelBuilder.Entity<BarData>()
            .HasIndex(b => new { b.Symbol, b.Timestamp })
            .IsUnique();


    }
}
