using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions
{
    public static class Maintenance
    {
        [FunctionName("MaintenancePost")]
        public static void MaintenancePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "maintenance")] Models.Maintenance req,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "Maintenance",
                ConnectionStringSetting = "CosmosDBConnection",
                CreateIfNotExists = true)]out Models.Maintenance maintenance,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            maintenance = req;
            maintenance.id = Guid.NewGuid();
            maintenance.UserId = principal.Identity.Name;
            log.LogInformation("Saving new maintenance");
        }

        [FunctionName("MaintenanceGetByVehicle")]
        public static IActionResult MaintenanceGetByVehicle(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "maintenance")] HttpRequest request,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "Maintenance");
            var maintenance = client.CreateDocumentQuery<Models.Maintenance>(collectionUri)
                .Where(p => p.Vehicle == request.Query["vehicleId"].ToString()) //TODO: type vehicle to guid instead of string
                .ToList();
            if (maintenance.Any(x => x.UserId != principal.Identity.Name))
            {
                return new UnauthorizedResult();
            }
            log.LogInformation("Got all maintenance");
            return new OkObjectResult(maintenance);
        }
    }
}