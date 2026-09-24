namespace Domain;

public class TimestampResolver : ITimestampResolver
{

    private readonly TimeSpan _tolerance;

    public TimestampResolver(TimeSpan tolerance)
    {
        _tolerance = tolerance;
    }

    public DateTimeOffset Resolve(DateTimeOffset? deviceTs, DateTimeOffset receivedAt)
    {
        if (deviceTs is null)
            return receivedAt;

        var difference = (deviceTs.Value - receivedAt).Duration();

        return difference > _tolerance ? receivedAt : deviceTs.Value;
    }
}
