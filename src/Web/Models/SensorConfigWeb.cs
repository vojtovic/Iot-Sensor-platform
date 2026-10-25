namespace Web.Models;

public class SensorConfigWeb
{
    public bool Enabled { get; set; }
    public double Deadband { get; set; }
    public string Channel { get; set; } = null!;
}
