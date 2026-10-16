using Domain;

namespace Web.Models;

public class CreateRoomRequest
{
    public string Name { get; set; } = null!;
    public int BuildingId { get; set; }
    public int Floor { get; set; }
}
