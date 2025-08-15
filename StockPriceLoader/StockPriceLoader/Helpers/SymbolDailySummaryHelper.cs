using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;
using StockPriceLoader.Models;

namespace StockPriceLoader.Helpers
{
    public class SymbolDailySummaryHelper
    {


        /**
         *  CalculateAndLoadDailySummary
         *  
         *  This will calculate the day's summary data and load it into the db.
         * 
         * 
         * 
         **/
        public static async Task CalculateAndLoadSymbolDailySummary()
        {

            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
            {
                Log.Information("Market is closed for the weekend. Skipping daily summary load.");
                return;
            }

            Log.Information("Calculating and Loading Daily Summary Data");
            using (AppDbContext context = new AppDbContext())
            {
                try
                {
                    List<Company> companies = context.Companies.ToList();
                    List<SymbolDailySummary> dailySummaries = new List<SymbolDailySummary>();
                    //This will loop through all companies in the companies table. It appends the data to the get request so the response will contain those tickers.
                    foreach (Company company in companies)
                    {
                        Log.Debug("Calculating Daily Summary for " + company.Symbol);
                        SymbolDailySummary summary = await SymbolDailySummaryHelper.CalculateDailySummaryForSymbol(company.Symbol);

                        if (summary != null)
                        {
                            dailySummaries.Add(summary);
                        }
                        else
                        {
                            continue;
                        }


                    }
                    int insertedSummaries = await SymbolDailySummaryHelper.InsertDailySummaries(dailySummaries);
                    Log.Information($"Inserted {insertedSummaries} of Daily Summaries.");



                }
                catch (Exception ex)
                {
                    Log.Error(ex, "There was an issue getting Company list from database");
                }
            }
        }


        /**
         * CalculateDailySummaryForSymbol
         * This function calculates the daily summary for a given symbol.
         * 
         * Returns an instance of DailySummary with the calculated values or NULL if there is not enough data to calculate the summary.
         *
         **/
        public static async Task<SymbolDailySummary> CalculateDailySummaryForSymbol(string symbol)
        {
            using (AppDbContext context = new AppDbContext())
            {
                try
                {
                    //Get the last 21 daily bars for calculations. 20 Days is a month in trading terms.
                    var last21 = await context.DailyBars
                        .Where(d => d.Symbol == symbol)
                        .OrderByDescending(d => d.Timestamp)
                        .Take(21)
                        .ToListAsync();

                    if (last21.Count != 21)
                    {
                        Log.Warning("Not enough previous daily information was available for symbol: " + symbol 
                            + " to calculate summary information for: " + DateTime.UtcNow.Date);
                        return null;
                    }
                    SymbolDailySummary dailySummary = new SymbolDailySummary
                    {
                        Symbol = symbol,
                        Date = last21.First().Timestamp.Date // Set to today's date
                    };

                    //Calculate returns
                    dailySummary.Return1d = (last21[0].Close - last21[1].Close) / last21[1].Close;
                    dailySummary.Return5d = (last21[0].Close - last21[4].Close) / last21[4].Close;


                    //Calculate Volatiily
                    List<double> returnsList = GetReturnList(last21.Take(6).ToList(), 5);
                    if (returnsList == null)
                    {
                        Log.Information("Unable to process data! Could not retreive returns list for the past 5 days for: " + symbol);
                        return null;
                    }
                    dailySummary.Volatility5d = StandardDeviation(returnsList);

                    returnsList = GetReturnList(last21.Take(11).ToList(), 10);
                    if (returnsList == null)
                    {
                        Log.Information("Unable to process data! Could not retreive returns list for the past 10 days for: " + symbol);
                        return null;
                    }
                    dailySummary.Volatility10d = StandardDeviation(returnsList);



                    //Calculate SMA
                    dailySummary.Sma5 = last21.Take(5).Average(d => d.Close);
                    dailySummary.Sma10 = last21.Take(10).Average(d => d.Close);


                    //Calculate RSI
                    List<DailyBarData> ascLast15 = last21.Take(15).ToList();
                    dailySummary.Rsi14 = CalculateRsi(ascLast15.Select(d => d.Close).ToList(), 14);

                    //Calculate Bollinger Bands
                    List<DailyBarData> ascLast20 = last21.Take(20).ToList();
                    dailySummary.BollingerBandwidth = CalculateBollingerBandwidth(ascLast20.Select(d=>d.Close).ToList(), 20);

                    //Calculate Volume
                    dailySummary.VolumeAvg5d = last21.Take(5).Average(d => d.Volume);
                    dailySummary.VolumeRatio = last21[0].Volume / dailySummary.VolumeAvg5d;

                    //List<double> 5dReturns = Get5DayReturnList(last20);
                    return dailySummary;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error calculating daily summary for symbol: " + symbol);
                    Console.WriteLine($"Error fetching daily bar data for symbol {symbol}: {ex.Message}");
                    return null;
                }
            }
                

            // Fetch daily bar data for the symbol


        }

        public static double StandardDeviation(IEnumerable<double> values)
        {
            var enumerable = values.ToList();
            var avg = enumerable.Average();
            var sum = enumerable.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sum / (enumerable.Count - 1));
        }

        public static double CalculateRsi(List<double> closes, int period = 14)
        {
            if (closes == null || closes.Count < period + 1)
            {
                Log.Error("Not enough closing prices to calculate RSI.");
                throw new ArgumentException($"At least {period + 1} closing prices required");
            }
            closes.Reverse(); // Ensure the dataset is in ascending order (oldest to newest)
            // Calculate daily changes
            List<double> changes = new List<double>();
            for (int i = 1; i < closes.Count; i++)
            {
                changes.Add(closes[i] - closes[i - 1]);
            }

            // Separate gains and losses
            List<double> gains = changes.Select(c => c > 0 ? c : 0).ToList();
            List<double> losses = changes.Select(c => c < 0 ? Math.Abs(c) : 0).ToList();

            // Calculate initial average gain and loss (simple average)
            double avgGain = gains.Take(period).Average();
            double avgLoss = losses.Take(period).Average();

            // Use Wilder’s smoothing method for the rest of the period, if more data is available
            for (int i = period; i < gains.Count; i++)
            {
                avgGain = ((avgGain * (period - 1)) + gains[i]) / period;
                avgLoss = ((avgLoss * (period - 1)) + losses[i]) / period;
            }

            if (avgLoss == 0)
                return 100; // RSI is 100 if no losses

            double rs = avgGain / avgLoss;
            double rsi = 100 - (100 / (1 + rs));
            return rsi;
        }

        public static double CalculateBollingerBandwidth(List<double> closes, int period = 20)
        {
            if (closes == null || closes.Count < period)
            {
                Log.Error("Not enough closing prices to calculate Bollinger Bands.");
                throw new ArgumentException($"At least {period} closing prices required");
            }
            closes.Reverse(); // Ensure the dataset is in ascending order (oldest to newest)
            var recentCloses = closes.TakeLast(period).ToList();

            double sma = recentCloses.Average();
            double stdDev = StandardDeviation(recentCloses);  // Use your Std Dev extension method

            double upperBand = sma + 2 * stdDev;
            double lowerBand = sma - 2 * stdDev;

            double bandwidth = (upperBand - lowerBand) / sma;
            return bandwidth;
        }

        /**
         * GetReturnList
         * This function provides a list of percentage returns for the provided list of DailyBarData.
         * We need one extra day of data to calculate the return for the first day.
         * It is assumed that the barData is ordered from most recent to oldest.
         * 
         */
        public static List<double> GetReturnList(List<DailyBarData> barData, int returnDuration) 
        {
            try
            {
                if (barData == null || barData.Count != returnDuration + 1)
                {
                    Log.Error("Invalid bar data provided for return calculation.");
                    return null;
                }
                List<double> returnsList = new List<double>();

                for (int i = 0; i < returnDuration; i++)
                {
                    double returnValue = (barData[i].Close - barData[i + 1].Close) / barData[i + 1].Close;
                    returnsList.Add(returnValue);
                }
                return returnsList;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error calculating returns for daily bar data.");
                return null;
            }
        }

        public static async Task<int> InsertDailySummaries(List<SymbolDailySummary> summaries)
        {
            using (AppDbContext context = new AppDbContext())
            {

                using (IDbContextTransaction transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        var sql = new StringBuilder();
                        var parameters = new List<object>();

                        sql.Append("INSERT INTO symbol_daily_summaries (symbol, date, return_1d, return_5d, volatility_5d, volatility_10d, sma_5, sma_10, rsi_14, bollinger_bandwidth, volume_avg_5d, volume_ratio) VALUES ");

                        for (int i = 0; i < summaries.Count; i++)
                        {
                            sql.Append($"(@p{i}_Symbol, @p{i}_Date, @p{i}_Return1d, @p{i}_Return5d, @p{i}_Volatility5d, @p{i}_Volatility10d, @p{i}_Sma5, @p{i}_Sma10, @p{i}_Rsi14, @p{i}_BollingerBandwidth, @p{i}_VolumeAvg5d, @p{i}_VolumeRatio),");

                            var s = summaries[i];
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Symbol", s.Symbol));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Date", s.Date));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Return1d", (object?)s.Return1d ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Return5d", (object?)s.Return5d ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Volatility5d", (object?)s.Volatility5d ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Volatility10d", (object?)s.Volatility10d ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Sma5", (object?)s.Sma5 ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Sma10", (object?)s.Sma10 ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_Rsi14", (object?)s.Rsi14 ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_BollingerBandwidth", (object?)s.BollingerBandwidth ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_VolumeAvg5d", (object?)s.VolumeAvg5d ?? DBNull.Value));
                            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_VolumeRatio", (object?)s.VolumeRatio ?? DBNull.Value));

                        }

                        sql.Length--; // Remove last comma
                        sql.Append(" ON CONFLICT (symbol, date) DO NOTHING;");



                        Log.Debug("Executing SQL Query for Bulk Insert:" + sql);
                        // Execute the raw SQL query
                        int rowsAffected = await context.Database.ExecuteSqlRawAsync(sql.ToString(), parameters.ToArray()); ;
                        transaction.Commit();
                        return rowsAffected;
                    }
                    catch (Exception ex)
                    {
                        // In case of error, roll back the transaction
                        Log.Error("Failed to insert records into table, Rolling back...", ex);
                        transaction.Rollback();
                        return 0;
                    }
                }
            }
        }
    }
}