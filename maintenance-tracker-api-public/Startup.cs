using AutoMapper;
using common;
using maintenance_tracker_api_public;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(Startup))]
namespace maintenance_tracker_api_public
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            MappingInitializer.Activate();

            builder.Services.AddSingleton(Mapper.Instance);
        }
    }
}
