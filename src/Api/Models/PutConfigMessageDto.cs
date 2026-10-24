
using Domain;
namespace api.Models;

public class PutConfigMessageDto
{
    public required int Interval { get; set; }
    public required int MaxSilence { get; set; }
    public required ChannelConfigDto Channels { get; set; }

}
