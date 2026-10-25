namespace Domain;

public interface IConfigValidator
{
    string? Evaluate(ConfigMessage message, ICollection<Sensor> Sensors);
}
