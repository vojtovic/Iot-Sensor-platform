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
    public static class BuildingsEndpoints
    {
        public static void GetBuilding(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/buildings/{id}",
                async (AppDbContext iot, int id, CancellationToken ct) =>
                {
                    var building = await iot
                        .Buildings.Where(d => d.Id == id)
                        .Select(d => new BuildingDto
                        {
                            Id = d.Id,
                            Name = d.Name,
                            Address = d.Address,
                            CreatedAt = d.CreatedAt,
                        })
                        .FirstOrDefaultAsync(ct);
                    if (building is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(building);
                }
            );
        }

        public static void GetBuildings(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/buildings",
                async (AppDbContext iot, CancellationToken ct) =>
                {
                    return await iot
                        .Buildings.Select(d => new BuildingDto
                        {
                            Id = d.Id,
                            Name = d.Name,
                            Address = d.Address,
                            CreatedAt = d.CreatedAt,
                        })
                        .ToListAsync(ct);
                }
            );
        }

        public static void AddBuilding(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/buildings",
                async (AppDbContext iot, AddBuildingDto request, CancellationToken ct) =>
                {
                    if (request is null)
                    {
                        return Results.BadRequest(new { message = "request is null" });
                    }
                    if (string.IsNullOrWhiteSpace(request.Name))
                    {
                        return Results.BadRequest(new { message = "request cant be null" });
                    }
                    var newbuilding = new Building
                    {
                        Name = request.Name,
                        Address = request.Address,
                        CreatedAt = DateTimeOffset.UtcNow,
                    };

                    iot.Buildings.Add(newbuilding);
                    await iot.SaveChangesAsync(ct);
                    var newBuilding = new BuildingDto
                    {
                        Id = newbuilding.Id,
                        Name = newbuilding.Name,
                        Address = newbuilding.Address,
                        CreatedAt = newbuilding.CreatedAt,
                    };

                    return Results.Created($"/api/v1/buildings/{newBuilding.Id}", newBuilding);
                }
            );
        }
    }
}
