using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPriceLoader.Helpers
{
    public class MathHelper
    {
        public static double Median(IEnumerable<double?> list)
        {
            //Sort the list first
            List<double> sortedList = list
                .Where(x => x.HasValue)   // remove nulls
                .Select(x => x.Value)     // unwrap nullable
                .OrderBy(x => x)          // sort
                .ToList();



            //If we have even items then there isn't a single median, so we return the average of the two middle items.
            if (list.Count() % 2 == 0)
            {
                int middleIndex = sortedList.Count / 2 - 1;

                return ((sortedList[middleIndex] + sortedList[middleIndex + 1]) / 2);
            }
            else
            {
                
                int middleIndex = sortedList.Count / 2;
                return sortedList[middleIndex];
            }
            
        }

        public static long Sum(IEnumerable<long> sumList)
        {
            return sumList.Select(x => x).Sum();
        }

    }
}
