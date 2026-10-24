
using Domain;
namespace api.Models;

public class PutConfigDto
{

    public required PutConfigMessageDto Message { get; set; }
    public ConfigTimeDto Time { get; set; } = null!;

}
