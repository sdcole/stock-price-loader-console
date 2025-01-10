namespace StockPriceLoader.Models
{
    public class StockMarketStatus
    {
        public DateTime timestamp { get; set; }
        public bool is_open { get; set; }
        public DateTime next_open { get; set; }
        public DateTime next_close { get; set; }
    }
}
