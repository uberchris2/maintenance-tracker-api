using System;
using System.Security.Claims;
using common.Models;
using maintenance_tracker_api.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace maintenance_tracker_api.Functions
{
    public class Feedback
    {
        private readonly IB2cHelper _b2cHelper;

        public Feedback(IB2cHelper b2cHelper)
        {
            _b2cHelper = b2cHelper;
        }

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
            message.AddContent("text/html", $"{_b2cHelper.GetName(principal)} says:<br /><br />{request.Message}");
            message.SetFrom(_b2cHelper.GetEmail(principal));
            message.SetSubject("Feedback from MaintenanceTracker");
            log.LogInformation($"Sending feedback message from user {_b2cHelper.GetOid(principal)}");
        }
    }
}