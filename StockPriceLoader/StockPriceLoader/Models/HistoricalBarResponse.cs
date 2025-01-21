using StockPriceLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPriceLoader.Models
{
    public class HistoricalBarResponse
    {
        public Dictionary<string, List<Bar>> bars { get; set; }
    }

}
