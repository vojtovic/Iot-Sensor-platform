using Infrastructure.Persistence;
using Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Infrastructure;
using System.Net;
using System.Threading.Channels;
using Ingest;

using api.Models;

namespace api.Endpoints
{

    public static class RoomsEndpoints
    {


        public static void GetRoom(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet("/rooms/{id}", async (AppDbContext iot, int id, CancellationToken ct) =>
            {


                var room = await iot.Rooms.Where(d => d.Id == id).Select(d => new RoomDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    BuildingId = d.BuildingId,
                    Floor = d.Floor,
                    Building = d.Building != null ? d.Building.Name : string.Empty,
                }).FirstOrDefaultAsync(ct);
                if (room is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(room);
            });


        }

        public static void GetRooms(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet("/rooms", async (AppDbContext iot, CancellationToken ct) =>
            {
                return await iot.Rooms.Select(d => new RoomDto
                {


                    Id = d.Id,
                    Name = d.Name,
                    BuildingId = d.BuildingId,
                    Floor = d.Floor,
                    Building = d.Building != null ? d.Building.Name : string.Empty,
                }).ToListAsync(ct);
            });



        }



    }

}
