namespace common.Models;

public record FeedbackDto
{
    public string Message { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
