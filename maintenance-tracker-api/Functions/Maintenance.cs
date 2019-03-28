using System;
using System.Security.Claims;
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
    }
}