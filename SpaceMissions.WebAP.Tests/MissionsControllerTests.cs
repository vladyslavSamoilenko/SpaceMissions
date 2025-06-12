using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SpaceMissions.Core.Entities;
using SpaceMissions.Infrastructure.Data;
using SpaceMissions.WebAP.DTOs;

namespace SpaceMissions.WebAP.Tests;

[TestFixture]
public class MissionsControllerTests
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.Single(d =>
                        d.ServiceType == typeof(DbContextOptions<SpaceMissionsDbContext>));
                    services.Remove(descriptor);

                    // Register InMemory test database
                    services.AddDbContext<SpaceMissionsDbContext>(opts =>
                        opts.UseInMemoryDatabase("TestDb_Missions"));

                    // Ensure fresh database instance
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<SpaceMissionsDbContext>();
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();

                    // Seed test data
                    var rocket = new Rocket { Id = 1, Name = "Falcon 9", IsActive = true };
                    var mission = new Mission
                    {
                        Id = 1,
                        MissionName = "Test Launch",
                        Company = "SpaceX",
                        Location = "Cape Canaveral",
                        LaunchDateTime = DateTime.UtcNow.AddDays(-1),
                        MissionStatus = "Success",
                        Price = 50000000,
                        Rocket = rocket
                    };
                    db.Rockets.Add(rocket);
                    db.Missions.Add(mission);
                    db.SaveChanges();
                });
            });

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetMissions_ReturnsPaginatedList()
    {
        var response = await _client!.GetAsync("/api/missions");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<Resourсe<PaginatedResponseDto<MissionListItemDto>>>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Data.Items.Count, Is.EqualTo(1));
        Assert.That(result.Data.Items[0].MissionName, Is.EqualTo("Test Launch"));
    }
}
