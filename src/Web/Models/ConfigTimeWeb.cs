namespace Web.Models;

public class ConfigTimeWeb
{
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
}
