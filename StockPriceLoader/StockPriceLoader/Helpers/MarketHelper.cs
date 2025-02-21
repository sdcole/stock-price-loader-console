using Serilog;
using StockPriceLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StockPriceLoader.Helpers
{
    public class MarketHelper
    {
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
                    APIHelper.ConfigureHTTPClient(client);

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
    }
}
