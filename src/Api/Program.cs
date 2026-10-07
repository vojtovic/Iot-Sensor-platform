using Infrastructure.Messaging;
using Ingest;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using EFCore.NamingConventions;
using Domain;
using Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using api.Endpoints;
using Npgsql.Internal;
using System.Text.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("iot"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IMqttBus>(_ => new MqttBus(
    builder.Configuration["Mqtt:Host"]!,
    builder.Configuration["Mqtt:Username"]!,
    builder.Configuration["Mqtt:Password"]!,
    builder.Configuration.GetValue<int>("Mqtt:Port"),
    _.GetRequiredService<ILogger<MqttBus>>(),
    _.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping
));

builder.Services.AddHostedService<TelemetryWorker>();
builder.Services.AddScoped<IMeasurementWriter, MeasurementWriter>();
builder.Services.AddSingleton<ITelemetryValidator, TelemetryValidator>();
builder.Services.AddSingleton<ITimestampResolver>(_ =>
    new TimestampResolver((TimeSpan.FromSeconds(
            builder.Configuration.GetValue<int>("Telemetry:TimestampToleranceSeconds")))));
builder.Services.AddHostedService<ClaimWorker>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddSingleton<IDeviceProvisioner>(_ => new DeviceProvisioner(
    builder.Configuration["Mqtt:Host"]!,
    builder.Configuration["Mqtt:Provisioner:Username"]!,
    builder.Configuration["Mqtt:Provisioner:Password"]!,
    builder.Configuration.GetValue<int>("Mqtt:Port"),
    _.GetRequiredService<ILogger<DeviceProvisioner>>()
));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
}

);

builder.Services.AddOpenApi();
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}






app.MapDeviceEndpoints();
app.GetDevice();
app.GenerateClaimToken();
app.UpdateDeviceStatusToDisable();
app.UpdateDeviceStatusToRejected();


app.MapGet("/", () => "Hello World!");

app.Run();
