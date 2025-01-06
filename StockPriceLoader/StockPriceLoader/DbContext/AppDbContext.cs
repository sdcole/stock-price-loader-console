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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configuration.GetConnectionString("AppConnection");
        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        // Configure StockRecord entity
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
    }
}
