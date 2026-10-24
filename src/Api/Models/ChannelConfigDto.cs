
using Domain;
namespace api.Models;

public class ChannelConfigDto
{

    public ICollection<SensorConfigDto> Sensors { get; set; } = new List<SensorConfigDto>();



}
