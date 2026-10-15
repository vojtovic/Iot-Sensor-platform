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
    public static class RoomsEndpoints
    {
        public static void GetRoom(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/rooms/{id}",
                async (AppDbContext iot, int id, CancellationToken ct) =>
                {
                    var room = await iot
                        .Rooms.Where(d => d.Id == id)
                        .Select(d => new RoomDto
                        {
                            Id = d.Id,
                            Name = d.Name,
                            BuildingId = d.BuildingId,
                            Floor = d.Floor,
                            Building = d.Building != null ? d.Building.Name : string.Empty,
                        })
                        .FirstOrDefaultAsync(ct);
                    if (room is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(room);
                }
            );
        }

        public static void GetRooms(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet(
                "/rooms",
                async (AppDbContext iot, CancellationToken ct) =>
                {
                    return await iot
                        .Rooms.Select(d => new RoomDto
                        {
                            Id = d.Id,
                            Name = d.Name,
                            BuildingId = d.BuildingId,
                            Floor = d.Floor,
                            Building = d.Building != null ? d.Building.Name : string.Empty,
                        })
                        .ToListAsync(ct);
                }
            );
        }

        public static void AddRoom(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapPost(
                "/rooms",
                async (AppDbContext iot, addRoomDto request, CancellationToken ct) =>
                {
                    if (request is null)
                    {
                        return Results.BadRequest(new { message = "request is null" });
                    }
                    if (string.IsNullOrWhiteSpace(request.Name))
                    {
                        return Results.BadRequest(new { message = "name cant be null" });
                    }
                    var building = await iot.Buildings.FirstOrDefaultAsync(
                        d => d.Id == request.BuildingId,
                        ct
                    );
                    if (building is null)
                    {
                        return Results.BadRequest(
                            new { message = "Building with this Id does not exist" }
                        );
                    }

                    var newroom = new Room
                    {
                        Name = request.Name,
                        Floor = request.Floor,
                        BuildingId = request.BuildingId,
                    };

                    iot.Rooms.Add(newroom);
                    await iot.SaveChangesAsync(ct);
                    var newRoom = new RoomDto
                    {
                        Id = newroom.Id,
                        BuildingId = newroom.BuildingId,
                        Name = newroom.Name,
                        Floor = newroom.Floor,
                        Building = building.Name,
                    };

                    return Results.Created($"/api/v1/rooms/{newRoom.Id}", newRoom);
                }
            );
        }
    }
}
