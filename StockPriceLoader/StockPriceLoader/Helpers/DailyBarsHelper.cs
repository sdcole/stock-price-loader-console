using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StockPriceLoader.Models;
using System.Text.Json;

namespace StockPriceLoader.Helpers
{
    public class DailyBarsHelper
    {
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

                                    Log.Debug("Executing SQL Query for Bulk Insert:" + sql);
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
