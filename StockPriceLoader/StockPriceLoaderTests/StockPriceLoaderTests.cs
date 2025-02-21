using StockPriceLoader;
using StockPriceLoader.Services;
using StockPriceLoader.Helpers;
namespace StockPriceLoaderTests
{
    public class StockPriceLoaderTests
    {
        [Fact]
        public async void CheckMarketStatusTest()
        {
            ConfigurationService.Initialize();
            EncryptionHelper.EncryptSensitiveConfigData(new[] { "ConnectionStrings:LoggingConnection", "ConnectionStrings:AppConnection", "API_KEY", "API_SECRET" });
            var resp = await MarketHelper.MarketIsOpen();
            if (resp.is_open != null)
            {
                Assert.True(true);
            }
            else
            {
                Assert.True(false);
            }



        }
    }
}
