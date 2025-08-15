using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using StockPriceLoader.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using StockPriceLoader.Helpers;
using StockPriceLoader.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace StockPriceLoader
{
    public static class StockPriceLoader
    {

        public static async Task Main(string[] args)
        {
            ConfigurationService.Initialize();
            //First we encrypt the contents of the app config file.
            //No need to try because if this fails the app cannot continue.
            EncryptionHelper.EncryptSensitiveConfigData(new[] { "ConnectionStrings:LoggingConnection", "ConnectionStrings:AppConnection", "API_KEY", "API_SECRET" });

            //Logging for DB setup
            var columnOptions = new Dictionary<string, ColumnWriterBase>
            {
                { "message", new RenderedMessageColumnWriter() },
                { "message_template", new MessageTemplateColumnWriter() },
                { "level", new LevelColumnWriter() },
                { "timestamp", new TimestampColumnWriter() },
                { "exception", new ExceptionColumnWriter() },
                { "properties", new PropertiesColumnWriter() }
            };

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("app_name", ConfigurationService.Configuration["APP_NAME"])
                .WriteTo.PostgreSQL(EncryptionHelper.Decrypt(ConfigurationService.Configuration["ConnectionStrings:LoggingConnection"]), "logs", columnOptions, needAutoCreateTable: true)
                .CreateLogger();

            Log.Information($"App {ConfigurationService.Configuration["APP_NAME"]} has Started");
            try
            {


                
                
                //Initial loop to keep app persistant
                while (true)
                {
                    Log.Debug("In Loop");
                    //Check if the market is open
                    StockMarketStatus marketStatus = await MarketHelper.MarketIsOpen();
                    if (marketStatus.is_open)
                    {
                        
                        MinuteBarsHelper.LoadAndPopulateBarsData();

                        //Since we are using await calls I want to calculate time until next minute.
                        var now = DateTime.UtcNow;
                        //Add 10 seconds to the minute to give Alpaca API some time to get most up to date info.
                        var delay = TimeSpan.FromMilliseconds((60000 - now.Second * 1000 - now.Millisecond) + 10000);
                        await Task.Delay(delay);
                    }
                    else
                    {
                        //Load the bar info for the entire day after the market closes.
                        await DailyBarsHelper.LoadAndPopulateDailyBarsData();
                        await SymbolDailySummaryHelper.CalculateAndLoadSymbolDailySummary();
                        //await CalculateAndLoadSectorDailySummary();
                        //Afer we load the days bar data we then summarize the data and load it into the db.
                        Log.Information("Sleeping till market open: " + marketStatus.next_open);
                        //If the market is not open sleep until it opens
                        //This is done in utc so that it matches server time.
                        DateTime nextOpenTimeUtc = marketStatus.next_open.ToUniversalTime();

                        TimeSpan delay = nextOpenTimeUtc - DateTime.UtcNow;
                        if (delay.TotalMilliseconds > 0)
                        {
                            await Task.Delay(delay);
                        }
                        else
                        {
                            //just loop again because the market is now open
                        }
                    }
                }
                
                
                
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An Unhandled Exception Occurred Stopping Application..");
            }
            //Close out logging allow any current data to update
            Log.CloseAndFlush();

        }


        

        
        


        

        

        


        /**
         * CalculateAndLoadSectorDailySummary
         * 
         * This function will pull data from multiple places including (sector, symbol_daily_summaries, and daily_bars) to calculate the sector summary data.
         * Weighted averages will be calculated to better represent the sector as a whole.
         * 
         **/
        public static async Task CalculateAndLoadSectorDailySummary(){
            try
            {
                Log.Information("Begin Calulcating and Loading the Sector Daily Data.");
                //TODO: Get SymbolDailySummaries and DailyBars from the database and calculate the weighted averages.
                using (AppDbContext context = new AppDbContext())
                {   List<Sector> sectorsList = await context.Sectors.ToListAsync();
                    foreach (Sector sector in sectorsList)
                    {
                        //Get the company symbol for the sector.
                        List<string> companySymbols = await context.Companies.Where(d => d.SectorId == sector.Id).Select(d => d.Symbol).ToListAsync();
                        List<SymbolDailySummary> symbolSummaries = await context.SymbolDailySummaries.Where(d => companySymbols.Contains(d.Symbol)).ToListAsync();
                        List<DailyBarData> dailyBarDatas = await context.DailyBars.Where(d => companySymbols.Contains(d.Symbol)).ToListAsync();

                        SectorDailySummary sectorDailySummary = new SectorDailySummary();
                        //TODO: First thing we need to do is calculate the entire volume for that day for the given sector..
                        sectorDailySummary.SectorId = sector.Id;

                        
                        sectorDailySummary.SymbolCount = companySymbols.Count;
                        sectorDailySummary.AvgReturn1D = symbolSummaries.Average(d => d.Return1d);

                        //Get the entire sectors volume for the day.
                        List<long> volumes = dailyBarDatas.Select(d => d.Volume).ToList();
                        long totalVolume = MathHelper.Sum(volumes);
                        double currentWeightedReturn = 0.0;

                        foreach (DailyBarData dailyBar in dailyBarDatas)
                        {
                            //we need to calculate the return by volume
                            double return1d = dailyBar.Volume *  symbolSummaries
                                .Where(s => s.Symbol == dailyBar.Symbol)
                                .Select(s => s.Return1d ?? 0)
                                .FirstOrDefault();
                            currentWeightedReturn += return1d;
                            
                            
                        }

                        double weightedReturn1D = currentWeightedReturn / totalVolume;


                        sectorDailySummary.MedianReturn1D = MathHelper.Median(symbolSummaries.Select(d => d.Return1d));
                        sectorDailySummary.AvgReturn5D = symbolSummaries.Average(d => d.Return5d);
                        sectorDailySummary.MedianReturn5D = MathHelper.Median(symbolSummaries.Select(d => d.Return5d));
                        sectorDailySummary.AvgBollingerBandwidth = symbolSummaries.Average(d => d.BollingerBandwidth);
                        sectorDailySummary.MedianBollingerBandwidth = MathHelper.Median(symbolSummaries.Select(d => d.BollingerBandwidth));
                        


                    }
                    
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "There was an issue calculating and loading sector daily summary data.");
            }
            
        }
    }

}