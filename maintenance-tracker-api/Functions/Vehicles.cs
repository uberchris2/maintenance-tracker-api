using System;
using maintenance_tracker_api.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions
{
    public static class Vehicles
    {
        [FunctionName("Vehicles")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "vehicles")] Vehicle req,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "Vehicles",
                ConnectionStringSetting = "CosmosDBConnection",
                CreateIfNotExists = true)]out Vehicle vehicle,
            ILogger log
        )
        {

            vehicle = req;
            vehicle.id = Guid.NewGuid();

            log.LogInformation("Saving new vehicle");

            //            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //            dynamic data = JsonConvert.DeserializeObject(requestBody);
            //            name = name ?? data?.name;
            //
            //            return name != null
            //                ? (ActionResult)new OkObjectResult($"Hello, {name}")
            //                : new BadRequestObjectResult("Who are you?");
        }
    }
}