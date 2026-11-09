using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Endpoints;
using Domain;
using EFCore.NamingConventions;
using Infrastructure;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Ingest;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Npgsql.Internal;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("iot"))
        .UseSnakeCaseNamingConvention()
);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("AppDb")
);

builder.Services.AddAuthorization(options =>
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"))
);

builder.Services.AddSingleton<IMqttBus>(_ => new MqttBus(
    builder.Configuration["Mqtt:Host"]!,
    builder.Configuration["Mqtt:Username"]!,
    builder.Configuration["Mqtt:Password"]!,
    builder.Configuration.GetValue<int>("Mqtt:Port"),
    _.GetRequiredService<ILogger<MqttBus>>(),
    _.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping
));

builder
    .Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddHostedService<TelemetryWorker>();
builder.Services.AddSingleton<TelemetryQueue>();

builder.Services.AddHostedService<TelemetryProcessor>();
builder.Services.AddScoped<IMeasurementWriter, MeasurementWriter>();
builder.Services.AddSingleton<ITelemetryValidator, TelemetryValidator>();
builder.Services.AddSingleton<ITimestampResolver>(_ => new TimestampResolver(
    (
        TimeSpan.FromSeconds(
            builder.Configuration.GetValue<int>("Telemetry:TimestampToleranceSeconds")
        )
    )
));

builder.Services.AddSingleton<IDeviceKeyVerifier>(_ => new DeviceKeyVerifier(
    (builder.Configuration["Claim:GroupKey"]!)
));

builder.Services.AddHostedService<ClaimWorker>();
builder.Services.AddHostedService<RetainedWorker>();
builder.Services.AddHostedService<ConfigWorker>();
builder.Services.AddHostedService<OnlineWorker>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddSingleton<IConfigValidator, ConfigValidator>();
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
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
    );
});

builder.Services.AddOpenApi();
var app = builder.Build();
app.MapStaticAssets().AllowAnonymous();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    var email = builder.Configuration["Admin:Email"];
    var password = builder.Configuration["Admin:Password"];
    if (email is null || password is null)
    {
        throw new InvalidOperationException("No admin password or username");
    }
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        var result = await roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin role was not created: "
                    + string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }
    }

    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
    {
        user = new IdentityUser { UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin user was not created: "
                    + string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }
    }

    if (!await userManager.IsInRoleAsync(user, "Admin"))
    {
        var result = await userManager.AddToRoleAsync(user, "Admin");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin role was not assigned: "
                    + string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }
    }

    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var api = app.MapGroup("/api/v1").RequireAuthorization("Admin");

app.MapIdentityApi<IdentityUser>();
app.MapPost("/register", () => Results.NotFound()).WithOrder(-1);
api.GetDevices();
api.GetDevice();
api.GenerateClaimToken();
api.UpdateDeviceStatusToDisable();
api.UpdateDeviceStatusToRejected();
api.GetBuildings();
api.GetBuilding();
api.GetRooms();
api.GetRoom();
api.UpdateDeviceRoom();
api.GetDeviceMeasurements();
api.UpdateSensor();
api.UpdateActuator();
api.AddBuilding();
api.AddRoom();
api.GetBuildingRooms();
api.UpdateDeviceStatusToPending();
api.UpdateDeviceStatusToApproved();
api.ApproveDevice();
api.AddSensorTypes();
api.GetSensorsTypes();
api.GetDeviceConfig();
api.PutDeviceConfig();
api.GetRequestedSensorTypes();
app.MapGet("/", () => "Hello World!");

app.Run();
