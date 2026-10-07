using Domain;
namespace api.Models;

public class ClaimTokenDto
{
    public required int Id { get; set; }
    public required string HardwareId { get; set; } = null!;
    public string? ClaimToken { get; set; }


}
