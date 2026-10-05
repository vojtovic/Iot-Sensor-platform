using Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Infrastructure;
using System.Net;
using System.Threading.Channels;


namespace Ingest;


public interface IClaimService
{
    Task<ClaimResponseMessage> HandleAsync(ClaimRequestMessage request, CancellationToken ct);
}

public record DeviceCapabilities(IReadOnlyList<SensorDeclaration>? Sensors, IReadOnlyList<ActuatorDeclaration>? Actuators);



public class ClaimService(AppDbContext appDbContext, IDeviceProvisioner provisioner, ILogger<ClaimService> logger) : IClaimService
{

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static async Task<ClaimResponseMessage?> AddDevice(AppDbContext appDbContext, ClaimRequestMessage request, ILogger<ClaimService> logger, CancellationToken ct)
    {

        var NewDevice = new Device
        {
            HardwareId = request.HardwareId,
            Status = DeviceStatus.Pending,
            FirmwareVersion = request.FirmwareVersion,
            LastSeenAt = DateTimeOffset.UtcNow,
            RoomId = null
        };


        var codes = request.Sensors?.Select(s => s.Type).Distinct() ?? Array.Empty<string>();


        var dictionary = await appDbContext.SensorTypes.Where(st => codes.Contains(st.Code)).ToDictionaryAsync(st => st.Code, st => st.Id, ct);
        var unknownTypeListSensors = new List<string>();
        var ChaWithUnknown = new List<string>();
        if (request.Sensors is not null)
        {
            foreach (var s in request.Sensors)
            {
                if (s.Type is null)
                {
                    return Error($"Sensor Channel {s.Channel} is null");
                }
                if (!dictionary.ContainsKey(s.Type))
                {
                    unknownTypeListSensors.Add(s.Type);
                    ChaWithUnknown.Add(s.Channel);

                }
            }
            if (unknownTypeListSensors.Any())
            {
                logger.LogWarning("Unknown Sensor {Device} in Channel {Chanel}", unknownTypeListSensors, ChaWithUnknown);
                return Error($"Unknown Sensor {string.Join(", ", unknownTypeListSensors)} in channels {string.Join(", ", ChaWithUnknown)}");
            }


            foreach (var newSensor in request.Sensors)
            {


                var sensor = new Sensor
                {
                    Device = NewDevice,
                    Channel = newSensor.Channel,
                    ChannelStatus = ChannelStatus.Pending,
                    SensorTypeId = dictionary[newSensor.Type],
                    CalibrationOffset = 0
                };
                appDbContext.Sensors.Add(sensor);
            }
            ;

        }

        if (request.Actuators is not null)
        {


            foreach (var newActuator in request.Actuators)
            {
                var actuator = new Actuator
                {
                    Device = NewDevice,
                    Channel = newActuator.Channel,
                    ChannelStatus = ChannelStatus.Pending,
                    Kind = newActuator.Kind
                };
                appDbContext.Actuators.Add(actuator);
            }
            ;
        }
        appDbContext.Devices.Add(NewDevice);
        await appDbContext.SaveChangesAsync(ct);
        return null;
    }

    private async Task<ClaimResponseMessage> ApproveAsync(Device device, ClaimRequestMessage request, CancellationToken ct)
    {

        var password = SecretGenerator.Password();

        if (!await provisioner.CreateDeviceClientAsync(device.HardwareId, password, ct))
        {
            return Error("did not managed to create account in broker");
        }


        var toApproveSensors = await appDbContext.Sensors.Where(st => st.ChannelStatus == ChannelStatus.Pending && st.DeviceId == device.Id).ToListAsync(ct);
        var toApproveActuators = await appDbContext.Actuators.Where(st => st.ChannelStatus == ChannelStatus.Pending && st.DeviceId == device.Id).ToListAsync(ct);

        device.Status = DeviceStatus.Approved;
        device.ClaimToken = null;
        device.CredentialsIssuedAt = DateTimeOffset.UtcNow;
        await appDbContext.SaveChangesAsync(ct);

        foreach (var approve in toApproveSensors)
        {
            approve.ChannelStatus = ChannelStatus.Active;
        }
        foreach (var approve in toApproveActuators)
        {
            approve.ChannelStatus = ChannelStatus.Active;
        }

        await appDbContext.SaveChangesAsync(ct);



        var acceptedSensors = toApproveSensors.Select(s => (s.Channel)).ToList();
        var acceptedActuators = toApproveActuators.Select(s => (s.Channel)).ToList();
        return new ClaimResponseMessage(
            Status: ClaimStatus.Credentials,
            ServerTime: DateTimeOffset.UtcNow,
            Username: device.HardwareId,
            Password: password,
            RetryAfter: null,
            Message: "Approved",
            Channels: new ClaimChannels(

                new ChannelGroup(acceptedSensors, []),
                new ChannelGroup(acceptedActuators, []))
        );

    }

    private static ClaimResponseMessage Pending(string message, int retryAfter) => new ClaimResponseMessage(
        Status: ClaimStatus.Pending,
        ServerTime: DateTimeOffset.UtcNow,
        Username: null,
        Password: null,
        RetryAfter: retryAfter,
        Message: message,
        Channels: null
    );

    private static ClaimResponseMessage Rejected(string message) => new ClaimResponseMessage(
        Status: ClaimStatus.Rejected,
        ServerTime: DateTimeOffset.UtcNow,
        Username: null,
        Password: null,
        RetryAfter: null,
        Message: message,
        Channels: null
    );

    private static ClaimResponseMessage Error(string message) => new ClaimResponseMessage(
        Status: ClaimStatus.Error,
        ServerTime: DateTimeOffset.UtcNow,
        Username: null,
        Password: null,
        RetryAfter: null,
        Message: message,
        Channels: null
    );

    public async Task<ClaimResponseMessage> HandleAsync(ClaimRequestMessage request, CancellationToken ct)
    {
        if (request.HardwareId is not null && (request.Sensors is not null || request.Actuators is not null))
        {
            var device = await appDbContext.Devices.FirstOrDefaultAsync(d => d.HardwareId == request.HardwareId, ct);

            var controlListSensors = new List<string>();
            if (request.Sensors is not null)
            {
                foreach (var reqSensor in request.Sensors)
                {
                    if (string.IsNullOrWhiteSpace(reqSensor.Channel))
                    {
                        return Error($"Sensor \"{reqSensor.Channel}\"   is not a usable channel name.");
                    }
                    controlListSensors.Add(reqSensor.Channel);
                }
                var duplicateSensorChannels = request.Sensors
                    .GroupBy(a => a.Channel)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                if (request.Sensors.Count != controlListSensors.Distinct().Count())
                {
                    return Error($"Channels in Sensors declared more than once: {string.Join(", ", duplicateSensorChannels)}");
                }
            }
            var controlListActuators = new List<string>();
            if (request.Actuators is not null)
            {
                foreach (var reqActuators in request.Actuators)
                {
                    if (string.IsNullOrWhiteSpace(reqActuators.Channel) && string.IsNullOrWhiteSpace(reqActuators.Kind))
                    {
                        return Error($"No Channel no Kind.");
                    }

                    if (string.IsNullOrWhiteSpace(reqActuators.Channel))
                    {
                        return Error($"Actuator Kind {reqActuators.Kind} has no Channel.");
                    }
                    if (string.IsNullOrWhiteSpace(reqActuators.Kind))
                    {
                        return Error($"Actuator Channel {reqActuators.Channel} has no Kind.");
                    }

                    controlListActuators.Add(reqActuators.Channel);
                }
                var duplicateActuatorChannels = request.Actuators
                    .GroupBy(a => a.Channel)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (request.Actuators.Count != controlListActuators.Distinct().Count())
                {
                    return Error($"Channels in Actuator declared more than once: {string.Join(", ", duplicateActuatorChannels)}");
                }
            }

            if (device is null)
            {

                var AddDeviceOut = await AddDevice(appDbContext, request, logger, ct);
                logger.LogInformation("Unknown device {Device}, registered as pending", request.HardwareId);
                if (AddDeviceOut is null)
                {
                    return Pending("device is pending", 5);
                }
                else
                {
                    return AddDeviceOut;
                }
            }
            device.LastSeenAt = DateTimeOffset.UtcNow;
            device.FirmwareVersion = request.FirmwareVersion;
            await appDbContext.SaveChangesAsync(ct);


            bool tokenValid = device.ClaimToken is not null && device.ClaimToken == request.ClaimToken;
            if (device.Status == DeviceStatus.Rejected || device.Status == DeviceStatus.Disabled)
            {
                return Rejected("Device Rejected or Disabled");
            }

            var DeclarationApplied = await ApplyDeclaration(device, request, ct);
            if (DeclarationApplied is not null)
            {
                return DeclarationApplied;
            }




            return device.Status switch
            {
                DeviceStatus.Pending when tokenValid => await ApproveAsync(device, request, ct),
                DeviceStatus.Pending when !tokenValid => Pending("Waiting for operator approval.", 5),
                DeviceStatus.Approved when tokenValid => await ReissueCredentialsAsync(request, device, ct),
                DeviceStatus.Approved when !tokenValid => Pending("Device is already approved. A valid claim token is required to issue credentials.", 5),
                DeviceStatus.Rejected or DeviceStatus.Disabled => Rejected("Device has been rejected or disabled by the operator."),
                _ => Rejected("Unexpected device state.")
            };

        }


        return new ClaimResponseMessage(
               Status: ClaimStatus.Rejected,
               ServerTime: DateTimeOffset.UtcNow,
               Username: null,
               Password: null,
               RetryAfter: null,
               Message: "Device is rejected",
               Channels: null
           );


    }

    private async Task<ClaimResponseMessage?> ApplyDeclaration(Device device, ClaimRequestMessage request, CancellationToken ct)
    {
        var codes = request.Sensors?.Select(s => s.Type).Distinct() ?? Array.Empty<string>();
        var dictionary = await appDbContext.SensorTypes.Where(st => codes.Contains(st.Code)).ToDictionaryAsync(st => st.Code, st => st.Id, ct);

        var sensors = await appDbContext.Sensors.Where(s => s.DeviceId == device.Id).Include(s => s.SensorType).ToListAsync(ct);


        var actuators = await appDbContext.Actuators.Where(s => s.DeviceId == device.Id).ToListAsync(ct);

        if (request.Sensors is not null)
        {
            var invalidSensorTypes = request.Sensors
                .Where(s => s.Type == null || !dictionary.ContainsKey(s.Type))
                .Select(s => s.Type)
                .ToList();

            if (invalidSensorTypes.Any())
            {

                logger.LogWarning("Unknown sensor types {type}", invalidSensorTypes);
                return Error($"Unknown sensor types: {string.Join(", ", invalidSensorTypes)}");
            }

            foreach (var requestSensor in request.Sensors)
            {
                var existingSensor = sensors.FirstOrDefault(s => s.Channel == requestSensor.Channel);

                var targetSensorTypeId = dictionary[requestSensor.Type];

                if (existingSensor is not null)
                {
                    if (existingSensor.ChannelStatus == ChannelStatus.Declined)
                    {

                    }
                    else if (existingSensor.ChannelStatus == ChannelStatus.Inactive)
                    {
                        existingSensor.ChannelStatus = ChannelStatus.Pending;
                    }
                    else if (existingSensor.SensorTypeId != targetSensorTypeId)
                    {

                        return Error(
                            $"Channel '{requestSensor.Channel}' is registered as '{existingSensor.SensorType.Code}' " +
                            $"but the device now declares '{requestSensor.Type}'. " +
                            "A type change must be resolved by the operator.");
                    }

                }
                else
                {
                    var newSensor = new Sensor
                    {
                        Device = device,
                        Channel = requestSensor.Channel,
                        ChannelStatus = ChannelStatus.Pending,
                        SensorTypeId = dictionary[requestSensor.Type],
                        CalibrationOffset = 0
                    };
                    appDbContext.Sensors.Add(newSensor);

                }
            }
        }

        if (request.Actuators is not null)
        {
            foreach (var requestActuator in request.Actuators)
            {
                var existingActuator = actuators.FirstOrDefault(s => s.Channel == requestActuator.Channel);


                if (existingActuator is not null)
                {
                    if (existingActuator.ChannelStatus == ChannelStatus.Declined)
                    {

                    }
                    else if (existingActuator.ChannelStatus == ChannelStatus.Inactive)
                    {
                        existingActuator.ChannelStatus = ChannelStatus.Pending;
                    }
                    else if (existingActuator.Kind != requestActuator.Kind)
                    {

                        return Error(
                            $"Channel '{requestActuator.Channel}' is registered as '{existingActuator.Kind}' " +
                            $"but the device now declares '{requestActuator.Kind}'. " +
                            "A type change must be resolved by the operator.");
                    }

                }
                else
                {
                    var newActuator = new Actuator
                    {
                        Device = device,
                        Channel = requestActuator.Channel,
                        ChannelStatus = ChannelStatus.Pending,
                        Kind = requestActuator.Kind
                    };
                    appDbContext.Actuators.Add(newActuator);

                }
            }
        }




        var requestChannels = request.Sensors?.Select(s => s.Channel).ToList();
        foreach (var sensor in sensors)
        {
            if (requestChannels is not null)
            {


                if (!requestChannels.Contains(sensor.Channel) && sensor.ChannelStatus != ChannelStatus.Declined)
                {
                    sensor.ChannelStatus = ChannelStatus.Inactive;
                }
            }
        }



        var requestChannelsActuators = request.Actuators?.Select(s => s.Channel).ToList();
        foreach (var actuator in actuators)
        {
            if (requestChannelsActuators is not null)
            {


                if (!requestChannelsActuators.Contains(actuator.Channel) && actuator.ChannelStatus != ChannelStatus.Declined)
                {
                    actuator.ChannelStatus = ChannelStatus.Inactive;
                }
            }
        }

        await appDbContext.SaveChangesAsync(ct);
        return null;
    }





    private async Task<ClaimResponseMessage> ReissueCredentialsAsync(ClaimRequestMessage request, Device device, CancellationToken ct)
    {
        var password = SecretGenerator.Password();
        if (!await provisioner.SetDevicePasswordAsync(device.HardwareId, password, ct))
        {
            return Error("Failed to change password on the broker.");
        }
        device.ClaimToken = null;
        device.CredentialsIssuedAt = DateTimeOffset.UtcNow;
        await appDbContext.SaveChangesAsync(ct);
        var sensors = await appDbContext.Sensors.Where(s => s.DeviceId == device.Id)
                        .Select(s => new { s.Channel, ChannelStatus = s.ChannelStatus }).ToListAsync(ct);
        var actuators = await appDbContext.Actuators.Where(s => s.DeviceId == device.Id)
                        .Select(s => new { s.Channel, ChannelStatus = s.ChannelStatus }).ToListAsync(ct);




        var AccetedSensors = sensors.Where(s => s.ChannelStatus == ChannelStatus.Active).Select(s => (s.Channel)).ToList();
        var PendigSensors = sensors.Where(s => s.ChannelStatus == ChannelStatus.Pending).Select(s => (s.Channel)).ToList();


        var AcceptedActuators = actuators.Where(s => s.ChannelStatus == ChannelStatus.Active).Select(s => (s.Channel)).ToList();
        var PendigActuators = actuators.Where(s => s.ChannelStatus == ChannelStatus.Pending).Select(s => (s.Channel)).ToList();


        return new ClaimResponseMessage(
            Status: ClaimStatus.Credentials,
            ServerTime: DateTimeOffset.UtcNow,
            Username: device.HardwareId,
            Password: password,
            RetryAfter: null,
            Message: "Credentials reissued.",
            Channels: new ClaimChannels(

                new ChannelGroup(AccetedSensors, PendigSensors),
                new ChannelGroup(AcceptedActuators, PendigActuators)));






    }

}
