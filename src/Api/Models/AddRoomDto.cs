using Domain;

namespace api.Models;

public class addRoomDto
{
    public int BuildingId { get; set; }
    public string Name { get; set; } = null!;
    public int Floor { get; set; }
}
