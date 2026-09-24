using Infrastructure.Messaging;
using Ingest;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using EFCore.NamingConventions;
using Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("iot"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IMqttBus>(_ => new MqttBus(
    builder.Configuration["Mqtt:Host"]!,
    builder.Configuration.GetValue<int>("Mqtt:Port")));

builder.Services.AddHostedService<TelemetryWorker>();
builder.Services.AddScoped<IMeasurementWriter, MeasurementWriter>();
builder.Services.AddSingleton<ITelemetryValidator, TelemetryValidator>();
builder.Services.AddSingleton<ITimestampResolver>(_ =>
    new TimestampResolver((TimeSpan.FromSeconds(
            builder.Configuration.GetValue<int>("Telemetry:TimestampToleranceSeconds")))));

var app = builder.Build();
app.MapGet("/", () => "Hello World!");

app.Run();
