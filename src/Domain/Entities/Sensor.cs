namespace Domain;

public class Sensor
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public int SensorTypeId { get; set; }
    public SensorType SensorType { get; set; } = null!;
    public double CalibrationOffset { get; set; }
}
