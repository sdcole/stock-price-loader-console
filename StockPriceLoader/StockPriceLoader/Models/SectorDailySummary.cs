using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPriceLoader.Models
{
    public class SectorDailySummary
    {
        public long Id { get; set; }
        [Required]
        public DateTime Date { get; set; }
        // Sector & Market Context
        [MaxLength(100)]
        [Required]
        public string? Sector { get; set; }
        public double? SectorAvgReturn5d { get; set; }
        public double? SectorVolatility5d { get; set; }
    }
}
