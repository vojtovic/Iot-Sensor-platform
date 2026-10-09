using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using api.Models;
using Domain;
using Infrastructure;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Ingest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace api.Endpoints
{
    public static class MeasurementsEndpoints
    {
        public static void GetDeviceMeasurements(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/devices/{id}/measurements",
                async (
                    AppDbContext iot,
                    int id,
                    DateTimeOffset from,
                    DateTimeOffset to,
                    CancellationToken ct
                ) =>
                {
                    if (!await iot.Devices.AnyAsync(s => s.Id == id, ct))
                    {
                        return Results.NotFound(
                            new { message = "There is no device with this id" }
                        );
                    }
                    if (from >= to)
                    {
                        return Results.BadRequest(
                            new { message = "Make sure your to value is bigger than from value" }
                        );
                    }
                    if ((to - from) > TimeSpan.FromDays(7))
                    {
                        return Results.BadRequest(
                            new { message = "Please select a shorter time range." }
                        );
                    }
                    var DeviceMeasurements = iot.Measurements.Where(s =>
                        s.Sensor != null
                        && s.Sensor.DeviceId == id
                        && s.Time >= from.ToUniversalTime()
                        && s.Time <= to.ToUniversalTime()
                    );
                    var measurements = await DeviceMeasurements
                        .OrderBy(m => m.Time)
                        .Select(m => new MeasurmentsDto
                        {
                            SensorId = m.SensorId,
                            Value = m.Value,
                            Quality = m.Quality,
                            Sensor = m.Sensor != null ? m.Sensor.Channel : "No channel",
                            Device = m.Sensor != null ? m.Sensor.Device.HardwareId : "no device",
                            Time = m.Time,
                        })
                        .ToListAsync(ct);

                    return Results.Ok(measurements);
                }
            );
        }
    }
}
