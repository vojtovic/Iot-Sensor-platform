namespace Infrastructure.Messaging;

using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Text;
using System.Buffers;
using MQTTnet.Protocol;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;


public class MqttBus : IMqttBus
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly MqttClientDisconnectOptions _DisOptions;
    private readonly ILogger<MqttBus> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly ConcurrentDictionary<string, Func<MqttMessage, Task>> _handlers = new();

    public MqttBus(string host, string username, string password, int port, ILogger<MqttBus> logger, CancellationToken ct)
    {

        _logger = logger;
        _client = new MqttClientFactory().CreateMqttClient();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithCredentials(username, password)
            .Build();
        _DisOptions = new MqttClientDisconnectOptionsBuilder()
            .Build();


        _client.ApplicationMessageReceivedAsync += async e =>
           {
               var message = new MqttMessage(
                   e.ApplicationMessage.Topic,
                   Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray()),
                   DateTimeOffset.UtcNow);

               var found = _handlers.FirstOrDefault(kv =>
                {
                    string[] partTopic = message.Topic.Split("/");
                    string[] partKey = kv.Key.Split("/");
                    if (partTopic.Length != partKey.Length)
                    {
                        return false;
                    }
                    for (int i = 0; i < partTopic.Length; i++)
                    {
                        if (partKey[i] == "#")
                        {
                            return true;
                        }
                        if (partTopic[i] != partKey[i] && partKey[i] != "+")
                        {
                            return false;
                        }

                    }
                    return true;

                });
               if (found.Value is not null)
               {
                   await found.Value(message);
               }
               else
               {
                   _logger.LogWarning("topic not found");
               }

           };

        _client.DisconnectedAsync += async h =>
        {
            _logger.LogWarning($"Disconnected:{h.Reason}");
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, ct);
                    await EnsureConnectionAsync(ct);

                    foreach (var filter in _handlers.Keys)
                    {
                        var subscribeOptions = new MqttClientFactory()
                            .CreateSubscribeOptionsBuilder()
                            .WithTopicFilter(f => f.WithTopic(filter).WithAtLeastOnceQoS())
                            .Build();


                        await _client.SubscribeAsync(subscribeOptions, ct);

                    }

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


    private async Task EnsureConnectionAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            if (_client.IsConnected)
            {
                return;
            }
            await _client.ConnectAsync(_options, ct);
        }
        finally
        {
            _semaphore.Release();
        }


    }

    public async Task SubscribeAsync(string topicFilter, Func<MqttMessage, Task> handler, CancellationToken ct)
    {
        _handlers[topicFilter] = handler;
        await EnsureConnectionAsync(ct);

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
