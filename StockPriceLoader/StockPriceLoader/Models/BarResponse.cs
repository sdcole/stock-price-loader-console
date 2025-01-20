using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPriceLoader.Models
{
    public class BarResponse
    {
        public Dictionary<string, Bar> bars { get; set; }
    }

}
