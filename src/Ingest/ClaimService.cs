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
    private static async Task AddDevice(AppDbContext appDbContext, ClaimRequestMessage request, CancellationToken ct)
    {
        var capabilities = new DeviceCapabilities(request.Sensors, request.Actuators);
        var serializedJson = JsonSerializer.Serialize<DeviceCapabilities>(capabilities, JsonOptions);

        var NewDevice = new Device
        {
            HardwareId = request.HardwareId,
            Status = DeviceStatus.Pending,
            FirmwareVersion = request.FirmwareVersion,
            LastSeenAt = DateTimeOffset.UtcNow,
            PendingCapabilities = serializedJson,
            RoomId = null
        };
        appDbContext.Devices.Add(NewDevice);
        await appDbContext.SaveChangesAsync(ct);
    }

    private async Task<ClaimResponseMessage> ApproveAsync(Device device, ClaimRequestMessage request, CancellationToken ct)
    {
        if (device.PendingCapabilities is null)
        {
            return Error("No stored capability declaration for this device.");
        }
        var jsonPayload = JsonSerializer.Deserialize<DeviceCapabilities>(device.PendingCapabilities, JsonOptions);
        if (jsonPayload is null)
        {
            return Error("Stored capability declaration could not be parsed.");
        }
        var acceptedSensors = new List<string>();
        var acceptedActuators = new List<string>();
        if (jsonPayload.Sensors is not null)
        {


            var codes = jsonPayload.Sensors.Select(s => s.Type).Distinct();
            var dictionary = await appDbContext.SensorTypes.Where(st => codes.Contains(st.Code)).ToDictionaryAsync(st => st.Code, st => st.Id, ct);
            foreach (var s in jsonPayload.Sensors)
            {
                if (!dictionary.ContainsKey(s.Type))
                {

                    return Error($"Unknown sensor type '{s.Type}'.");
                }
            }


            foreach (var newSensor in jsonPayload.Sensors)
            {


                var sensor = new Sensor
                {
                    DeviceId = device.Id,
                    Channel = newSensor.Channel,
                    SensorTypeId = dictionary[newSensor.Type],
                    CalibrationOffset = 0
                };
                appDbContext.Sensors.Add(sensor);
                acceptedSensors.Add(newSensor.Channel);
            }
        ;
        }
        if (jsonPayload.Actuators is not null)
        {


            foreach (var newActuator in jsonPayload.Actuators)
            {
                var actuator = new Actuator
                {
                    DeviceId = device.Id,
                    Channel = newActuator.Channel,
                    Kind = newActuator.Kind
                };
                appDbContext.Actuators.Add(actuator);
                acceptedActuators.Add(newActuator.Channel);
            }
        ;
        }



        var password = SecretGenerator.Password();

        if (!await provisioner.CreateDeviceClientAsync(device.HardwareId, password, ct))
        {
            return Error("did not managed to create account in broker");
        }

        device.Status = DeviceStatus.Approved;
        device.PendingCapabilities = null;
        device.ClaimToken = null;
        device.CredentialsIssuedAt = DateTimeOffset.UtcNow;
        await appDbContext.SaveChangesAsync(ct);

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
            if (device is null)
            {
                await AddDevice(appDbContext, request, ct);
                logger.LogInformation("Unknown device {Device}, registered as pending", request.HardwareId);
                return Pending("device is pending", 5);
            }

            bool tokenValid = device.ClaimToken is not null && device.ClaimToken == request.ClaimToken;

            return device.Status switch
            {
                DeviceStatus.Pending when tokenValid => await ApproveAsync(device, request, ct),
                DeviceStatus.Pending when !tokenValid => Pending("Waiting for operator approval.", 5),
                DeviceStatus.Approved when tokenValid => await ApproveAsync(device, request, ct),
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

}
