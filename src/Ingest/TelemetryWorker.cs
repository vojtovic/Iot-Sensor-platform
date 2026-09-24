using Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Domain;
using Microsoft.Extensions.DependencyInjection;


namespace Ingest;



public class TelemetryWorker(IMqttBus bus, ILogger<TelemetryWorker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        await bus.SubscribeAsync("iot/v1/+/telemetry", async msg =>
        {
            logger.LogInformation("Zpráva z {Topic}: {Payload}", msg.Topic, msg.Payload);
            try
            {
                var jsonPayload = JsonSerializer.Deserialize<TelemetryMessage>(msg.Payload, JsonOptions);

                if (jsonPayload is null)
                {
                    logger.LogWarning("Prázdná zpráva z {Topic}", msg.Topic);
                    return;
                }

                logger.LogInformation("Čas: {Ts}, seq: {Seq}, počet čtení: {Count}",
                    jsonPayload.Ts, jsonPayload.Seq, jsonPayload.Readings.Count);

                string[] parts = msg.Topic.Split("/");
                using var scope = scopeFactory.CreateScope();
                var writer = scope.ServiceProvider.GetRequiredService<IMeasurementWriter>();


                foreach (var reading in jsonPayload.Readings)
                {
                    logger.LogInformation("  {Channel} = {Value} (status: {Status})",
                        reading.Channel, reading.Value, reading.Status ?? "—");

                }
                await writer.WriteAsync(parts[2], jsonPayload, msg.ReceivedAt, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Canceled");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, " Nevalidní zpráva. {Topic}", msg.Topic);
            }
            return;

        }, stoppingToken);











    }
}
