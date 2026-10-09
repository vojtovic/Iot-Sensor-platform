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
    }
}
