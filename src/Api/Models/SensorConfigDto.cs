
using Domain;
namespace api.Models;

public class SensorConfigDto
{
    public required bool Enabled { get; set; }
    public required double Deadband { get; set; }
    public required string Channel { get; set; }
}
