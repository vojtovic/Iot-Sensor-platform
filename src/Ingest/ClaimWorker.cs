using Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Ingest;


public class ClaimWorker(IMqttBus bus, ILogger<ClaimWorker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptionsRead = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions JsonOptionsWrite = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

    };

    protected override async Task ExecuteAsync(CancellationToken ct)
    {

        await bus.SubscribeAsync("iot/v1/claim/request", async msg =>
        {
            logger.LogInformation("Zpráva z {Topic}: {Payload}", msg.Topic, msg.Payload);
            try
            {
                var jsonPayload = JsonSerializer.Deserialize<ClaimRequestMessage>(msg.Payload, JsonOptionsRead);

                if (jsonPayload is null)
                {
                    logger.LogWarning("Prázdná zpráva z {Topic}", msg.Topic);
                    return;
                }

                logger.LogInformation("Čas: {Ts}, HardwareId: {HardwareId}, ClaimToken: {ClaimToken}, FirmwareVersion: {FirmwareVersion}",
                    jsonPayload.Ts, jsonPayload.HardwareId, jsonPayload.ClaimToken, jsonPayload.FirmwareVersion);

                using var scope = scopeFactory.CreateScope();
                var claimService = scope.ServiceProvider.GetRequiredService<IClaimService>();

                if (jsonPayload.Sensors != null)
                {

                    foreach (var sensor in jsonPayload.Sensors)
                    {
                        logger.LogInformation("  {Channel} = {Type}",
                            sensor.Channel, sensor.Type);

                    }
                }

                if (jsonPayload.Actuators != null)
                {
                    foreach (var actuator in jsonPayload.Actuators)
                    {
                        logger.LogInformation("  {Channel} = {Kind}",
                            actuator.Channel, actuator.Kind);

                    }
                }
                var response = await claimService.HandleAsync(jsonPayload, ct);
                logger.LogInformation("Response: {Status} {Message}", response.Status, response.Message);
                var serializedJson = JsonSerializer.Serialize<ClaimResponseMessage>(response, JsonOptionsWrite);

                var topic = "iot/v1/claim/response/" + (jsonPayload.ClaimToken ?? jsonPayload.HardwareId);

                await bus.PublishAsync(topic, serializedJson, ct);




            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Canceled");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, " Invalid message. {Topic}", msg.Topic);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, " Exception. {Topic}", msg.Topic);
            }
            return;

        }, ct);

    }
}
