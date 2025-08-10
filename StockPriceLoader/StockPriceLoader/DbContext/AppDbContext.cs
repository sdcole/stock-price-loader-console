using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using StockPriceLoader.Helpers;
using StockPriceLoader.Models;
using StockPriceLoader.Services;

public class AppDbContext : DbContext
{

    public AppDbContext()
    {
        
    }

    public DbSet<Company> Companies { get; set; }
    public DbSet<MinuteBarData> MinuteBars { get; set; }
    public DbSet<DailyBarData> DailyBars { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (ConfigurationService.Configuration["ConnectionStrings:AppConnection"] == null)
        {
            Exception ex = new Exception("Connection string 'AppConnection' is not configured in appsettings.json.");
            Log.Error(ex, "Connection string 'AppConnection' is not configured in appsettings.json.");
            throw ex;
        }

        var connectionString = EncryptionHelper.Decrypt(ConfigurationService.Configuration["ConnectionStrings:AppConnection"]);
        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Company entity
        modelBuilder.Entity<Company>().ToTable("companies");

        modelBuilder.Entity<Company>()
            .Property(c => c.Id).HasColumnName("id").IsRequired();

        modelBuilder.Entity<Company>()
            .Property(c => c.Symbol).HasColumnName("symbol").IsRequired().HasMaxLength(10);

        modelBuilder.Entity<Company>()
            .Property(c => c.CompanyDescription).HasColumnName("company_description").IsRequired();

        modelBuilder.Entity<Company>()
            .Property(c => c.Sector).HasColumnName("sector").IsRequired().HasMaxLength(100);

        // Configure MinuteBarData entity
        modelBuilder.Entity<MinuteBarData>().ToTable("minute_bars");

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.Id).HasColumnName("id").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.Symbol).HasColumnName("symbol").IsRequired().HasMaxLength(10);

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.Timestamp).HasColumnName("timestamp").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.Open).HasColumnName("open").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.High).HasColumnName("high").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.Low).HasColumnName("low").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.Close).HasColumnName("close").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.Volume).HasColumnName("volume").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.TradeCount).HasColumnName("trade_count").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .Property(b => b.VW).HasColumnName("vw").IsRequired();

        modelBuilder.Entity<MinuteBarData>()
            .HasIndex(b => new { b.Symbol, b.Timestamp }).IsUnique();

        // Configure DailyBarData entity
        modelBuilder.Entity<DailyBarData>().ToTable("daily_bars");

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.Id).HasColumnName("id").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.Symbol).HasColumnName("symbol").IsRequired().HasMaxLength(10);

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.Timestamp).HasColumnName("timestamp").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.Open).HasColumnName("open").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.High).HasColumnName("high").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.Low).HasColumnName("low").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.Close).HasColumnName("close").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.Volume).HasColumnName("volume").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.TradeCount).HasColumnName("trade_count").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .Property(b => b.VW).HasColumnName("vw").IsRequired();

        modelBuilder.Entity<DailyBarData>()
            .HasIndex(b => new { b.Symbol, b.Timestamp }).IsUnique();
    }
}
