namespace Web.Models;

public class RequestedSensorTypeWeb
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string HardwareId { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
