using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace maintenance_tracker_api_public.Functions
{
    public class Receipts
    {
        //I would love to use a SqlQuery in this endpoint, but https://github.com/Azure/azure-webjobs-sdk/issues/1726
        [FunctionName("ReceiptAuthorizationGet")]
        public async Task<IActionResult> ReceiptAuthorizationGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request,
            [Blob("receipts", FileAccess.ReadWrite, Connection = "UploadStorage")] CloudBlobContainer container,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            CancellationToken token
        )
        {
            var uri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "VehicleMaintenance");
            var vehicleId = Guid.Parse(request.Query["vehicleId"]);
            var userId = Guid.Parse(request.Query["userId"]);
            var vehiclesAndMaintenance = client.CreateDocumentQuery<VehicleMaintenanceModel>(uri)
                .Where(x => x.UserId == userId && (x.id == vehicleId || x.VehicleId == vehicleId)).ToList();
            if (!vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle).Shared
                || !vehiclesAndMaintenance.Any(vm => vm.Type == VehicleMaintenanceTypes.Maintenance && vm.Receipt == request.Query["name"]))
            {
                return new BadRequestResult();
            }
            var blob = container.GetBlockBlobReference($"{request.Query["userId"]}/{request.Query["name"]}");
            var policy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read
            };
            var sas = blob.GetSharedAccessSignature(policy);
            log.LogInformation($"Authorized access to receipt \"{request.Query["name"]}\" for anonymous user at {request.HttpContext.Connection.RemoteIpAddress}");
            var authorization = new ReceiptAuthorizationDto { Url = $"{blob.Uri}{sas}" };
            return new OkObjectResult(authorization);
        }
    }
}