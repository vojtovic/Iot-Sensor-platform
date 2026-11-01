using System.Text.Json;
using Domain;
using Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ingest;

public class TelemetryWorker(IMqttBus bus, ILogger<TelemetryWorker> logger, TelemetryQueue queue)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bus.SubscribeAsync(
            "iot/v1/+/telemetry",
            async msg =>
            {
                logger.LogDebug("Message from {Topic}: {Payload}", msg.Topic, msg.Payload);
                try
                {
                    var jsonPayload = JsonSerializer.Deserialize<TelemetryMessage>(
                        msg.Payload,
                        JsonOptions
                    );

                    if (jsonPayload is null)
                    {
                        logger.LogWarning("Empty message from {Topic}", msg.Topic);
                        return;
                    }

                    logger.LogDebug(
                        "Time: {Ts}, seq: {Seq}, read num: {Count}",
                        jsonPayload.Ts,
                        jsonPayload.Seq,
                        jsonPayload.Readings.Count
                    );

                    string[] parts = msg.Topic.Split("/");

                    foreach (var reading in jsonPayload.Readings)
                    {
                        logger.LogDebug(
                            "  {Channel} = {Value} (status: {Status})",
                            reading.Channel,
                            reading.Value,
                            reading.Status ?? "—"
                        );
                    }

                    var item = new TelemetryItem(parts[2], jsonPayload, msg.ReceivedAt);
                    await queue.Writer.WriteAsync(item, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Canceled");
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, " Inavalid message {Topic}", msg.Topic);
                }
                return;
            },
            stoppingToken
        );
    }
}
