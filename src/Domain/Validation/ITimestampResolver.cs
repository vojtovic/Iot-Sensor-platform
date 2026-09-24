namespace Domain;

public interface ITimestampResolver
{
    DateTimeOffset Resolve(DateTimeOffset? deviceTs, DateTimeOffset receivedAt);
}
