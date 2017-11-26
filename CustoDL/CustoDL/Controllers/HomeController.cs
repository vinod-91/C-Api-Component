using CustoDL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CustoDL.Business;
using System.Threading.Tasks;

namespace CustoDL.Controllers
{
    public class HomeController : ApiController
    {
        private readonly Subscriber _subscriber;
        private readonly Pulisher _publisher;

        public HomeController()
        {
            _subscriber = new Subscriber();
            _publisher = new Pulisher();
        }

        [HttpGet]
        public async Task Subscriber(double latitude, double longitude, long subscriberId)
        {
            if (ValidateCoOrdinates(latitude, longitude))
            {
               await _subscriber.AddSubscriber(subscriberId.ToString(), new CoOrdinates { Latitude = latitude, Longitude = longitude });
            }
        }

        [HttpGet]
        public async Task Publisher(double latitude, double longitude, long publisherId)
        {
            if (ValidateCoOrdinates(latitude, longitude))
            {
                await _subscriber.FindSubscriber(publisherId.ToString(), new CoOrdinates { Latitude = latitude, Longitude = longitude });
            }
        }

        [HttpGet]
        public void Test(int id)
        {
            var str = id.ToString();
        }

        private bool ValidateCoOrdinates(double latitude, double longitude)
        {
            return latitude <= 90 && latitude >= -90 && longitude <= 180 && longitude >= -180;
        }
    }
}
