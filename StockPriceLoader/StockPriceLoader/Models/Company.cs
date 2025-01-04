using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPriceLoader.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Ticker { get; set; }
        public string CompanyDescription { get; set; }
        public string Sector { get; set; }
    }
}
