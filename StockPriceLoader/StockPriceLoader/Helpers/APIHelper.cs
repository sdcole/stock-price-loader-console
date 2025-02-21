using Microsoft.Extensions.Configuration;
using StockPriceLoader.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPriceLoader.Helpers
{
    public class APIHelper
    {
        /**
         * This function adds the required request headers to the HTTP Client to allow for api calls.
         * 
         */
        public static void ConfigureHTTPClient(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", EncryptionHelper.Decrypt(ConfigurationService.Configuration["API_KEY"]));
            client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", EncryptionHelper.Decrypt(ConfigurationService.Configuration["API_SECRET"]));
        }
    }
}
