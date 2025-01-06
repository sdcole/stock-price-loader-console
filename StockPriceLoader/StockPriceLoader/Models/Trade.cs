namespace StockPriceLoader.Models
{
    /*
     *  Trade objects are the deserialized data from the api response
     * 
     * 
     */
    public class Trade
    {
        public List<string> c { get; set; }
        public int i { get; set; }
        public double p { get; set; }
        public int s { get; set; }
        public DateTime t { get; set; }
        public string x { get; set; }
        public string z { get; set; }
        public string? u { get; set; }
    }
}
