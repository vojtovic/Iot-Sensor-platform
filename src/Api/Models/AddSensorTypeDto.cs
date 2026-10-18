namespace api.Models;

public class AddSensorTypeDto
{
    public string Code { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public double MinValid { get; set; }
    public double MaxValid { get; set; }
}
