using System;
using RestSharp;
using RestSharp.Deserializers;
using System.Collections.Generic;

namespace Crypto_Portfolio
{
    public class RestAPI
    {
        public Uri baseURL = new Uri("https://api.coinmarketcap.com/");
        public RestClient client;
        public RestRequest request;
        public List<CoinMarketCap> deserializedResponse;
        public List<decimal> priceList = new List<decimal>();
        public int progress = 0;

        public RestAPI()
        {

        }

        public List<decimal> requestPrice(List<string> coinName)
        {

            foreach (string coin in coinName)
            {
                // Build client and request with RestSharp
                client = new RestClient(baseURL);
                request = new RestRequest("v1/ticker/" + coin, Method.GET);
                request.AddHeader("Accept", "application/json");

                // Deserialize the JSON response
                IRestResponse response = client.Execute(request);
                JsonDeserializer deserializeNeeded = new JsonDeserializer();
                deserializedResponse = deserializeNeeded.Deserialize<List<CoinMarketCap>>(response);

                // Return the value
                decimal price = deserializedResponse[0].price_usd;
                priceList.Add(price);
                progress++;
            }
            return priceList;
        }

        public int getProgresReport()
        {
            return progress;
        }
    }
}
