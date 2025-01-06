namespace StockPriceLoader.Models
{
    public class StockPrice
    {
        public int Id { get; set; }
        public string Ticker { get; set; }
        public double TradePrice { get; set; }
        public DateTime Timestamp { get; set; }
        public int TradeSize { get; set; }
        public string Exchange { get; set; }
        public char Tape { get; set; }
        public List<string> Conditions { get; set; }  // Array of condition flags
        public string? UpdateStatus { get; set; }  // Canceled or corrected

        //This constructor takes in a trade object (comes from the api) 

        public StockPrice() { }
        public StockPrice(string ticker, Trade trade)
        {
            Ticker = ticker;
            TradePrice = trade.p;
            Timestamp = trade.t;
            TradeSize = trade.s;
            Exchange = trade.x;
            Tape = trade.z.Length > 0 ? trade.z[0] : ' '; // Assuming Tape is the first character of the 'z' value
            Conditions = trade.c.ToList(); // Assuming 'c' is a list of conditions, we convert it to List<string>
            UpdateStatus = trade.u; // Defaulting to "Corrected", can be set based on your logic
        }
    }


    
}
