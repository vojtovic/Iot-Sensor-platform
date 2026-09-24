namespace Domain;

public class Building
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
