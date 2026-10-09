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

    public static class BuildingsEndpoints
    {


        public static void GetBuilding(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet("/buildings/{id}", async (AppDbContext iot, int id, CancellationToken ct) =>
            {


                var building = await iot.Buildings.Where(d => d.Id == id).Select(d => new BuildingDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Address = d.Address,
                    CreatedAt = d.CreatedAt
                }).FirstOrDefaultAsync(ct);
                if (building is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(building);
            });


        }

        public static void GetBuildings(this WebApplication app)
        {
            var appRoute = app.MapGroup("/api/v1");
            appRoute.MapGet("/buildings", async (AppDbContext iot, CancellationToken ct) =>
            {


                return await iot.Buildings.Select(d => new BuildingDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Address = d.Address,
                    CreatedAt = d.CreatedAt
                }).ToListAsync(ct);
            });



        }



    }

}
