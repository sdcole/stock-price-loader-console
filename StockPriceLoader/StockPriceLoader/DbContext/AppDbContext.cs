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
    public DbSet<Sector> Sectors { get; set; }
    public DbSet<SymbolDailySummary> SymbolDailySummaries { get; set; }


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
            .Property(c => c.SectorId).HasColumnName("sector_id").IsRequired();


        //Sectors
        modelBuilder.Entity<Sector>().ToTable("sectors");

        modelBuilder.Entity<Sector>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<Sector>()
            .Property(s => s.Id)
            .HasColumnName("id")
            .IsRequired();

        modelBuilder.Entity<Sector>()
            .Property(s => s.SectorName)
            .HasColumnName("sector_name")
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<Sector>()
            .Property(s => s.SectorType)
            .HasColumnName("sector_type")
            .IsRequired()
            .HasMaxLength(50);

        modelBuilder.Entity<Sector>()
            .Property(s => s.Description)
            .HasColumnName("description")
            .HasMaxLength(500);



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

        // Configure SymbolDailySummary entity
        modelBuilder.Entity<SymbolDailySummary>().ToTable("symbol_daily_summaries");
        modelBuilder.Entity<DailyBarData>().HasKey(s => s.Id);
        modelBuilder.Entity<SymbolDailySummary>()
            .Property(s => s.Id).HasColumnName("id").IsRequired();
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Symbol)
            .HasColumnName("symbol")
            .IsRequired()
            .HasMaxLength(10);
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Date).HasColumnName("date").IsRequired();
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Return1d).HasColumnName("return_1d");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Return5d).HasColumnName("return_5d");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Volatility5d).HasColumnName("volatility_5d");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Volatility10d).HasColumnName("volatility_10d");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Sma5).HasColumnName("sma_5");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Sma10).HasColumnName("sma_10");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.Rsi14).HasColumnName("rsi_14");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.BollingerBandwidth).HasColumnName("bollinger_bandwidth");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.VolumeAvg5d).HasColumnName("volume_avg_5d");
        modelBuilder.Entity<SymbolDailySummary>().Property(s => s.VolumeRatio).HasColumnName("volume_ratio");
        /*
         * 
         * 
         * public long Id { get; set; }
        [Required]
        public string Symbol { get; set; }
        [Required]
        public DateTime Date { get; set; }

        // Price & Volume Features
        public double? Return1d { get; set; }
        public double? Return5d { get; set; }
        public double? Volatility5d { get; set; }
        public double? Volatility10d { get; set; }
        public double? Sma5 { get; set; }
        public double? Sma10 { get; set; }
        public double? Rsi14 { get; set; }
        public double? BollingerBandwidth { get; set; }
        public double? VolumeAvg5d { get; set; }
        public double? VolumeRatio { get; set; }

        */
    }
}
