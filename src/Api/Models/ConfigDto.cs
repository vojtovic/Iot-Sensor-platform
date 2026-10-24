
using Domain;
namespace api.Models;

public class ConfigDto
{

    public required ConfigMessageDto Message { get; set; }
    public ConfigTimeDto Time { get; set; } = null!;

}
