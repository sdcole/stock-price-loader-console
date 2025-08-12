using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPriceLoader.Models
{
    public class Sector
    {
        public int Id { get; set; }
        public string SectorName { get; set; }
        public string SectorType { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
