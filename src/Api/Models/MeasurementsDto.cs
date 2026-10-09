using Domain;

namespace api.Models;

public class MeasurmentsDto
{
    public int SensorId { get; set; }
    public double Value { get; set; }
    public Quality Quality { get; set; }
    public string? Sensor { get; set; }
    public string? Device { get; set; }
    public DateTimeOffset Time { get; set; }
}
