using CustoDL.Models;
using System;
using System.Linq;
using Newtonsoft.Json;
using StackExchange.Redis;
using RedisConfig;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace CustoDL.Business
{
    public class Subscriber
    {
        private static readonly ConnectionMultiplexer redisCache = RedisConnectorHelper.Connection;
        private IDatabase db = redisCache.GetDatabase();
        private ISubscriber sub = redisCache.GetSubscriber();
        private string _baseUrl = ConfigurationManager.AppSettings["ReverseGeoCode"];

        public async Task<bool> AddSubscriber(string subscriberId, CoOrdinates coordinates)
        {
            try
            {
                dynamic googleResults = new Uri(string.Format(_baseUrl, coordinates.Latitude, coordinates.Longitude)).GetDynamicGeoCodeJson();
                var geoCodeResponse = JsonConvert.DeserializeObject<GoogleGeoCodeResponse>(googleResults);
                RedisKey key = ((GoogleGeoCodeResponse)geoCodeResponse).results[0].address_components[4].long_name;

                bool val = await db.GeoAddAsync(key, coordinates.Longitude, coordinates.Latitude, subscriberId, CommandFlags.None);
                return val;
            }
            catch(Exception ex)
            {
                var str = ex.Message;
                return false;
            }
        }

        public void RemoveSubscriber(string subscriberId, CoOrdinates coordinates)
        {
            dynamic googleResults = new Uri(string.Format(_baseUrl, coordinates.Latitude, coordinates.Longitude)).GetDynamicGeoCodeJson();
            var geoCodeResponse = JsonConvert.DeserializeObject<GoogleGeoCodeResponse>(googleResults);
            RedisKey key = ((CustoDL.Models.GoogleGeoCodeResponse)geoCodeResponse).results[0].address_components[4].long_name;
            db.GeoRemove(geoCodeResponse.results[0].address_components[3].long_name, subscriberId, CommandFlags.None);
        }

        public async Task FindSubscriber(string publisherId, CoOrdinates coordinates)
        {
            dynamic googleResults = new Uri(string.Format(_baseUrl, coordinates.Latitude, coordinates.Longitude)).GetDynamicGeoCodeJson();
            var geoCodeResponse = JsonConvert.DeserializeObject<GoogleGeoCodeResponse>(googleResults);
            RedisKey key = ((CustoDL.Models.GoogleGeoCodeResponse)geoCodeResponse).results[0].address_components[4].long_name;
            //To fetch closest "10" delivery boy to the hotel with a range of radius 5.0KM
            var result = db.GeoRadius(key, coordinates.Longitude, coordinates.Latitude, 5.0, GeoUnit.Kilometers, 10, Order.Ascending,GeoRadiusOptions.Default, CommandFlags.None);
            await SendNotification(result, coordinates, publisherId);
        }

        private async Task SendNotification(GeoRadiusResult[] activeSubscribers, CoOrdinates coordinates, string publisherId)
        {
            //TO-DO: Send notification to the filtered subscribers, consider the subscribers with response.

            //Pick the earlier available user from the responded users.
            var originAddresses = activeSubscribers.Select(x => x.Position.Value.Latitude.ToString()+","+ x.Position.Value.Longitude.ToString()).ToArray();
            var origin = string.Join("|", originAddresses);
            using (var client = new HttpClient())
            {
                var uri = new Uri(string.Format(ConfigurationManager.AppSettings["DirectionMatrixApi"], origin, coordinates.Latitude.ToString(), coordinates.Longitude.ToString()));
               // var uri = new Uri($"https://maps.googleapis.com/maps/api/distancematrix/json?units=metric&origins={origin}&destinations={coordinates.Latitude.ToString()},{coordinates.Longitude.ToString()}&key=AIzaSyC0FU6C2KERjo4LU6Wfq67PKJWNJFd3iEo");

                HttpResponseMessage response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("GoogleDistanceMatrixApi failed with status code: " + response.StatusCode);
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var res = JsonConvert.DeserializeObject<dynamic>(content);
                    var temp = res[2].Value;
                    foreach(var re in res[2].Value)
                    {
                        // Select the Delivery Boy with least turn around time
                    }
                }
            }

            //Assign the order to the delivery boy
            AssignSubscriber(publisherId, activeSubscribers.First().Member, new CoOrdinates { Latitude = 12.9222382, Longitude = 80.12740099999996 }).GetAwaiter(); // Rewrite to get Delivery coordinates
        }

        public async Task AssignSubscriber(string publisherId, string subscriberId, CoOrdinates assignedCoordinates)
        {
            var checkForExistingSubscriber = db.GeoRadius(publisherId, assignedCoordinates.Longitude, assignedCoordinates.Latitude, 1.0, GeoUnit.Kilometers, 1, Order.Ascending, GeoRadiusOptions.Default, CommandFlags.None);
            if(checkForExistingSubscriber.Count() == 1)
            {
                // Logic to club with the existing order
            }
            bool addedSuccessfully = await db.GeoAddAsync(publisherId, assignedCoordinates.Longitude, assignedCoordinates.Latitude, subscriberId, CommandFlags.None);
        }
    }
}