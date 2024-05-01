using CreekRiver.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using CreekRiver.Models.DTOs;
using System.Reflection.Metadata.Ecma335;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// allows passing datetimes without time zone data 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// allows our api endpoints to access the database through Entity Framework Core
builder.Services.AddNpgsql<CreekRiverDbContext>(builder.Configuration["CreekRiverDbConnectionString"]);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/campsites", (CreekRiverDbContext db) =>
{
    return db.Campsites.Select(c => new CampsiteDTO
    {
        Id = c.Id,
        Nickname = c.Nickname,
        ImageUrl = c.ImageUrl,
        CampsiteTypeId = c.CampsiteTypeId
    }).ToList();
});

app.MapGet("/api/campsites/{id}", (CreekRiverDbContext db, int id) =>
{
    CampsiteDTO campsiteDTO = null;

    try
    {
        campsiteDTO = db.Campsites
            .Include(c => c.CampsiteType)
            .Select(c => new CampsiteDTO
            {
                Id = c.Id,
                Nickname = c.Nickname,
                CampsiteTypeId = c.CampsiteTypeId,
                CampsiteType = new CampsiteTypeDTO
                {
                    Id = c.CampsiteType.Id,
                    CampsiteTypeName = c.CampsiteType.CampsiteTypeName,
                    FeePerNight = c.CampsiteType.FeePerNight,
                    MaxReservationDays = c.CampsiteType.MaxReservationDays
                }
            }).Single(c => c.Id == id);
    }
    catch (InvalidOperationException)
    {
        return Results.NotFound();
    }
    
    return Results.Ok(campsiteDTO);
});

app.MapPost("/api/campsites", (CreekRiverDbContext db, Campsite campsite) =>
{
    db.Campsites.Add(campsite);
    db.SaveChanges();
    return Results.Created($"/api/campsites/{campsite.Id}", campsite);
});

app.MapDelete("/api/campsites/{id}", (CreekRiverDbContext db, int id) =>
{
    Campsite campsite = db.Campsites.SingleOrDefault(campsite => campsite.Id == id);
    if (campsite == null)
    {
        return Results.NotFound();
    }

    db.Campsites.Remove(campsite);
    db.SaveChanges();

    return Results.NoContent();
});

app.Run();
