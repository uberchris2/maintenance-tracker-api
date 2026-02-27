using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api_public.Functions;

public class Receipts(Container cosmosContainer, BlobContainerClient blobContainer, ILogger<Receipts> logger)
{
    [Function("ReceiptAuthorizationGet")]
    public async Task<IActionResult> ReceiptAuthorizationGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request)
    {
        var vehicleId = Guid.Parse(request.Query["vehicleId"]!);
        var userId = Guid.Parse(request.Query["userId"]!);
        var name = request.Query["name"].ToString();

        var query = cosmosContainer
            .GetItemLinqQueryable<VehicleMaintenanceModel>()
            .Where(x => x.UserId == userId && (x.id == vehicleId || x.VehicleId == vehicleId))
            .ToFeedIterator();

        var records = new List<VehicleMaintenanceModel>();
        while (query.HasMoreResults)
            records.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

        var vehicleModel = records.SingleOrDefault(vm => vm.Type == VehicleMaintenanceTypes.Vehicle);
        if (vehicleModel is null || !vehicleModel.Shared
            || !records.Any(vm => vm.Type == VehicleMaintenanceTypes.Maintenance && vm.Receipt == name))
        {
            return new BadRequestResult();
        }

        var blobClient = blobContainer.GetBlobClient($"{userId}/{name}");
        var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1))
        {
            BlobContainerName = blobContainer.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        logger.LogInformation("Authorized access to receipt {ReceiptName} for anonymous user at {RemoteIp}", name, request.HttpContext.Connection.RemoteIpAddress);
        return new OkObjectResult(new ReceiptAuthorizationDto { Url = sasUri.ToString() });
    }
}
