namespace Domain;

public class Measurement
{
    public DateTimeOffset Time { get; set; }
    public int SensorId { get; set; }
    public Sensor Sensor { get; set; } = null!;
    public double Value { get; set; }
    public Quality Quality { get; set; }
}
