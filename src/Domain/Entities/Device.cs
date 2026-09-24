namespace Domain;

public class Device
{
    public int Id { get; set; }
    public string HardwareId { get; set; } = null!;
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public DeviceStatus Status { get; set; }
    public string? FirmwareVersion { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? CredentialsIssuedAt { get; set; }
    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
