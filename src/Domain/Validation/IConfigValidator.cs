namespace Domain;

public interface IConfigValidator
{
    bool Evaluate(ConfigMessage message, ICollection<Sensor> Sensors);
}
