
using Domain;

namespace Web.Models;



public class DeviceWeb
{
    public int Id { get; set; }
    public string HardwareId { get; set; } = null!;
    public DeviceStatus Status { get; set; }
    public string? FirmwareVersion { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? CredentialsIssuedAt { get; set; }
    public string? Room { get; set; }
    public List<SensorWeb> Sensors { get; set; } = [];
    public List<ActuatorWeb> Actuators { get; set; } = [];


}
