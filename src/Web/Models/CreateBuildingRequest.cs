using Domain;

namespace Web.Models;

public class CreateBuildingRequest
{
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}
