using AutoMapper;
using maintenance_tracker_api;
using maintenance_tracker_api.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(Startup))]
namespace maintenance_tracker_api
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            MappingInitializer.Activate();

            builder.Services.AddTransient<IB2cHelper, B2cHelper>();
            builder.Services.AddSingleton(Mapper.Instance);
        }
    }
}
