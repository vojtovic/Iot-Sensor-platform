namespace Simulator;

using MQTTnet;

using MQTTnet.Protocol;
using System.Text.Json;

using System;
public record Reading(string Channel, double Value, string? Status);

public class Message
{
    public DateTimeOffset Ts { get; set; }
    public int Seq { get; set; }
    public List<Reading> Readings { get; set; } = new();
};



public static class VirtualDevice
{
    private static CancellationTokenSource source = new CancellationTokenSource();
    private static CancellationToken ct = source.Token;
    private static int i = 0;
    private static string hardwareId = "sim-01";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private static string Serializer()
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


    public static MqttApplicationMessage Create_Message()
    {
        return new MqttApplicationMessageBuilder()
            .WithTopic($"iot/v1/{hardwareId}/telemetry")
            .WithPayload(Serializer())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
    }


    public static async Task Publish_Sim_Message(int delay)
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            source.Cancel();
        };

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithClientId(hardwareId)
            .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var applicationMessage = Create_Message();
                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Task was cancelled.");
        }

        await mqttClient.DisconnectAsync();

        Console.WriteLine("MQTT application message is published.");
    }

}
