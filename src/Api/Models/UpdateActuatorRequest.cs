using Domain;

namespace api.Models;

public class UpdateActuatorRequest
{
    public ChannelStatus? status { get; set; }
}
