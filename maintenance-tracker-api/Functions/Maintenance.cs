using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using maintenance_tracker_api.Models;
using maintenance_tracker_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions
{
    public class Maintenance
    {
        private readonly IB2cHelper _b2cHelper;
        private readonly IMapper _mapper;

        public Maintenance(IB2cHelper b2cHelper, IMapper mapper)
        {
            _b2cHelper = b2cHelper;
            _mapper = mapper;
        }

        [FunctionName("MaintenancePost")]
        public void MaintenancePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "maintenance")] MaintenanceDto request,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "VehicleMaintenance",
                ConnectionStringSetting = "CosmosDBConnection")]out MaintenanceModel maintenance,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            maintenance = _mapper.Map<MaintenanceModel>(request); ;
            maintenance.id = Guid.NewGuid();
            maintenance.UserId = _b2cHelper.GetOid(principal);
            maintenance.Type = VehicleMaintenanceTypes.Maintenance;
            log.LogInformation($"Saving new maintenance id {maintenance.id} for user {_b2cHelper.GetOid(principal)}");
        }

        [FunctionName("MaintenanceDelete")]
        public async Task MaintenanceDelete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "maintenance/{id}")] HttpRequest request,
            string id,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal,
            CancellationToken token
        )
        {
            var uri = UriFactory.CreateDocumentUri("MaintenanceDB", "VehicleMaintenance", id);
            var options = new RequestOptions { PartitionKey = new PartitionKey(_b2cHelper.GetOid(principal).ToString()) };
            await client.DeleteDocumentAsync(uri, options, token);
            log.LogInformation($"Deleted maintenance id {id} for user {_b2cHelper.GetOid(principal)}");
        }
    }
}