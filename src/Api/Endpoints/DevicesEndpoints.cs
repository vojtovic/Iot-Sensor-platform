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
    public static class DevicesEndpoints
    {
        public static void GetDevices(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/devices",
                async (AppDbContext iot, CancellationToken ct) =>
                {
                    return await iot
                        .Devices.Select(d => new DeviceDto
                        {
                            Id = d.Id,
                            HardwareId = d.HardwareId,
                            RoomId = d.RoomId,
                            Room = d.Room != null ? d.Room.Name : "no Room",
                            Status = d.Status,
                            FirmwareVersion = d.FirmwareVersion,
                            LastSeenAt = d.LastSeenAt,
                            IsOnline = d.IsOnline,
                            OnlineChangedAt = d.OnlineChangedAt,
                            CredentialsIssuedAt = d.CredentialsIssuedAt,
                            Sensors = d
                                .Sensors.Select(s => new SensorDto
                                {
                                    Id = s.Id,
                                    DeviceId = s.DeviceId,
                                    Channel = s.Channel,
                                    SensorTypeId = s.SensorTypeId,
                                    CalibrationOffset = s.CalibrationOffset,
                                    ChannelStatus = s.ChannelStatus,
                                })
                                .ToList(),

                            Actuators = d
                                .Actuators.Select(s => new ActuatorDto
                                {
                                    Id = s.Id,
                                    DeviceId = s.DeviceId,
                                    Channel = s.Channel,
                                    Kind = s.Kind,
                                    ChannelStatus = s.ChannelStatus,
                                })
                                .ToList(),
                        })
                        .ToListAsync(ct);
                }
            );
        }

        public static void GetDevice(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/devices/{id}",
                async (AppDbContext iot, int id, CancellationToken ct) =>
                {
                    var device = await iot
                        .Devices.Where(d => d.Id == id)
                        .Select(d => new DeviceDto
                        {
                            Id = d.Id,
                            HardwareId = d.HardwareId,
                            RoomId = d.RoomId,
                            Room = d.Room != null ? d.Room.Name : "no Room",
                            Status = d.Status,
                            IsOnline = d.IsOnline,
                            OnlineChangedAt = d.OnlineChangedAt,
                            FirmwareVersion = d.FirmwareVersion,
                            LastSeenAt = d.LastSeenAt,
                            CredentialsIssuedAt = d.CredentialsIssuedAt,
                            Sensors = d
                                .Sensors.Select(s => new SensorDto
                                {
                                    Id = s.Id,
                                    DeviceId = s.DeviceId,
                                    Channel = s.Channel,
                                    SensorTypeId = s.SensorTypeId,
                                    CalibrationOffset = s.CalibrationOffset,
                                    ChannelStatus = s.ChannelStatus,
                                })
                                .ToList(),

                            Actuators = d
                                .Actuators.Select(s => new ActuatorDto
                                {
                                    Id = s.Id,
                                    DeviceId = s.DeviceId,
                                    Channel = s.Channel,
                                    Kind = s.Kind,
                                    ChannelStatus = s.ChannelStatus,
                                })
                                .ToList(),
                        })
                        .FirstOrDefaultAsync(ct);

                    if (device is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(device);
                }
            );
        }

        public static void GenerateClaimToken(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/devices/{id}/generateclaimtoken",
                async (AppDbContext iot, int id, CancellationToken ct) =>
                {
                    var device = await iot
                        .Devices.Include(d => d.Room)
                        .Include(d => d.Sensors)
                        .Include(d => d.Actuators)
                        .FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (device is null)
                    {
                        return Results.NotFound();
                    }
                    if (device.Status == DeviceStatus.Pending)
                    {
                        device.ClaimToken = SecretGenerator.Password();
                        await iot.SaveChangesAsync(ct);
                    }
                    else
                    {
                        return Results.Conflict(new { message = "Device has to be Pending" });
                    }

                    var claimTokenDto = new ClaimTokenDto
                    {
                        Id = device.Id,
                        HardwareId = device.HardwareId,
                        ClaimToken = device.ClaimToken,
                    };

                    return Results.Ok(claimTokenDto);
                }
            );
        }

        public static void UpdateDeviceStatusToDisable(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/devices/{id}/disable",
                async (
                    AppDbContext iot,
                    IDeviceProvisioner provisioner,
                    int id,
                    CancellationToken ct
                ) =>
                {
                    var device = await iot
                        .Devices.Include(d => d.Room)
                        .Include(d => d.Sensors)
                        .Include(d => d.Actuators)
                        .FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (device is null)
                    {
                        return Results.NotFound();
                    }

                    if (device.Status == DeviceStatus.Pending)
                    {
                        return Results.Conflict(new { message = "Cant reject Panding device" });
                    }
                    if (device.Status == DeviceStatus.Rejected)
                    {
                        return Results.Conflict(new { message = "Device was already rejected" });
                    }
                    if (device.Status == DeviceStatus.Disabled)
                    {
                        return Results.Conflict(new { message = "Device was already disable" });
                    }
                    if (device.Status == DeviceStatus.Approved)
                    {
                        if (await provisioner.DisableDevice(device.HardwareId, ct))
                        {
                            device.Status = DeviceStatus.Disabled;
                            await iot.SaveChangesAsync(ct);
                        }
                        else
                        {
                            return Results.Problem(
                                "Failed to set status to disable",
                                statusCode: 502
                            );
                        }
                    }

                    var deviceDto = new DeviceDto
                    {
                        Id = device.Id,
                        HardwareId = device.HardwareId,
                        RoomId = device.RoomId,
                        Room = device.Room != null ? device.Room.Name : "no Room",
                        Status = device.Status,
                        FirmwareVersion = device.FirmwareVersion,
                        LastSeenAt = device.LastSeenAt,
                        IsOnline = device.IsOnline,
                        OnlineChangedAt = device.OnlineChangedAt,
                        CredentialsIssuedAt = device.CredentialsIssuedAt,
                        Sensors = device
                            .Sensors.Select(s => new SensorDto
                            {
                                Id = s.Id,
                                DeviceId = s.DeviceId,
                                Channel = s.Channel,
                                SensorTypeId = s.SensorTypeId,
                                CalibrationOffset = s.CalibrationOffset,
                                ChannelStatus = s.ChannelStatus,
                            })
                            .ToList(),

                        Actuators = device
                            .Actuators.Select(s => new ActuatorDto
                            {
                                Id = s.Id,
                                DeviceId = s.DeviceId,
                                Channel = s.Channel,
                                Kind = s.Kind,
                                ChannelStatus = s.ChannelStatus,
                            })
                            .ToList(),
                    };

                    return Results.Ok(deviceDto);
                }
            );
        }

        public static void UpdateDeviceStatusToRejected(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/devices/{id}/reject",
                async (AppDbContext iot, int id, CancellationToken ct) =>
                {
                    var device = await iot
                        .Devices.Include(d => d.Room)
                        .Include(d => d.Sensors)
                        .Include(d => d.Actuators)
                        .FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (device is null)
                    {
                        return Results.NotFound();
                    }

                    if (device.Status == DeviceStatus.Rejected)
                    {
                        return Results.Conflict(new { message = "Device was already rejected" });
                    }
                    if (device.Status == DeviceStatus.Disabled)
                    {
                        return Results.Conflict(new { message = "Device was already disabled" });
                    }
                    if (device.Status == DeviceStatus.Approved)
                    {
                        return Results.Conflict(
                            new { message = "Approved device cant be rejected, use disable" }
                        );
                    }
                    if (device.Status == DeviceStatus.Pending)
                    {
                        device.ClaimToken = null;
                        device.Status = DeviceStatus.Rejected;
                        await iot.SaveChangesAsync(ct);
                    }

                    var deviceDto = new DeviceDto
                    {
                        Id = device.Id,
                        HardwareId = device.HardwareId,
                        RoomId = device.RoomId,
                        Room = device.Room != null ? device.Room.Name : "no Room",
                        Status = device.Status,
                        IsOnline = device.IsOnline,
                        OnlineChangedAt = device.OnlineChangedAt,
                        FirmwareVersion = device.FirmwareVersion,
                        LastSeenAt = device.LastSeenAt,
                        CredentialsIssuedAt = device.CredentialsIssuedAt,
                        Sensors = device
                            .Sensors.Select(s => new SensorDto
                            {
                                Id = s.Id,
                                DeviceId = s.DeviceId,
                                Channel = s.Channel,
                                SensorTypeId = s.SensorTypeId,
                                CalibrationOffset = s.CalibrationOffset,
                                ChannelStatus = s.ChannelStatus,
                            })
                            .ToList(),

                        Actuators = device
                            .Actuators.Select(s => new ActuatorDto
                            {
                                Id = s.Id,
                                DeviceId = s.DeviceId,
                                Channel = s.Channel,
                                Kind = s.Kind,
                                ChannelStatus = s.ChannelStatus,
                            })
                            .ToList(),
                    };

                    return Results.Ok(deviceDto);
                }
            );
        }

        public static void UpdateDeviceRoom(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPatch(
                "/devices/{id}",
                async (
                    AppDbContext iot,
                    int id,
                    UpdateDeviceRequest request,
                    CancellationToken ct
                ) =>
                {
                    var device = await iot.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (device is null)
                    {
                        return Results.NotFound();
                    }

                    if (device.Status == DeviceStatus.Rejected)
                    {
                        return Results.Conflict(new { message = "Device was already rejected" });
                    }
                    if (request.RoomId is null)
                    {
                        return Results.BadRequest(new { message = "RoomId you entered is null" });
                    }
                    var room = await iot.Rooms.FirstOrDefaultAsync(d => d.Id == request.RoomId, ct);
                    if (room is null)
                    {
                        return Results.BadRequest(
                            new { message = "Room with this Id does not exist" }
                        );
                    }

                    device.RoomId = request.RoomId;
                    await iot.SaveChangesAsync(ct);

                    var updatedDevice = await iot
                        .Devices.Include(d => d.Room)
                        .FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (updatedDevice is not null && updatedDevice.Room is not null)
                    {
                        if (updatedDevice.Room.Name is null)
                        {
                            return Results.BadRequest(new { message = "new room name is null" });
                        }
                        return Results.Ok(
                            new
                            {
                                DeviceHardwareId = updatedDevice.HardwareId,
                                RoomId = updatedDevice.RoomId,
                                Name = updatedDevice.Room.Name,
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

        public static void UpdateDeviceStatusToPending(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/devices/{id}/restore",
                async (AppDbContext iot, int id, CancellationToken ct) =>
                {
                    var device = await iot.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (device is null)
                    {
                        return Results.NotFound();
                    }

                    if (device.Status == DeviceStatus.Rejected)
                    {
                        device.Status = DeviceStatus.Pending;
                        await iot.SaveChangesAsync(ct);
                    }
                    else
                    {
                        return Results.Conflict(new { message = "You can not do this" });
                    }

                    return Results.NoContent();
                }
            );
        }

        public static void UpdateDeviceStatusToApproved(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/devices/{id}/enable",
                async (
                    AppDbContext iot,
                    int id,
                    IDeviceProvisioner provisioner,
                    CancellationToken ct
                ) =>
                {
                    var device = await iot.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (device is null)
                    {
                        return Results.NotFound();
                    }

                    if (device.Status == DeviceStatus.Disabled)
                    {
                        if (await provisioner.EnableDevice(device.HardwareId, ct))
                        {
                            device.Status = DeviceStatus.Approved;
                            await iot.SaveChangesAsync(ct);
                        }
                        else
                        {
                            return Results.Problem(
                                "Failed to set status to disable",
                                statusCode: 502
                            );
                        }
                    }
                    else
                    {
                        return Results.Conflict(new { message = "You can not do this" });
                    }

                    return Results.NoContent();
                }
            );
        }

        public static void ApproveDevice(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/devices/{id}/approved",
                async (AppDbContext iot, int id, IMqttBus mqtt, CancellationToken ct) =>
                {
                    var device = await iot
                        .Devices.Include(d => d.Room)
                        .Include(d => d.Sensors)
                        .Include(d => d.Actuators)
                        .FirstOrDefaultAsync(d => d.Id == id, ct);

                    if (device is null)
                    {
                        return Results.NotFound();
                    }
                    if (device.Status != DeviceStatus.Pending)
                    {
                        return Results.Conflict(new { message = "Device has to be Pending" });
                    }
                    if (device.DeviceSecret is null)
                    {
                        return Results.Conflict(
                            new
                            {
                                message = "Device has no secret (old firmware), use generate token",
                            }
                        );
                    }
                    device.ClaimToken = device.DeviceSecret;
                    if (!await iot.DeviceConfigurations.Where(c => c.DeviceId == id).AnyAsync(ct))
                    {
                        ConfigMessage resultMessage = new ConfigMessage(
                            1,
                            60,
                            900,
                            new ChannelConfig(
                                device
                                    .Sensors.Where(s => s.ChannelStatus != ChannelStatus.Declined)
                                    .Select(c => new SensorConfig(true, 0, c.Channel))
                                    .ToList()
                            )
                        );
                        var jsonPayload = JsonSerializer.Serialize<ConfigMessage>(
                            resultMessage,
                            JsonOptionsWrite
                        );
                        DeviceConfiguration ConfigToAdd = new()
                        {
                            DeviceId = id,
                            Version = 1,
                            Payload = jsonPayload,
                            CreatedAt = DateTimeOffset.UtcNow,
                        };
                        string topic = $"iot/v1/{device.HardwareId}/config";

                        await iot.AddAsync(ConfigToAdd, ct);
                        await iot.SaveChangesAsync(ct);
                        await mqtt.PublishAsync(topic, jsonPayload, ct, true);
                        ConfigToAdd.SentAt = DateTimeOffset.UtcNow;
                        await iot.SaveChangesAsync(ct);
                    }
                    await iot.SaveChangesAsync(ct);
                    return Results.Ok(new { message = "Device Approved" });
                }
            );
        }

        private static readonly JsonSerializerOptions JsonOptionsRead = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public static void GetDeviceConfig(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/devices/{id}/config",
                async (AppDbContext iot, int id, CancellationToken ct) =>
                {
                    var deviceExists = await iot.Devices.Where(d => d.Id == id).AnyAsync(ct);

                    if (deviceExists is false)
                    {
                        return Results.NotFound(new { message = "Device not found" });
                    }

                    var config = await iot
                        .DeviceConfigurations.Where(c => c.DeviceId == id)
                        .OrderByDescending(c => c.Version)
                        .FirstOrDefaultAsync(ct);
                    if (config is null)
                    {
                        return Results.NotFound(new { message = "Device has no configuration" });
                    }
                    var jsonPayload = JsonSerializer.Deserialize<ConfigMessage>(
                        config.Payload,
                        JsonOptionsRead
                    );
                    if (jsonPayload is null || jsonPayload.Channels is null)
                    {
                        return Results.InternalServerError(
                            new { message = "Stored configuration is invalid" }
                        );
                    }

                    if (jsonPayload.Channels.Sensors is null)
                    {
                        return Results.InternalServerError(
                            new { message = "Stored configuration is invalid" }
                        );
                    }
                    ConfigMessageDto resultMessage = new()
                    {
                        Version = config.Version,
                        Interval = jsonPayload.Interval,
                        MaxSilence = jsonPayload.MaxSilence,
                        Channels = new ChannelConfigDto
                        {
                            Sensors = jsonPayload
                                .Channels.Sensors.Select(c => new SensorConfigDto
                                {
                                    Enabled = c.Enabled,
                                    Deadband = c.Deadband,
                                    Channel = c.Channel,
                                })
                                .ToList(),
                        },
                    };
                    ConfigTimeDto resultTime = new()
                    {
                        CreatedAt = config.CreatedAt,
                        SentAt = config.SentAt,
                        AcknowledgedAt = config.Acknowledged,
                    };

                    ConfigDto configResult = new() { Message = resultMessage, Time = resultTime };

                    return Results.Ok(configResult);
                }
            );
        }

        private static readonly JsonSerializerOptions JsonOptionsWrite = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static void PutDeviceConfig(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPut(
                "/devices/{id}/config",
                async (
                    AppDbContext iot,
                    int id,
                    PutConfigDto request,
                    IConfigValidator validator,
                    CancellationToken ct,
                    IMqttBus mqtt
                ) =>
                {
                    var device = await iot
                        .Devices.Where(d => d.Id == id)
                        .Include(s => s.Sensors)
                            .ThenInclude(t => t.SensorType)
                        .FirstOrDefaultAsync(ct);

                    if (device is null)
                    {
                        return Results.NotFound(new { message = "Device not found" });
                    }
                    if (device.Status != DeviceStatus.Approved)
                    {
                        return Results.Conflict(new { message = "Device has to be Approved" });
                    }

                    var config = await iot
                        .DeviceConfigurations.Where(c => c.DeviceId == id)
                        .OrderByDescending(c => c.Version)
                        .FirstOrDefaultAsync(ct);

                    if (request is null || request.Message.Channels is null)
                    {
                        return Results.BadRequest(new { message = "Bad request" });
                    }

                    if (request.Message.Channels.Sensors is null)
                    {
                        return Results.BadRequest(
                            new { message = "Stored configuration is invalid" }
                        );
                    }
                    int newVersion;
                    if (config is not null)
                    {
                        newVersion = config.Version + 1;
                    }
                    else
                    {
                        newVersion = 1;
                    }

                    ConfigMessage resultMessage = new ConfigMessage(
                        newVersion,
                        request.Message.Interval,
                        request.Message.MaxSilence,
                        new ChannelConfig(
                            request
                                .Message.Channels.Sensors.Select(c => new SensorConfig(
                                    c.Enabled,
                                    c.Deadband,
                                    c.Channel
                                ))
                                .ToList()
                        )
                    );
                    if (validator.Evaluate(resultMessage, device.Sensors) is not null)
                    {
                        return Results.BadRequest(
                            new
                            {
                                message = $"Config is not valid : {validator.Evaluate(resultMessage, device.Sensors)}",
                            }
                        );
                    }

                    var jsonPayload = JsonSerializer.Serialize<ConfigMessage>(
                        resultMessage,
                        JsonOptionsWrite
                    );

                    DeviceConfiguration ConfigToAdd = new()
                    {
                        DeviceId = id,
                        Version = newVersion,
                        Payload = jsonPayload,
                        CreatedAt = DateTimeOffset.UtcNow,
                    };

                    string topic = $"iot/v1/{device.HardwareId}/config";

                    await iot.AddAsync(ConfigToAdd, ct);
                    await iot.SaveChangesAsync(ct);
                    await mqtt.PublishAsync(topic, jsonPayload, ct, true);

                    ConfigToAdd.SentAt = DateTimeOffset.UtcNow;
                    await iot.SaveChangesAsync(ct);
                    return Results.NoContent();
                }
            );
        }

        public static void DisableDeviceConfig(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPut(
                "/devices/{id}/config/disable",
                async (AppDbContext iot, int id, CancellationToken ct, IMqttBus mqtt) =>
                {
                    var device = await iot
                        .Devices.Where(d => d.Id == id)
                        .Include(s => s.Sensors)
                            .ThenInclude(t => t.SensorType)
                        .FirstOrDefaultAsync(ct);

                    if (device is null)
                    {
                        return Results.NotFound(new { message = "Device not found" });
                    }
                    if (device.Status != DeviceStatus.Approved)
                    {
                        return Results.Conflict(new { message = "Device has to be Approved" });
                    }

                    string topic = $"iot/v1/{device.HardwareId}/config";

                    await mqtt.PublishAsync(topic, "", ct, true);
                    return Results.NoContent();
                }
            );
        }
    }
}
