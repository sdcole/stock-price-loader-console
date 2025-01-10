using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using StockPriceLoader.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;
using Serilog.Sinks.PostgreSQL;


namespace StockPriceLoader
{
    internal static class StockPriceLoader
    {

        private static IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("config.json", optional: false);

        private static IConfigurationRoot config = builder.Build();
        //Alpaca API Keys
        [Required]
        private static readonly string API_KEY = config["API_KEY"];

        [Required]
        private static readonly string API_SECRET = config["API_SECRET"];

        [Required]
        private static readonly string APP_NAME = config["APP_NAME"];

        public static async Task Main(string[] args)
        {

            //Logging for DB setup
            var columnOptions = new Dictionary<string, ColumnWriterBase>
            {
                { "message", new RenderedMessageColumnWriter() },
                { "message_template", new MessageTemplateColumnWriter() },
                { "level", new LevelColumnWriter() },
                { "time_stamp", new TimestampColumnWriter() },
                { "exception", new ExceptionColumnWriter() },
                { "properties", new PropertiesColumnWriter() }
            };

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("app_name", APP_NAME)
                .WriteTo.PostgreSQL(config.GetConnectionString("LoggingConnection"), "logs", columnOptions, needAutoCreateTable: true)
                .CreateLogger();

            Log.Information("App Started");

            try
            {
                //Initial loop to keep app persistant
                while (true)
                {
                    Log.Debug("In Loop");
                    //Check if the market is open
                    if (await MarketIsOpen())
                    {
                        //If market is open get current data and sleep for set increment currently set to 15 seconds.
                        LoadAndPopulateMarketData();
                        await Task.Delay(15000);
                    }
                    else
                    {
                        //If the market is not open sleep for 1 second and check again
                        await Task.Delay(1000);
                    }
                }
            } catch (Exception ex)
            {
                Log.Fatal("An Unhandled Exception Occurred Stopping Application..", ex);
            }
            

        }


        /*
         * MarketIsOpen
         * returns boolean whether the stock market is open.
         * 
         * 
         */
        public static async Task<bool> MarketIsOpen()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Log.Debug("Checking if market is open...");
                    //Set the api headers for the market api
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", API_KEY);
                    client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", API_SECRET);

                    string marketStatus = @"https://api.alpaca.markets/v2/clock";

                    
                    HttpResponseMessage response = await client.GetAsync(marketStatus);
                    Log.Debug("Got a response");
                    string resp = await response.Content.ReadAsStringAsync();
                    // Ensure the request was successful
                    //Throw error if there was issues
                    response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    string content = await response.Content.ReadAsStringAsync();




                    StockMarketStatus status = JsonSerializer.Deserialize<StockMarketStatus>(content);
                    Log.Debug("Current Market Status: " + status.is_open);
                    return status.is_open;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to get the current market status.", ex);
                }
                return false;
            }
        }

        /*
         *  LoadAndPopulateMarketData
         * 
         * This function gets a list of tickers from the DB. Then it appends it to the api call.
         * The api call will be parsed into records for the DB.
         * 
         */
        public static async Task LoadAndPopulateMarketData()
        {
            using (AppDbContext context = new AppDbContext(config))
            {

                Log.Debug("Getting Company List");
                List<Company> companies = context.Companies.ToList();
                string getLastPriceURL = @"https://data.alpaca.markets/v2/stocks/trades/latest?symbols=";


                //This will loop through all companies in the companies table. It appends the data to the get request so the response will contain those tickers.
                foreach (Company company in companies)
                {
                    getLastPriceURL += company.Ticker + ",";
                }

                getLastPriceURL = getLastPriceURL.Substring(0, getLastPriceURL.Length - 1);
                getLastPriceURL += @"&feed=iex&currency=USD";

                using (HttpClient client = new HttpClient())
                {
                    try
                    {


                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", API_KEY);
                        client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", API_SECRET);


                        // Send GET request to the URL
                        //var response = await client.GetAsync(getLastPriceURL);
                        Log.Debug("Getting Current Prices..");
                        HttpResponseMessage response = await client.GetAsync(getLastPriceURL);
                        string resp = await response.Content.ReadAsStringAsync();
                        // Ensure the request was successful
                        //response.EnsureSuccessStatusCode();

                        // Read the response content as a string
                        string content = await response.Content.ReadAsStringAsync();




                        Trades trades = JsonSerializer.Deserialize<Trades>(content);


                        using (IDbContextTransaction transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                // Output some of the data
                                foreach (var trade in trades.trades)
                                {
                                    context.StockPrice.Add(new StockPrice(trade.Key, trade.Value));

                                }
                                Log.Debug("Committing price add to table");
                                context.SaveChanges();
                                transaction.Commit();

                                Log.Information("Process loaded data successfully.");

                            }
                            catch (Exception ex)
                            {
                                Log.Error("Failed to insert records into table, Rolling back..", ex);
                                transaction.Rollback();
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error("An unhandled exception occurred", ex);
                    }
                }
            }
        }
        
    }
}