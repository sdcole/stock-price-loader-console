namespace StockPriceLoader.Models
{
    public class StockMarketStatus
    {
        public DateTime Timestamp { get; set; }
        public bool IsOpen { get; set; }
        public DateTime NextOpen { get; set; }
        public DateTime NextClose { get; set; }
    }
}
