namespace Infrastructure.Messaging;

using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Text;
using System.Buffers;
using MQTTnet.Protocol;


public class MqttBus : IMqttBus
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly MqttClientDisconnectOptions _DisOptions;
    private readonly ILogger<MqttBus> _logger;

    public MqttBus(string host, string username, string password, int port, ILogger<MqttBus> logger)
    {
        _logger = logger;
        _client = new MqttClientFactory().CreateMqttClient();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithCredentials(username, password)
            .Build();

        _DisOptions = new MqttClientDisconnectOptionsBuilder()
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


        _client.DisconnectedAsync += async h =>
        {
            _logger.LogWarning($"Disconnected:{h.Reason}");
            while (!ct.IsCancellationRequested)
            {



                try
                {
                    await Task.Delay(5000, ct);
                    await _client.ConnectAsync(_options, ct);
                    await _client.SubscribeAsync(subscribeOptions, ct);
                    _logger.LogInformation("Reconnected");
                    break;
                }
                catch (OperationCanceledException)
                {
                    //log
                    _logger.LogInformation("END , OFF");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Reconnect failed:");
                }

            }

        };

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
