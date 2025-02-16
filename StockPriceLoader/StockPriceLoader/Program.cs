﻿using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using StockPriceLoader.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using StockPriceLoader.Helpers;


namespace StockPriceLoader
{
    internal static class StockPriceLoader
    {

        private static IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("config.json", optional: false, reloadOnChange: true);

        private static IConfigurationRoot config = builder.Build();

        [Required]
        private static readonly string APP_NAME = config["APP_NAME"];

        public static async Task Main(string[] args)
        {

            


            try
            {
                // Check if the connection string is already encrypted (simple Base64 check)
                if (!EncryptionHelper.IsEncrypted(config.GetConnectionString("LoggingConnection")))
                {
                    
                    string encryptedConnectionString = EncryptionHelper.Encrypt(config.GetConnectionString("LoggingConnection"));

                    // Update config.json with the encrypted string
                    EncryptionHelper.UpdateConfigFile(encryptedConnectionString, "ConnectionStrings:LoggingConnection");
                }

                // Check if the connection string is already encrypted (simple Base64 check)
                if (!EncryptionHelper.IsEncrypted(config.GetConnectionString("AppConnection")))
                {
                    
                    string encryptedConnectionString = EncryptionHelper.Encrypt(config.GetConnectionString("AppConnection"));

                    // Update config.json with the encrypted string
                    EncryptionHelper.UpdateConfigFile(encryptedConnectionString, "ConnectionStrings:AppConnection");
                }
                // Check if the connection string is already encrypted (simple Base64 check)
                if (!EncryptionHelper.IsEncrypted(config["API_KEY"]))
                {

                    string encryptedConnectionString = EncryptionHelper.Encrypt(config["API_KEY"]);

                    // Update config.json with the encrypted string
                    EncryptionHelper.UpdateConfigFile(encryptedConnectionString, "API_KEY");
                }
                // Check if the connection string is already encrypted (simple Base64 check)
                if (!EncryptionHelper.IsEncrypted(config["API_SECRET"]))
                {

                    string encryptedConnectionString = EncryptionHelper.Encrypt(config["API_SECRET"]);

                    // Update config.json with the encrypted string
                    EncryptionHelper.UpdateConfigFile(encryptedConnectionString, "API_SECRET");
                }

                config.Reload();
            }
            catch (Exception ex)
            {
                
            }

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
                .WriteTo.PostgreSQL(EncryptionHelper.Decrypt(config.GetConnectionString("LoggingConnection")), "logs", columnOptions, needAutoCreateTable: true)
                .CreateLogger();

            Log.Information("App Started");
            try
            {

                //Initial loop to keep app persistant
                while (true)
                {
                    Log.Debug("In Loop");
                    //Check if the market is open
                    StockMarketStatus marketStatus = await MarketIsOpen();

                    if (marketStatus.is_open)
                    {

                        LoadAndPopulateBarsData();
                        //If market is open get current data and sleep for set increment currently set to 15 seconds.
                        //LoadAndPopulateMarketData();
                        await Task.Delay(60000);
                    }
                    else
                    {
                        //Load the bar info for the entire day
                        await LoadAndPopulateDailyBarsData();
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
            } catch (Exception ex)
            {
                Log.Fatal(ex, "An Unhandled Exception Occurred Stopping Application..");
            }
            //Close out logging allow any current data to update
            Log.CloseAndFlush();

        }


        /*
         * MarketIsOpen
         * returns boolean whether the stock market is open.
         * 
         * 
         */
        public static async Task<StockMarketStatus> MarketIsOpen()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Log.Debug("Checking if market is open...");
                    //Set the api headers for the market api
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", EncryptionHelper.Decrypt(config["API_KEY"]));
                    client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", EncryptionHelper.Decrypt(config["API_SECRET"]));

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
                    return status;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to get the current market status.", ex);
                }
                return null;
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
                        client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", EncryptionHelper.Decrypt(config["API_KEY"]));
                        client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", EncryptionHelper.Decrypt(config["API_SECRET"]));


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
                                    context.StockPrices.Add(new StockPrice(trade.Key, trade.Value));

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





        /*
                 *  LoadAndPopulateBarsData
                 * 
                 * This function gets a list of tickers from the DB. Then it appends it to the api call.
                 * The api call will be parsed into records for the DB.
                 * 
                 */
        public static async Task LoadAndPopulateBarsData()
        {
            using (AppDbContext context = new AppDbContext(config))
            {
                try
                {
                    Log.Debug("Getting Company List");
                    List<Company> companies = context.Companies.ToList();
                    string getLastPriceURL = @"https://data.alpaca.markets/v2/stocks/bars/latest?symbols=";


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
                            client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", EncryptionHelper.Decrypt(config["API_KEY"]));
                            client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", EncryptionHelper.Decrypt(config["API_SECRET"]));


                            // Send GET request to the URL
                            //var response = await client.GetAsync(getLastPriceURL);
                            Log.Debug("Getting Current Prices..");
                            HttpResponseMessage response = await client.GetAsync(getLastPriceURL);
                            string resp = await response.Content.ReadAsStringAsync();
                            // Ensure the request was successful
                            //response.EnsureSuccessStatusCode();

                            // Read the response content as a string
                            string content = await response.Content.ReadAsStringAsync();




                            BarResponse bars = JsonSerializer.Deserialize<BarResponse>(content);


                            using (IDbContextTransaction transaction = context.Database.BeginTransaction())
                            {
                                try
                                {
                                    // Output some of the data
                                    foreach (var bar in bars.bars)
                                    {
                                        context.MinuteBars.Add(new MinuteBarData(bar.Key, bar.Value));

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
                            Log.Error("An unhandled exception occurred", ex.ToString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("An unhandled exception occurred", ex.ToString());
                }
            }
        }




        /**
         *  LoadAndPopulateDailyBarsData
         *  
         *  This will average a day's worth of data and load it into the db.
         * 
         * 
         * 
         **/
        public static async Task LoadAndPopulateDailyBarsData()
        {
            using (AppDbContext context = new AppDbContext(config))
            {
                try
                {
                    List<Company> companies = context.Companies.ToList();
                    string getLastPriceURL = @"https://data.alpaca.markets/v2/stocks/bars?symbols=";


                    //This will loop through all companies in the companies table. It appends the data to the get request so the response will contain those tickers.
                    foreach (Company company in companies)
                    {
                        getLastPriceURL += company.Ticker + ",";
                    }

                    getLastPriceURL = getLastPriceURL.Substring(0, getLastPriceURL.Length - 1);
                    getLastPriceURL += @"&timeframe=1D&start=" + DateTime.UtcNow.Date.ToString("yyyy-MM-dd") + "&end=" + DateTime.UtcNow.Date.ToString("yyyy-MM-dd") + "&limit=1000&adjustment=raw&feed=iex&currency=USD&sort=asc";

                    using (HttpClient client = new HttpClient())
                    {
                        try
                        {


                            client.DefaultRequestHeaders.Add("Accept", "application/json");
                            client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", EncryptionHelper.Decrypt(config["API_KEY"]));
                            client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", EncryptionHelper.Decrypt(config["API_SECRET"]));


                            // Send GET request to the URL
                            //var response = await client.GetAsync(getLastPriceURL);

                            HttpResponseMessage response = await client.GetAsync(getLastPriceURL);
                            string resp = await response.Content.ReadAsStringAsync();
                            // Ensure the request was successful
                            //response.EnsureSuccessStatusCode();

                            // Read the response content as a string
                            string content = await response.Content.ReadAsStringAsync();




                            HistoricalBarResponse bars = JsonSerializer.Deserialize<HistoricalBarResponse>(content);


                            using (IDbContextTransaction transaction = context.Database.BeginTransaction())
                            {
                                try
                                {
                                    // Output some of the data
                                    foreach (var bar in bars.bars)
                                    {
                                        context.DailyBars.Add(new DailyBarData(bar.Key, bar.Value[0]));

                                    }

                                    context.SaveChanges();
                                    transaction.Commit();



                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "There was an issue saving to database");
                                    transaction.Rollback();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "There was an issue calling the Alpaca Market API");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "There was an issue getting Company list from database");
                }
            }
        }



    }
}