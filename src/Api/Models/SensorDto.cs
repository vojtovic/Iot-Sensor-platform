
using Domain;
namespace api.Models;

public class SensorDto
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string Channel { get; set; } = null!;
    public int SensorTypeId { get; set; }
    public double CalibrationOffset { get; set; }
    public ChannelStatus ChannelStatus { get; set; }
}
