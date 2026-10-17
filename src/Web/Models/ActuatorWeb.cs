
using Domain;
namespace Web.Models;

public class ActuatorWeb
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string Channel { get; set; } = null!;
    public string Kind { get; set; } = null!;
    public ChannelStatus ChannelStatus { get; set; }
}
