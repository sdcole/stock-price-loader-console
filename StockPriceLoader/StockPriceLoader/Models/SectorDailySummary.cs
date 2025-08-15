using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPriceLoader.Models
{

        [Table("sector_daily_summaries", Schema = "public")]
        public class SectorDailySummary
        {
            [Key]
            [Column("id")]
            public long Id { get; set; }

            [Column("sector_id")]
            public int SectorId { get; set; }

            [Column("date")]
            public DateTime Date { get; set; }

            [Column("symbol_count")]
            public int SymbolCount { get; set; }

            // Returns
            [Column("avg_return_1d")]
            public double? AvgReturn1D { get; set; }

            [Column("median_return_1d")]
            public double? MedianReturn1D { get; set; }

            [Column("avg_return_5d")]
            public double? AvgReturn5D { get; set; }

            [Column("median_return_5d")]
            public double? MedianReturn5D { get; set; }

            [Column("avg_return_20d")]
            public double? AvgReturn20D { get; set; }

            [Column("median_return_20d")]
            public double? MedianReturn20D { get; set; }

            // Volatility
            [Column("avg_volatility_5d")]
            public double? AvgVolatility5D { get; set; }

            [Column("median_volatility_5d")]
            public double? MedianVolatility5D { get; set; }

            [Column("avg_volatility_10d")]
            public double? AvgVolatility10D { get; set; }

            [Column("median_volatility_10d")]
            public double? MedianVolatility10D { get; set; }

            // Technical Indicators
            [Column("avg_rsi_14")]
            public double? AvgRsi14 { get; set; }

            [Column("median_rsi_14")]
            public double? MedianRsi14 { get; set; }

            [Column("avg_bollinger_bandwidth")]
            public double? AvgBollingerBandwidth { get; set; }

            [Column("median_bollinger_bandwidth")]
            public double? MedianBollingerBandwidth { get; set; }

            [Column("avg_sma_diff")]
            public double? AvgSmaDiff { get; set; }

            [Column("median_sma_diff")]
            public double? MedianSmaDiff { get; set; }

            // Volume Metrics
            [Column("avg_volume")]
            public double? AvgVolume { get; set; }

            [Column("median_volume")]
            public double? MedianVolume { get; set; }

            [Column("avg_volume_ratio")]
            public double? AvgVolumeRatio { get; set; }

            [Column("median_volume_ratio")]
            public double? MedianVolumeRatio { get; set; }

            // Extremes
            [Column("best_symbol")]
            public string? BestSymbol { get; set; }

            [Column("best_return")]
            public double? BestReturn { get; set; }

            [Column("worst_symbol")]
            public string? WorstSymbol { get; set; }

            [Column("worst_return")]
            public double? WorstReturn { get; set; }
        }
 }


