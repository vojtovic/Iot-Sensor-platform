using Domain;

namespace api.Models;

public class AddBuildingDto
{
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}
