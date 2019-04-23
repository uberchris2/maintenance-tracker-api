using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using maintenance_tracker_api.Models;
using maintenance_tracker_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace maintenance_tracker_api.Functions
{
    public class Receipts
    {
        private readonly IB2cHelper _b2cHelper;

        public Receipts(IB2cHelper b2cHelper)
        {
            _b2cHelper = b2cHelper;
        }

        [FunctionName("ReceiptAuthorizationGet")]
        public IActionResult ReceiptAuthorizationGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request,
            [Blob("receipts", FileAccess.ReadWrite, Connection = "UploadStorage")] CloudBlobContainer container,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var blob = container.GetBlockBlobReference($"{_b2cHelper.GetOid(principal)}/{request.Query["name"]}");
            var policy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            };
            var sas = blob.GetSharedAccessSignature(policy);
            log.LogInformation($"Authorized access to receipt \"{request.Query["name"]}\" for user {_b2cHelper.GetOid(principal)}");
            var authorization = new ReceiptAuthorizationDto { Url = $"{blob.Uri}{sas}" };
            return new OkObjectResult(authorization);
        }

        [FunctionName("ReceiptsGet")]
        public IActionResult ReceiptsGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "receipts")] HttpRequest request,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var uri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "VehicleMaintenance");
            var receipts = client.CreateDocumentQuery<MaintenanceModel>(uri) //TODO async
                .Where(x => x.UserId == _b2cHelper.GetOid(principal) && x.Type == VehicleMaintenanceTypes.Maintenance && x.Receipt != null)
                .Select(x => x.Receipt)
                .Distinct()
                .ToList();
            log.LogInformation($"Got receipts for user {_b2cHelper.GetOid(principal)}");
            return new OkObjectResult(receipts);
        }
    }
}