using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockPriceLoader.Models;

namespace StockPriceLoader.Helpers
{
    public class SectorDailySummaryHelper
    {

        public static async Task<List<Company>> GetCompaniesBySector(string sector)
        {
            using (AppDbContext context = new AppDbContext())
            {
                return await context.Companies.Where(d => d.Sector.ToUpper() == sector.ToUpper()).ToListAsync();
            } 
        }



    }
}
