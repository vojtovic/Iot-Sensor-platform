using Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace Ingest;



public class ConfigWorker(IMqttBus bus, ILogger<ConfigWorker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        await bus.SubscribeAsync("iot/v1/+/config/ack", async msg =>
        {
            logger.LogInformation("Message from {Topic}: {Payload}", msg.Topic, msg.Payload);
            try
            {
                var jsonPayload = JsonSerializer.Deserialize<AckMessage>(msg.Payload, JsonOptions);

                if (jsonPayload is null)
                {
                    logger.LogWarning("Empty message from {Topic}", msg.Topic);
                    return;
                }

                logger.LogInformation("Version: {Version}, Accepted: {Accepted}, Error: {Error}",
                    jsonPayload.Version, jsonPayload.Accepted, jsonPayload.Error);

                string[] parts = msg.Topic.Split("/");
                using var scope = scopeFactory.CreateScope();
                var iot = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var deviceConfig = await iot.DeviceConfigurations.Where(d => d.Device.HardwareId == parts[2] && d.Version == jsonPayload.Version).FirstOrDefaultAsync(stoppingToken);

                if (deviceConfig is null)
                {
                    logger.LogWarning("Message is null");
                    return;
                }
                if (jsonPayload.Accepted && deviceConfig.Acknowledged is null)
                {
                    deviceConfig.Acknowledged = DateTimeOffset.UtcNow;
                }
                if (!jsonPayload.Accepted)
                {
                    logger.LogWarning("Not accepted");
                    logger.LogError("Error: {Error}", jsonPayload.Error);
                    return;
                }

                await iot.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Canceled");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, " Inavalid message. {Topic}", msg.Topic);
            }
            return;

        }, stoppingToken);











    }
}
