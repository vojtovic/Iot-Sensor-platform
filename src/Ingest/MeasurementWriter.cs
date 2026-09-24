using Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Domain;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Infrastructure;
namespace Ingest;

public interface IMeasurementWriter
{
    Task WriteAsync(string hardwareId, TelemetryMessage message, DateTimeOffset receivedAt, CancellationToken ct);
}



public class MeasurementWriter(AppDbContext appDbContext, ITimestampResolver timestampResolver, ITelemetryValidator telemetryValidator, ILogger<MeasurementWriter> logger) : IMeasurementWriter
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ITimestampResolver _timestampResolver = timestampResolver;


    public async Task WriteAsync(string hardwareId, TelemetryMessage message, DateTimeOffset receivedAt, CancellationToken ct)
    {

        var device = await _appDbContext.Devices.Include(d => d.Sensors).ThenInclude(s => s.SensorType).FirstOrDefaultAsync(d => d.HardwareId == hardwareId, ct);
        var ResolvedTime = _timestampResolver.Resolve(message.Ts, receivedAt);

        if (device is not null)
        {
            foreach (var reading in message.Readings)
            {
                var sensor = device.Sensors.FirstOrDefault(s => s.Channel == reading.Channel);
                if (sensor is not null)
                {
                    var telemetryValidated = telemetryValidator.Evaluate(reading, sensor.SensorType);
                    var measurement = new Measurement
                    {
                        Time = ResolvedTime,
                        SensorId = sensor.Id,
                        Value = reading.Value + sensor.CalibrationOffset,
                        Quality = telemetryValidated

                    };
                    _appDbContext.Measurements.Add(measurement);
                }
                else
                {
                    logger.LogWarning("unknown channel {Channel} in device {Device}", reading.Channel, hardwareId);
                }



            }
            await _appDbContext.SaveChangesAsync(ct);
        }
        else
        {
            logger.LogWarning("Unknown {Device} device", hardwareId);
        }



    }
}
