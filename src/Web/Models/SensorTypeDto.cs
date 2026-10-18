namespace Web.Models;

public class SensorTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string? Unit { get; set; }
    public double MinValid { get; set; }
    public double MaxValid { get; set; }
}
