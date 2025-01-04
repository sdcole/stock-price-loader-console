using Alpaca.Markets;
using Npgsql;
using RestSharp;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using StockPriceLoader.Models;
using System.Text.Json;

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



        public static async Task Main(string[] args)
        {

            using (AppDbContext context = new AppDbContext(config))
            {
                var companies = context.Companies.ToList();
                //AAPL%2CTSLA%2CFIN%2CAMD%2CINTC&feed=iex&currency=USD
                string getLastPriceURL = @"https://data.alpaca.markets/v2/stocks/trades/latest?symbols=";


                foreach (Company company in companies)
                {
                    Console.WriteLine($"ID: {company.Id}, Ticker: {company.Ticker}, Description: {company.CompanyDescription}, Sector: {company.Sector}");
                    getLastPriceURL += company.Ticker + ",";
                }

                getLastPriceURL = getLastPriceURL.Substring(0, getLastPriceURL.Length - 1);
                getLastPriceURL += @"&feed=iex&currency=USD";

                using (var client = new HttpClient())
                {
                    try
                    {

                        
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", API_KEY);
                        client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", API_SECRET);


                        // Send GET request to the URL
                        //var response = await client.GetAsync(getLastPriceURL);
                        var response = await client.GetAsync(getLastPriceURL);
                        string resp = await response.Content.ReadAsStringAsync();
                        // Ensure the request was successful
                        //response.EnsureSuccessStatusCode();

                        // Read the response content as a string
                        string content = await response.Content.ReadAsStringAsync();

                        var trades = JsonSerializer.Deserialize<Trades>(content);


                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                // Output some of the data
                                foreach (var trade in trades.trades)
                                {
                                    Console.WriteLine(trade);
                                    context.StockPrice.Add(new StockPrice(trade.Key, trade.Value));
                                    
                                }
                                context.SaveChanges();
                                transaction.Commit();

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                transaction.Rollback();
                            }

                        }
                                
                        // Print the content to the console
                        Console.WriteLine(content);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Request failed: {ex.Message}");
                    }
                }
            }



        }
        
    }
}