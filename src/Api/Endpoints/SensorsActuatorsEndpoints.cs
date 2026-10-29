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
                "/sensors/{id}",
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
                                CalibrationOffset = updatedSensor.CalibrationOffset,
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

        public static void UpdateActuator(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPatch(
                "/actuators/{id}",
                async (
                    AppDbContext iot,
                    int id,
                    UpdateActuatorRequest request,
                    CancellationToken ct
                ) =>
                {
                    var actuator = await iot.Actuators.FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (actuator is null)
                    {
                        return Results.NotFound();
                    }

                    if (actuator.ChannelStatus == ChannelStatus.Inactive)
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
                    if (request.status is null)
                    {
                        return Results.BadRequest(new { message = "no changes" });
                    }
                    if (request.status is not null)
                    {
                        actuator.ChannelStatus = request.status.Value;
                    }
                    await iot.SaveChangesAsync(ct);

                    var updatedActuator = await iot.Actuators.FirstOrDefaultAsync(
                        d => d.Id == id,
                        ct
                    );

                    if (updatedActuator is not null)
                    {
                        return Results.Ok(
                            new
                            {
                                Channel = updatedActuator.Channel,
                                ChannelStatus = updatedActuator.ChannelStatus,
                                Kind = updatedActuator.Kind,
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

        public static void GetSensorsTypes(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/sensor-types",
                async (AppDbContext iot, CancellationToken ct) =>
                {
                    return await iot
                        .SensorTypes.Select(d => new SensorTypeDto
                        {
                            Id = d.Id,
                            Code = d.Code,
                            Unit = d.Unit,
                            MinValid = d.MinValid,
                            MaxValid = d.MaxValid,
                        })
                        .ToListAsync(ct);
                }
            );
        }

        private static bool IsCodeValid(ReadOnlySpan<char> code)
        {
            if (code.IsEmpty)
            {
                return false;
            }

            foreach (char c in code)
            {
                if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '_')
                {
                    return false;
                }
            }
            return true;
        }

        public static void AddSensorTypes(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/sensor-types",
                async (AppDbContext iot, AddSensorTypeDto request, CancellationToken ct) =>
                {
                    if (request is null)
                    {
                        return Results.BadRequest(new { message = "request is null" });
                    }
                    if (
                        string.IsNullOrWhiteSpace(request.Code)
                        || string.IsNullOrWhiteSpace(request.Unit)
                    )
                    {
                        return Results.BadRequest(new { message = "request cant be null" });
                    }

                    if (request.MinValid >= request.MaxValid)
                    {
                        return Results.BadRequest(new { message = "MaxValid has to be bigger" });
                    }

                    string normalizedCode = request.Code.Trim().ToLowerInvariant();

                    if (!IsCodeValid(normalizedCode))
                    {
                        return Results.BadRequest(
                            new
                            {
                                message = "Code must contain only lowercase letters (a-z) and digits (0-9) and _",
                            }
                        );
                    }
                    if (await iot.SensorTypes.AnyAsync(s => s.Code == normalizedCode, ct))
                    {
                        return Results.Conflict(
                            new
                            {
                                message = $"Sensor type with code '{normalizedCode}'already exists.",
                            }
                        );
                    }

                    var newsensor = new SensorType
                    {
                        Code = normalizedCode,
                        Unit = request.Unit,
                        MinValid = request.MinValid,
                        MaxValid = request.MaxValid,
                    };

                    iot.SensorTypes.Add(newsensor);
                    await iot.SaveChangesAsync(ct);
                    await iot
                        .RequestedSensorTypes.Where(d => d.Code == normalizedCode)
                        .ExecuteDeleteAsync(ct);

                    var newSensor = new SensorTypeDto
                    {
                        Id = newsensor.Id,
                        Code = newsensor.Code,
                        Unit = newsensor.Unit,
                        MinValid = newsensor.MinValid,
                        MaxValid = newsensor.MaxValid,
                    };

                    return Results.Created($"/api/v1/sensor-types/{newSensor.Id}", newSensor);
                }
            );
        }

        public static void GetRequestedSensorTypes(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/sensor-types/requested",
                async (AppDbContext iot, CancellationToken ct) =>
                {
                    return await iot
                        .RequestedSensorTypes.OrderByDescending(d => d.LastSeenAt)
                        .Select(d => new RequestedSensorTypeDTO
                        {
                            Id = d.Id,
                            Code = d.Code,
                            HardwareId = d.HardwareId,
                            Channel = d.Channel,
                            FirstSeenAt = d.FirstSeenAt,
                            LastSeenAt = d.LastSeenAt,
                        })
                        .ToListAsync(ct);
                }
            );
        }
    }
}
