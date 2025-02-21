using StockPriceLoader.Helpers;
using StockPriceLoader.Services;

namespace StockPriceLoaderTests
{
    public class EncryptionHelperTests
    {
        [Fact]
        public void EncryptTest()
        {

            string base64EncryptedData = EncryptionHelper.Encrypt("Hello World!");
            Assert.True(base64EncryptedData.Contains("!==ENC==!"));

            
        }

        [Fact]
        public void DecryptTest()
        {

            string base64EncryptedData = EncryptionHelper.Encrypt("Hello World!");
            Assert.True(EncryptionHelper.Decrypt(base64EncryptedData).Contains("Hello World!"));

            
        }

        [Fact]
        public void VerifyTest()
        {

            string base64EncryptedData = EncryptionHelper.Encrypt("Hello World!");
            Assert.True(EncryptionHelper.IsEncrypted(base64EncryptedData));

        }

        [Fact]
        public void UpdateConfigFileTest()
        {

            Thread.Sleep(1000);
             ConfigurationService.Initialize();
             EncryptionHelper.UpdateConfigFile("Hello World!", "TEST");
             //Give it a second to release the file
             Thread.Sleep(1000);
             string configFilePath = AppContext.BaseDirectory + "appsettings.json";
            
             // Read existing JSON content
             string jsonContent = File.ReadAllText(configFilePath);
             Assert.True(jsonContent.Contains("Hello World!"));


        }
    }
}