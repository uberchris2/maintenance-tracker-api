using System;
using System.Security.Claims;
using common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace maintenance_tracker_api_public.Functions
{
    public class Feedback
    {
        [FunctionName("FeedbackPost")]
        public void FeedbackPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feedback")] FeedbackDto request,
            [SendGrid] out SendGridMessage message,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            message = new SendGridMessage();
            message.AddTo(Environment.GetEnvironmentVariable("FeedbackRecipient"));
            message.AddContent("text/html", request.Message);
            message.SetFrom(request.Email);
            message.SetSubject("Feedback for MaintenanceTracker");
            log.LogInformation($"Sending feedback message from anonymous user");
        }
    }
}