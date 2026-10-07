
using Domain;
namespace api.Models;

public class DeviceDto
{
    public required int Id { get; set; }
    public required string HardwareId { get; set; } = null!;
    public int? RoomId { get; set; }
    public DeviceStatus Status { get; set; }
    public string? FirmwareVersion { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? CredentialsIssuedAt { get; set; }
    public ICollection<SensorDto> Sensors { get; set; } = new List<SensorDto>();
    public ICollection<ActuatorDto> Actuators { get; set; } = new List<ActuatorDto>();
    public string? Room { get; set; }


}
