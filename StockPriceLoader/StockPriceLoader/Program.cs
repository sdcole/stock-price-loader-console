﻿using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using StockPriceLoader.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using StockPriceLoader.Helpers;
using StockPriceLoader.Services;
using Microsoft.EntityFrameworkCore;

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

                        LoadAndPopulateBarsData();

                        //Since we are using await calls I want to calculate time until next minute.
                        var now = DateTime.UtcNow;
                        //Add 10 seconds to the minute to give Alpaca API some time to get most up to date info.
                        var delay = TimeSpan.FromMilliseconds((60000 - now.Second * 1000 - now.Millisecond) + 10000);
                        

                        await Task.Delay(delay);
                    }
                    else
                    {
                        //Load the bar info for the entire day after the market closes.
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
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An Unhandled Exception Occurred Stopping Application..");
            }
            //Close out logging allow any current data to update
            Log.CloseAndFlush();

        }





        /*
        *  LoadAndPopulateBarsData
        * 
        * This function gets a list of tickers from the DB. 
        * Then it kicks off async api calls so that it can handle large lists.
        * 
        */
        public static async Task LoadAndPopulateBarsData()
        {
            using (AppDbContext context = new AppDbContext())
            {
                try
                {
                    Log.Debug("Getting Company List");
                    List<Company> companies = context.Companies.ToList();
                    string getLastPriceURL = @"https://data.alpaca.markets/v2/stocks/bars/latest?feed=iex&currency=USD&symbols=";
                    //The character count of this + the other necessary gets is 80
                    // max get req length is 2048 so 2048 - 
                    string apiGetReq = getLastPriceURL;

                    foreach (Company company in companies)
                    {

                        //This makes sure we dont go over the 2048 character limit.
                        if (apiGetReq.Length >= 2043)
                        {
                            apiGetReq = apiGetReq.Substring(0, apiGetReq.Length - 1);
                            CallApiAndLoadMinuteData(apiGetReq);
                            apiGetReq = getLastPriceURL;
                        }
                        else
                        {
                            apiGetReq += company.Symbol + ",";
                        }

                    }
                    apiGetReq = apiGetReq.Substring(0, apiGetReq.Length - 1);
                    CallApiAndLoadMinuteData(apiGetReq);
                }
                catch (Exception ex)
                {
                    Log.Error("An unhandled exception occurred", ex.ToString());
                }
            }
        }


        public static async Task CallApiAndLoadMinuteData(string apiGetReq)
        {
            using (AppDbContext context = new AppDbContext())
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {


                        APIHelper.ConfigureHTTPClient(client);


                        // Send GET request to the URL
                        Log.Debug("Getting Current Prices..");
                        //Sleep 10 seconds to the api can catch up..
                        //await Task.Delay(10000);
                        HttpResponseMessage response = await client.GetAsync(apiGetReq);
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
                                int amountInserted = 0;
                                int amountIgnored = 0;

                                // Build the SQL values string for bulk insert
                                var sqlValues = string.Join(", ", bars.bars.Select(bar =>
                                    $"('{bar.Key}', '{bar.Value.t:yyyy-MM-dd HH:mm:ss}', {bar.Value.o}, {bar.Value.h}, {bar.Value.l}, {bar.Value.c}, {bar.Value.v}, {bar.Value.n}, {bar.Value.vw})"));

                                // Build the complete SQL query for the bulk insert
                                var sql = $@"
                                    INSERT INTO minute_bars (symbol, timestamp, open, high, low, close, volume, trade_count, vw)
                                    VALUES {sqlValues}
                                    ON CONFLICT (symbol, timestamp) DO NOTHING;";  // Handle conflict by doing nothing for duplicates

                                // Execute the raw SQL query
                                var rowsAffected = await context.Database.ExecuteSqlRawAsync(sql);

                                // Assuming `rowsAffected` gives the number of successfully inserted rows
                                amountInserted = rowsAffected;
                                amountIgnored = bars.bars.Count - amountInserted;
                                // If any records were inserted, log the results
                                Log.Information($"Process minute data successfully. Number Inserted: {amountInserted} Number of Duplicates Ignored: {amountIgnored}");

                                // Commit the transaction
                                transaction.Commit();

                            }
                            catch (Exception ex)
                            {
                                // In case of error, roll back the transaction
                                Log.Error("Failed to insert records into table, Rolling back...", ex);
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
            using (AppDbContext context = new AppDbContext())
            {
                try
                {
                    List<Company> companies = context.Companies.ToList();
                    string getLastPriceURL = @"https://data.alpaca.markets/v2/stocks/bars?timeframe=1D&start=" + DateTime.UtcNow.Date.ToString("yyyy-MM-dd") + "&end=" + DateTime.UtcNow.Date.ToString("yyyy-MM-dd") + "&limit=5000&adjustment=raw&feed=iex&currency=USD&sort=asc&symbols=";
                    //235 chars
                    string apiGetReq = getLastPriceURL;
                    //This will loop through all companies in the companies table. It appends the data to the get request so the response will contain those tickers.
                    foreach (Company company in companies)
                    {
                        

                        //This makes sure we dont go over the 2048 character limit.
                        if (apiGetReq.Length >= 2043)
                        {
                            apiGetReq = apiGetReq.Substring(0, apiGetReq.Length - 1);
                            CallApiAndLoadDailyData(apiGetReq);
                            apiGetReq = getLastPriceURL;
                        }
                        else
                        {
                            apiGetReq += company.Symbol + ",";
                        }
                    }
                    apiGetReq = apiGetReq.Substring(0, apiGetReq.Length - 1);
                    CallApiAndLoadDailyData(apiGetReq);




                }
                catch (Exception ex)
                {
                    Log.Error(ex, "There was an issue getting Company list from database");
                }
            }
        }

        public static async Task CallApiAndLoadDailyData(string apiGetReq)
        {
            using (AppDbContext context = new AppDbContext())
            {

                using (HttpClient client = new HttpClient())
                {
                    try
                    {


                        APIHelper.ConfigureHTTPClient(client);


                        // Send GET request to the URL
                        //var response = await client.GetAsync(getLastPriceURL);

                        HttpResponseMessage response = await client.GetAsync(apiGetReq);
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
                                if (bars.bars.Count == 0)
                                {
                                    Log.Information("No Daily Data found to insert");
                                }
                                else
                                {
                                    int amountInserted = 0;
                                    int amountIgnored = 0;

                                    var sqlValues = string.Join(", ", bars.bars.SelectMany(bar =>
                                        bar.Value.Select(b =>
                                        $"('{bar.Key}', '{b.t:yyyy-MM-dd}', {b.o}, {b.h}, {b.l}, {b.c}, {b.v}, {b.n}, {b.vw})")));

                                    var sql = $@"
                                        INSERT INTO daily_bars (symbol, timestamp, open, high, low, close, volume, trade_count, vw)
                                        VALUES {sqlValues}
                                        ON CONFLICT (symbol, timestamp) DO NOTHING;";

                                    // Execute the raw SQL query
                                    var rowsAffected = await context.Database.ExecuteSqlRawAsync(sql);

                                    // Assuming `rowsAffected` gives the number of successfully inserted rows
                                    amountInserted = rowsAffected;
                                    amountIgnored = bars.bars.Count - amountInserted;
                                    // If any records were inserted, log the results
                                    Log.Information($"Daily data successfully loaded. Number Inserted: {amountInserted} Number of Duplicates Ignored: {amountIgnored}");

                                    // Commit the transaction
                                    transaction.Commit();
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                // In case of error, roll back the transaction
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
        }
    }

}