using Domain;

namespace api.Models;

public class UpdateSensorRequest
{
    public ChannelStatus? status { get; set; }
    public double? CalibrationOffset { get; set; }
}
