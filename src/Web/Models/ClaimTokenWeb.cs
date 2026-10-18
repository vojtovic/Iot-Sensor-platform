using Domain;
namespace Web.Models;

public class ClaimTokenWeb
{
    public required int Id { get; set; }
    public required string HardwareId { get; set; } = null!;
    public string? ClaimToken { get; set; }
}
