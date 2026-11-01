using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ingest;

public class TelemetryProcessor(
    TelemetryQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<TelemetryProcessor> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var taskList = new List<Task>();
        for (var i = 0; i < 4; i++)
        {
            taskList.Add(ProcessAsync(ct));
        }
        await Task.WhenAll(taskList);
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await foreach (var q in queue.Reader.ReadAllAsync(ct))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var imes = scope.ServiceProvider.GetRequiredService<IMeasurementWriter>();
                await imes.WriteAsync(q.HardwareId, q.Message, q.ReceivedAt, ct);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "{h}", q.HardwareId);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{q.HardwareId}", q.HardwareId);
            }
        }
    }
}
