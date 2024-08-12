using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WorldClockApp.Models;

namespace WorldClockApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _GoogleAPIKey;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
            _GoogleAPIKey = _configuration["GoogleAPIKey"];
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Clock()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Weather()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Currency()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Clock(string location, string location1, string location2)
        {
            if (!string.IsNullOrEmpty(location))
            {
                var coordinates = await GetCoordinates(location);
                if (coordinates.HasValue)
                {
                    var time = await GetTimeZoneTime(coordinates.Value.Item1, coordinates.Value.Item2);
                    ViewBag.Location = CapitalizeFirstLetter(location);
                    ViewBag.Country = await GetCountryName(coordinates.Value.Item1, coordinates.Value.Item2);
                    ViewBag.Time = time;
                }
                else
                {
                    ViewBag.Error = "Location not found.";
                }
            }

            if (!string.IsNullOrEmpty(location1) && !string.IsNullOrEmpty(location2))
            {
                var coordinates1 = await GetCoordinates(location1);
                var coordinates2 = await GetCoordinates(location2);

                if (coordinates1.HasValue && coordinates2.HasValue)
                {
                    var time1 = await GetTimeZoneTime(coordinates1.Value.Item1, coordinates1.Value.Item2);
                    var time2 = await GetTimeZoneTime(coordinates2.Value.Item1, coordinates2.Value.Item2);

                    DateTime dateTime1 = DateTime.ParseExact(time1, "yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture);
                    DateTime dateTime2 = DateTime.ParseExact(time2, "yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture);

                    var timeDifference = CalculateTimeDifference(dateTime1, dateTime2);

                    ViewBag.Location1 = CapitalizeFirstLetter(location1);
                    ViewBag.Country1 = await GetCountryName(coordinates1.Value.Item1, coordinates1.Value.Item2);
                    ViewBag.Time1 = time1;

                    ViewBag.Location2 = CapitalizeFirstLetter(location2);
                    ViewBag.Country2 = await GetCountryName(coordinates2.Value.Item1, coordinates2.Value.Item2);
                    ViewBag.Time2 = time2;

                    ViewBag.TimeDifference = timeDifference;
                }
                else
                {
                    ViewBag.ErrorComparison = "One or both locations not found for comparison.";
                }
            }

            return View();
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
        }
        private string CalculateTimeDifference(DateTime time1, DateTime time2)
        {
            try
            {
                TimeSpan difference = time2 - time1;
                int hours = Math.Abs(difference.Hours);
                string direction = difference.TotalMinutes > 0 ? "ahead" : "behind";

                return $"{hours} hours {direction}";
            }
            catch
            {
                return "Error calculating time difference.";
            }
        }

        private async Task<(double, double)?> GetCoordinates(string location)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string geocodingUrl = $"https://maps.googleapis.com/maps/api/geocode/json?address={location}&key={_GoogleAPIKey}";
                    HttpResponseMessage response = await client.GetAsync(geocodingUrl);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    if (result.GetProperty("status").GetString() == "OK")
                    {
                        var locationData = result.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");
                        double lat = locationData.GetProperty("lat").GetDouble();
                        double lng = locationData.GetProperty("lng").GetDouble();
                        return (lat, lng);
                    }
                }
                catch
                {
                    // Handle errors
                }
            }
            return null;
        }

        private async Task<string> GetCountryName(double latitude, double longitude)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string geocodingUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={_GoogleAPIKey}";
                    HttpResponseMessage response = await client.GetAsync(geocodingUrl);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    if (result.GetProperty("status").GetString() == "OK")
                    {
                        var addressComponents = result.GetProperty("results")[0].GetProperty("address_components");
                        foreach (var component in addressComponents.EnumerateArray())
                        {
                            var types = component.GetProperty("types").EnumerateArray();
                            if (types.Any(t => t.GetString() == "country"))
                            {
                                return component.GetProperty("long_name").GetString();
                            }
                        }
                    }
                }
                catch
                {
                    // Handle errors
                }
            }
            return "Unknown";
        }

        private async Task<string> GetTimeZoneTime(double latitude, double longitude)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string timestamp = ((int)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds).ToString();
                    string timeZoneUrl = $"https://maps.googleapis.com/maps/api/timezone/json?location={latitude},{longitude}&timestamp={timestamp}&key={_GoogleAPIKey}";
                    HttpResponseMessage response = await client.GetAsync(timeZoneUrl);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    if (result.GetProperty("status").GetString() == "OK")
                    {
                        double offset = result.GetProperty("rawOffset").GetDouble() + result.GetProperty("dstOffset").GetDouble();
                        var localDateTime = System.DateTime.UtcNow.AddSeconds(offset);
                        return localDateTime.ToString("yyyy-MM-dd hh:mm:ss tt"); // 12-hour format with AM/PM
                    }
                }
                catch
                {
                    // Handle errors
                }
            }
            return "Could not retrieve time for this location.";
        }
    }
}
