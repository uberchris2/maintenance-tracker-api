using System;
using System.Threading.Tasks;
using common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace maintenance_tracker_api_public.Functions
{
    public class Feedback
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly ILogger<Feedback> _logger;

        public Feedback(ISendGridClient sendGridClient, ILogger<Feedback> logger)
        {
            _sendGridClient = sendGridClient;
            _logger = logger;
        }

        [Function("FeedbackPost")]
        public async Task<IActionResult> FeedbackPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feedback")] HttpRequest request)
        {
            var dto = await request.ReadFromJsonAsync<FeedbackDto>();
            var message = new SendGridMessage();
            message.AddTo(Environment.GetEnvironmentVariable("FeedbackRecipient"));
            message.AddContent("text/html", dto!.Message);
            message.SetFrom(dto.Email);
            message.SetSubject("Feedback for MaintenanceTracker");
            await _sendGridClient.SendEmailAsync(message, request.HttpContext.RequestAborted);
            _logger.LogInformation("Sending feedback message from anonymous user");
            return new OkResult();
        }
    }
}
