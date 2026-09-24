namespace Domain;

public class DeviceConfiguration
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public int Version { get; set; }
    public string Payload { get; set; } = null!;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? Acknowledged { get; set; }
}
