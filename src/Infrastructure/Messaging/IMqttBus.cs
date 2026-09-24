namespace Infrastructure.Messaging;

public record MqttMessage(
string Topic,
string Payload,
DateTimeOffset ReceivedAt
);


public interface IMqttBus
{
    Task SubscribeAsync(string topicFilter, Func<MqttMessage, Task> handler, CancellationToken ct);
    Task PublishAsync(string topic, string payload, CancellationToken ct);
}
