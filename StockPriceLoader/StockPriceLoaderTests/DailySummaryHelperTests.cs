using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockPriceLoader.Helpers;

namespace StockPriceLoaderTests
{
    public class DailySummaryHelperTests
    {

        [Fact]
        public void StandardDeviationTest()
        {
            List<double> values = new List<double> { 10.0, 12.0, 23.0, 23.0, 16.0, 23.0, 21.0, 16.0 };
            Assert.Equal(5.2372293656638167, SymbolDailySummaryHelper.StandardDeviation(values));
        }
    }
}
