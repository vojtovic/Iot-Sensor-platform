using Domain;
namespace api.Models;

public class ConfigTimeDto
{
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
}
