using Alpaca.Markets;
using Npgsql;
using RestSharp;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace CodeExamples
{
    internal static class SimpleAPICall
    {

        private static IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("config.json", optional: false);

        private static IConfigurationRoot config = builder.Build();
        //Alpaca API Keys
        private static string API_KEY = config["API_KEY"];

        private static string API_SECRET = config["API_SECRET"];

        /**
         * This function is the main in the console app to run the DB
         * 
         * TODO: REMOVE HARDCODED VALUES
         * TODO: MAKE IT SO IT SO IT RUNS IN BACKGROUND (NO CMD POPUP)
         * TODO: CREATE AN OBJECT FOR A RESPONSE (I DIDN'T DO THIS BC IT TAKES LONGER BUT IT IS THE CORRECT WAY).
         * 
         */
        public static async Task Main(string[] args)
        {

            
            //Database connection
            #region CONN
            NpgsqlConnection conn = new NpgsqlConnection(config["CONN_STRING"]);
            #endregion
            conn.Open();
            NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM STOCKS_TO_CHECK", conn);

            using NpgsqlDataReader rdr = cmd.ExecuteReader();
            //Create a list of tasks (api calls) to execute.
            List<Task<bool>> tasks = new List<Task<bool>>();

            //Read through all of the stock tickers found in the database.
            while (rdr.Read())
            {
                //Add task to list
                tasks.Add(CallAPIAndInsertToDB(rdr.GetString(0)));
            }
            //Done using DB release resources
            conn.Close();
            //Kick off all tasks (Since the API is IO bound we run multiple to make the program more efficient)
            await Task.WhenAll(tasks);

            

        }
        /**
         * This function calls the API and saves the response in the DB
         * Parameter passed is the ticker symbol (4 letter char for a stock).
         * Returns Task information and a bool on completion status.
         */
        public static async Task<bool> CallAPIAndInsertToDB(string ticker)
        {
            try
            {
                //API Information
                var options = new RestClientOptions("https://data.alpaca.markets/v2/stocks/" + ticker + "/trades/latest?feed=iex");
                var client = new RestClient(options);
                var request = new RestRequest("");
                request.AddHeader("accept", "application/json");
                request.AddHeader("APCA-API-KEY-ID", API_KEY);
                request.AddHeader("APCA-API-SECRET-KEY", API_SECRET);
                //Call API
                var response = await client.GetAsync(request);


                //Parse API response into something we can use (Deserialize).
                JObject jsonObj = JObject.Parse(response.Content);


                //Assign the variables found from the api
                string price = (string)jsonObj.SelectToken("trade.p");
                DateTime time = (DateTime)jsonObj.SelectToken("trade.t");

                //Connect to DB
                #region CLOSE
                NpgsqlConnection conn = new NpgsqlConnection(config["CONN_STRING"]);
                #endregion
                conn.Open();
                //Insert query for the table
                NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO RECENT_STOCK_DATA (TICKER_SYMBOL, PRICE, DATE_TIME) VALUES (@ticker,@price,@time)", conn);
                //Parameters to prevent SQL injection
                var tick = cmd.Parameters.AddWithValue("@ticker", ticker);
                var prc = cmd.Parameters.AddWithValue("@price", Double.Parse(price));
                var tm = cmd.Parameters.AddWithValue("@time", time);

                //Run insert query
                cmd.ExecuteNonQuery();

                //Close DB connection
                conn.Close();
                //Return true to tell that it successfully ran
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;

        }
    }
}