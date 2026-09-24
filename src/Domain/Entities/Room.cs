namespace Domain;

public class Room
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Floor { get; set; }
}
