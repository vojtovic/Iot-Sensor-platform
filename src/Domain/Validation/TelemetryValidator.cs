namespace Domain;

public class TelemetryValidator : ITelemetryValidator
{
    public Quality Evaluate(Reading reading, SensorType type)
    {
        if (reading.Status == "error")
            return Quality.Error;

        if (reading.Value > type.MaxValid || reading.Value < type.MinValid)
            return Quality.Suspect;

        return Quality.Ok;
    }
}
