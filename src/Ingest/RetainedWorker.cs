using Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ingest;


public class RetainedWorker(IMqttBus bus, ILogger<RetainedWorker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var iot = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var devices = await iot.Devices.Where(d => d.Status == DeviceStatus.Approved)
        .Select(d => new
        {
            HardwareId = d.HardwareId,
            LastConfig = iot.DeviceConfigurations
            .Where(c => c.DeviceId == d.Id).OrderByDescending(c => c.Version).FirstOrDefault()
        })
        .Where(d => d.LastConfig != null).ToListAsync(ct);
        int sent = 0;
        foreach (var d in devices)
        {
            try
            {
                string topic = $"iot/v1/{d.HardwareId}/config";
                await bus.PublishAsync(topic, d.LastConfig!.Payload, ct, true);
                d.LastConfig.SentAt = DateTimeOffset.UtcNow;
                sent++;

            }
            catch (Exception ex)
            {

                logger.LogError(ex, " Failed to publish configuration for {HardwareId}", d.HardwareId);
            }

        }
        logger.LogInformation("{Sent} Configurations were sent", sent);
        await iot.SaveChangesAsync(ct);
    }
}
