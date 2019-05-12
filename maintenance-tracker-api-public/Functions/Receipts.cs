using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace maintenance_tracker_api_public.Functions
{
    public class Receipts
    {
        private readonly IMapper _mapper;

        public Receipts(IMapper mapper)
        {
            _mapper = mapper;
        }

        [FunctionName("ReceiptAuthorizationGet")]
        public async Task<IActionResult> ReceiptAuthorizationGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request,
            [Blob("receipts", FileAccess.ReadWrite, Connection = "UploadStorage")] CloudBlobContainer container,
            [CosmosDB("MaintenanceDB", "VehicleMaintenance", ConnectionStringSetting = "CosmosDBConnection", 
                SqlQuery = "select * from VehicleMaintenance vm where UserId = '{Query.userId}' AND ((vm.id = '{Query.vehicleId}' AND vm.Shared = true) OR vm.VehicleId = 'Query.vehicleId')")] IList<VehicleMaintenanceModel> vehiclesAndMaintenance,
            ILogger log,
            CancellationToken token
        )
        {
            if (vehiclesAndMaintenance.Count(vm => vm.Type == VehicleMaintenanceTypes.Vehicle) != 1
                || !vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle).Shared
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