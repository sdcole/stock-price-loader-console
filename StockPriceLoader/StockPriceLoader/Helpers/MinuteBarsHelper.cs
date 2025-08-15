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
    public class MinuteBarsHelper
    {
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


                                var sqlValues = string.Join(", ", bars.bars.Select(bar =>
                                    $"('{bar.Key}', '{bar.Value.t:yyyy-MM-dd HH:mm:ss}', {bar.Value.o}, {bar.Value.h}, {bar.Value.l}, {bar.Value.c}, {bar.Value.v}, {bar.Value.n}, {bar.Value.vw})"));

                                // Build the complete SQL query for the bulk insert
                                var sql = $@"
                                    INSERT INTO minute_bars (symbol, timestamp, open, high, low, close, volume, trade_count, vw)
                                    VALUES {sqlValues}
                                    ON CONFLICT (symbol, timestamp) DO NOTHING;";  // Handle conflict by doing nothing for duplicates

                                Log.Debug("Executing SQL Query for Bulk Insert:" + sql);
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
    }
}
