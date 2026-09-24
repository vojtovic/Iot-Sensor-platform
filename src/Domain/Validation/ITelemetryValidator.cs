namespace Domain;

public interface ITelemetryValidator
{
    Quality Evaluate(Reading reading, SensorType type);
}
