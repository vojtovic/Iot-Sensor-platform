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
    public static class SensorsEndpoints
    {
        public static void UpdateSensor(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPatch(
                "/sensor/{id}",
                async (
                    AppDbContext iot,
                    int id,
                    UpdateSensorRequest request,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await iot.Sensors.FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (sensor is null)
                    {
                        return Results.NotFound();
                    }

                    if (sensor.ChannelStatus == ChannelStatus.Inactive)
                    {
                        return Results.Conflict(
                            new { message = "you cant chage inactive channel" }
                        );
                    }
                    if (
                        request.status == ChannelStatus.Inactive
                        || request.status == ChannelStatus.Pending
                    )
                    {
                        return Results.BadRequest(
                            new { message = "you cant make channel this status" }
                        );
                    }
                    if (request is null)
                    {
                        return Results.BadRequest(new { message = "request is null" });
                    }
                    if (request.status is null && request.CalibrationOffset is null)
                    {
                        return Results.BadRequest(new { message = "no changes" });
                    }
                    if (request.status is not null)
                    {
                        sensor.ChannelStatus = request.status.Value;
                    }
                    if (request.CalibrationOffset is not null)
                    {
                        sensor.CalibrationOffset = request.CalibrationOffset.Value;
                    }
                    await iot.SaveChangesAsync(ct);

                    var updatedSensor = await iot.Sensors.FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (updatedSensor is not null)
                    {
                        return Results.Ok(
                            new
                            {
                                Channel = updatedSensor.Channel,
                                ChannelStatus = updatedSensor.ChannelStatus,
                            }
                        );
                    }
                    else
                    {
                        return Results.NoContent();
                    }
                }
            );
        }
    }
}
