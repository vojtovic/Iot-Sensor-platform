namespace Infrastructure.Messaging;

using MQTTnet;
using System.Text;
using System.Buffers;
using MQTTnet.Protocol;


public class MqttBus : IMqttBus
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;

    public MqttBus(string host, int port)
    {
        _client = new MqttClientFactory().CreateMqttClient();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .Build();
    }


    public async Task SubscribeAsync(string topicFilter, Func<MqttMessage, Task> handler, CancellationToken ct)
    {
        _client.ApplicationMessageReceivedAsync += async e =>
           {
               var message = new MqttMessage(
                   e.ApplicationMessage.Topic,
                   Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray()),
                   DateTimeOffset.UtcNow);

               await handler(message);
           };

        await _client.ConnectAsync(_options, ct);

        var subscribeOptions = new MqttClientFactory()
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(topicFilter).WithAtLeastOnceQoS())
            .Build();

        await _client.SubscribeAsync(subscribeOptions, ct);


    }
    public async Task PublishAsync(string topic, string payload, CancellationToken ct)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)   // QoS 1
            .Build();

        await _client.PublishAsync(message, ct);

    }
}
