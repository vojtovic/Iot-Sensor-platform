namespace Infrastructure;

using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Text;
using System.Buffers;
using MQTTnet.Protocol;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Security.Cryptography;

public class DeviceProvisioner : IDeviceProvisioner
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly ILogger<DeviceProvisioner> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private TaskCompletionSource<string>? _pending;

    public DeviceProvisioner(string host, string username, string password, int port, ILogger<DeviceProvisioner> logger)
    {

        _logger = logger;
        _client = new MqttClientFactory().CreateMqttClient();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithCredentials(username, password)
            .Build();

        _client.ApplicationMessageReceivedAsync += async e =>
           {

               _logger.LogInformation("dynce response: {payload}", Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray()));
               _pending?.TrySetResult(Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray()));

           };
    }

    public async Task<bool> CreateDeviceClientAsync(string Username, string Password, CancellationToken ct)
    {
        await EnsureConnectionAsync(ct);

        var payload = JsonSerializer.Serialize(
            new
            {
                commands = new[] { new{
                command = "createClient",
                username = Username,
                password = Password,
                groups = new[] {new {groupname = "devices"} } } }
            }
        );

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("$CONTROL/dynamic-security/v1")
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)   // QoS 1
            .Build();
        await _client.PublishAsync(message, ct);

        _pending = new TaskCompletionSource<string>();

        try
        {
            var answer = await _pending.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            if (answer.Contains("error"))
            {
                return false;
            }
            return true;
        }
        catch (TimeoutException ex)
        {
            _logger.LogInformation("Broker did not answer: {ex}", ex);
            return false;
        }
        finally
        {
            _pending = null;
        }

    }

    private async Task EnsureConnectionAsync(CancellationToken ct)
    {
        var subscribeOptions = new MqttClientFactory()
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic("$CONTROL/dynamic-security/v1/response").WithAtLeastOnceQoS())
            .Build();
        await _semaphore.WaitAsync(ct);
        try
        {
            if (_client.IsConnected)
            {
                return;
            }
            await _client.ConnectAsync(_options, ct);
            await _client.SubscribeAsync(subscribeOptions, ct);
        }
        finally
        {
            _semaphore.Release();
        }


    }

}
