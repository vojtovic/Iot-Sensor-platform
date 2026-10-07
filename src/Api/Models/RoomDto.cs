
using Domain;
namespace api.Models;

public class RoomDto
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public string Name { get; set; } = null!;
    public int Floor { get; set; }
    public ICollection<BuildingDto> Sensors { get; set; } = new List<BuildingDto>();
}
