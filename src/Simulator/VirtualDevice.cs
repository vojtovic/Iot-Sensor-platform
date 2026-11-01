namespace Simulator;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Domain;
using MQTTnet;
using MQTTnet.Protocol;

public record Reading(string Channel, double Value, string? Status);

public class Message
{
    public DateTimeOffset Ts { get; set; }
    public int Seq { get; set; }
    public List<Reading> Readings { get; set; } = new();
};

public class VirtualDevice(string hardwareId)
{
    public int Sent { get; private set; }
    public int Failed { get; private set; }
    public bool Connected { get; private set; }
    private int i = 0;
    public List<double> Times = new List<double>();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private string Serializer()
    {
        var value = new DataGenerator();
        var massage = new Message
        {
            Ts = DateTime.UtcNow,
            Seq = i,
            Readings = new List<Reading>
            {
                new Reading("temp1", value.GenerateValue(20, 33, 0.2), value.GenerateStatus()),
                new Reading("hum1", value.GenerateValue(20, 90, 5), value.GenerateStatus()),
                new Reading("co2", value.GenerateValue(400, 1200, 20), value.GenerateStatus()),
            },
        };

        i++;
        return JsonSerializer.Serialize(massage, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptionsRead = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public MqttApplicationMessage Create_Message()
    {
        return new MqttApplicationMessageBuilder()
            .WithTopic($"iot/v1/{hardwareId}/telemetry")
            .WithPayload(Serializer())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
    }

    public MqttApplicationMessage Offline()
    {
        return new MqttApplicationMessageBuilder()
            .WithTopic($"iot/v1/{hardwareId}/status")
            .WithPayload("offline")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(true)
            .Build();
    }

    public MqttApplicationMessage Online()
    {
        return new MqttApplicationMessageBuilder()
            .WithTopic($"iot/v1/{hardwareId}/status")
            .WithPayload("online")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(true)
            .Build();
    }

    public MqttApplicationMessage CreateClaimRequest(string claimToken)
    {
        var sensors = new List<SensorDeclaration>
        {
            new SensorDeclaration("temp1", "temperature"),
            new SensorDeclaration("co2", "co2"),
            new SensorDeclaration("hum1", "humidity"),
        };
        var actuators = new List<ActuatorDeclaration>();
        var message = new ClaimRequestMessage(
            hardwareId,
            claimToken,
            "v.sim.0.1",
            DateTimeOffset.UtcNow,
            sensors,
            actuators
        );

        return new MqttApplicationMessageBuilder()
            .WithTopic("iot/v1/claim/request")
            .WithPayload(JsonSerializer.Serialize(message, JsonOptions))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
    }

    public async Task Publish_Sim_Message(int delay, string add, CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();

        string? groupKey = Environment.GetEnvironmentVariable("Claim__GroupKey");
        if (groupKey is null)
        {
            Console.WriteLine("ClaimGroupKey is null.");
            return;
        }

        DeviceKeyVerifier deviceKeyVerifier = new DeviceKeyVerifier(groupKey);
        var key = deviceKeyVerifier.Calculate(hardwareId);
        var password = Environment.GetEnvironmentVariable("MQTT_PASSWORD");

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions2 = new MqttClientOptionsBuilder()
            .WithTcpServer(add, 1883)
            .WithClientId(key)
            .Build();

        var ConnAsyncResult = await mqttClient.ConnectAsync(
            mqttClientOptions2,
            CancellationToken.None
        );
        if (ConnAsyncResult.ResultCode != 0)
        {
            Console.WriteLine("Mqtt connection was not succesfull");
            return;
        }
        var taskCompletionSource = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
            if (!taskCompletionSource.TrySetResult(payload))
            {
                Console.WriteLine("Ignoring message");
            }
            return Task.CompletedTask;
        };
        var mqttClientOptions3 = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter($"iot/v1/claim/response/{key}", MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        var subsribeRequest = await mqttClient.SubscribeAsync(
            mqttClientOptions3,
            CancellationToken.None
        );
        Console.WriteLine($"{subsribeRequest.Items.First().ResultCode.ToString()}");
        var publishResult = await mqttClient.PublishAsync(
            CreateClaimRequest(key),
            CancellationToken.None
        );
        string taskCompleteResult;
        try
        {
            taskCompleteResult = await taskCompletionSource.Task.WaitAsync(
                TimeSpan.FromSeconds(10)
            );
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"ERROR:{ex}");
            return;
        }

        await mqttClient.DisconnectAsync();
        var deserializededtaskCompleteResult = JsonSerializer.Deserialize<ClaimResponseMessage>(
            taskCompleteResult,
            JsonOptionsRead
        );
        if (deserializededtaskCompleteResult is null)
        {
            return;
        }
        if (deserializededtaskCompleteResult.Status != ClaimStatus.Credentials)
        {
            Console.WriteLine(
                $"Status is not Credantials, {deserializededtaskCompleteResult.Status}"
            );
            return;
        }

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(add, 1883)
            .WithClientId(hardwareId)
            .WithCredentials(
                deserializededtaskCompleteResult.Username,
                deserializededtaskCompleteResult.Password
            )
            .WithWillTopic($"iot/v1/{hardwareId}/status")
            .WithWillPayload("offline")
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithWillRetain(true)
            .Build();

        var clientConnectResultFromReconect = await mqttClient.ConnectAsync(
            mqttClientOptions,
            CancellationToken.None
        );
        if (clientConnectResultFromReconect.ResultCode != 0)
        {
            Console.WriteLine("Mqtt connection was not succesfull");
            return;
        }
        Connected = true;
        var offline = Offline();

        var online = Online();
        var PublishAsyncResult1 = await mqttClient.PublishAsync(online, CancellationToken.None);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var applicationMessage = Create_Message();

                    Stopwatch sw1 = Stopwatch.StartNew();
                    var PublishAsyncResult = await mqttClient.PublishAsync(
                        applicationMessage,
                        CancellationToken.None
                    );
                    if (!PublishAsyncResult.IsSuccess)
                    {
                        Console.WriteLine(
                            $"Publish failed: {PublishAsyncResult.ReasonCode} {PublishAsyncResult.ReasonString}"
                        );
                        Failed++;
                    }
                    else
                    {
                        Sent++;

                        Times.Add(sw1.Elapsed.TotalMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}");
                    Failed++;
                }
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Task was cancelled.");
        }
        try
        {
            var PublishAsyncResult2 = await mqttClient.PublishAsync(
                offline,
                CancellationToken.None
            );

            await mqttClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex}");
        }
        Console.WriteLine("MQTT application message is published. Disconnected device is offline");
        sw.Stop();
    }
}
