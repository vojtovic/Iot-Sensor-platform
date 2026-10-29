using System.Text.Json;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ingest;

public class OnlineWorker(
    IMqttBus bus,
    ILogger<OnlineWorker> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bus.SubscribeAsync(
            "iot/v1/+/status",
            async msg =>
            {
                try
                {
                    string[] parts = msg.Topic.Split("/");
                    using var scope = scopeFactory.CreateScope();

                    var iot = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var device = await iot
                        .Devices.Where(d => d.HardwareId == parts[2])
                        .FirstOrDefaultAsync(stoppingToken);
                    if (device is null)
                    {
                        logger.LogWarning("Unknown device {HardwareId}", parts[2]);
                        return;
                    }
                    if (device.IsOnline is not null)
                    {
                        if (msg.Payload == "online" && device.IsOnline is false)
                        {
                            device.IsOnline = true;
                            device.OnlineChangedAt = DateTimeOffset.UtcNow;
                            await iot.SaveChangesAsync(stoppingToken);
                        }
                        else if (msg.Payload == "offline" && device.IsOnline is true)
                        {
                            device.IsOnline = false;
                            device.OnlineChangedAt = DateTimeOffset.UtcNow;
                            await iot.SaveChangesAsync(stoppingToken);
                        }
                    }
                    else
                    {
                        if (msg.Payload == "online")
                        {
                            device.IsOnline = true;
                            device.OnlineChangedAt = DateTimeOffset.UtcNow;
                            await iot.SaveChangesAsync(stoppingToken);
                        }
                        else if (msg.Payload == "offline")
                        {
                            device.IsOnline = false;
                            device.OnlineChangedAt = DateTimeOffset.UtcNow;
                            await iot.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Canceled");
                }
                return;
            },
            stoppingToken
        );
    }
}
