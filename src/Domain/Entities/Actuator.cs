namespace Domain;

public class Actuator
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public string Kind { get; set; } = null!;
}
