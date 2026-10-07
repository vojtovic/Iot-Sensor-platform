using Domain;
namespace api.Models;

public class BuildingDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
