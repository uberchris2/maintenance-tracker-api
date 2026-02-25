using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions
{
    public class CarQueryProxy
    {
        private const string CarQueryBaseUrl = "https://www.carqueryapi.com/api/0.3/";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CarQueryProxy> _logger;

        public CarQueryProxy(IHttpClientFactory httpClientFactory, ILogger<CarQueryProxy> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [Function("ProxyYears")]
        public async Task<IActionResult> ProxyYears(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "yearmakemodel/year")] HttpRequest request)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{CarQueryBaseUrl}?cmd=getYears",
                request.HttpContext.RequestAborted);
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Proxied years request to carqueryapi.com");
            return new ContentResult { Content = content, ContentType = "application/json", StatusCode = (int)response.StatusCode };
        }

        [Function("ProxyMakes")]
        public async Task<IActionResult> ProxyMakes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "yearmakemodel/make")] HttpRequest request)
        {
            var year = request.Query["year"].ToString();
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{CarQueryBaseUrl}?cmd=getMakes&year={year}",
                request.HttpContext.RequestAborted);
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Proxied makes request to carqueryapi.com");
            return new ContentResult { Content = content, ContentType = "application/json", StatusCode = (int)response.StatusCode };
        }

        [Function("ProxyModels")]
        public async Task<IActionResult> ProxyModels(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "yearmakemodel/model")] HttpRequest request)
        {
            var make = request.Query["make"].ToString();
            var year = request.Query["year"].ToString();
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{CarQueryBaseUrl}?cmd=getModels&make={make}&year={year}",
                request.HttpContext.RequestAborted);
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Proxied models request to carqueryapi.com");
            return new ContentResult { Content = content, ContentType = "application/json", StatusCode = (int)response.StatusCode };
        }
    }
}
