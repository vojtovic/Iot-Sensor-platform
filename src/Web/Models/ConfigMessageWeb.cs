using Domain;
namespace Web.Models;

public class ConfigMessageWeb
{
    public int Version { get; set; }
    public int Interval { get; set; }
    public int MaxSilence { get; set; }
    public ChannelsWeb Channels { get; set; } = null!;

}
