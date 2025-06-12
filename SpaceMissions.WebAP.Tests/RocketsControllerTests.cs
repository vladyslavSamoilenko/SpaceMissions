using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SpaceMissions.Core.Entities;
using SpaceMissions.Infrastructure.Data;
using SpaceMissions.WebAP.DTOs;

namespace SpaceMissions.WebAP.Tests
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
        ) : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, "testuser")
            }, "Test"));
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    [TestFixture]
    public class RocketsControllerTests
    {
        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var descriptor = services.Single(d =>
                            d.ServiceType == typeof(DbContextOptions<SpaceMissionsDbContext>));
                        services.Remove(descriptor);
                        services.AddDbContext<SpaceMissionsDbContext>(opts =>
                            opts.UseInMemoryDatabase("TestDb_Rockets"));

                        services.AddAuthentication("Test")
                            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                                "Test", options => { });
                        services.PostConfigure<AuthenticationOptions>(opts =>
                        {
                            opts.DefaultScheme = "Test";
                        });

                        var sp = services.BuildServiceProvider();
                        using var scope = sp.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<SpaceMissionsDbContext>();
                        db.Database.EnsureDeleted();
                        db.Database.EnsureCreated();

                        var rocket1 = new Rocket { Id = 1, Name = "Falcon 1", IsActive = true };
                        var rocket2 = new Rocket { Id = 2, Name = "Soyuz", IsActive = false };
                        db.Rockets.AddRange(rocket1, rocket2);
                        db.Missions.Add(new Mission {
                            Id = 100,
                            MissionName = "TestMission",
                            Company = "TestCo",
                            Location = "TestLand",
                            LaunchDateTime = System.DateTime.UtcNow,
                            MissionStatus = "Planned",
                            Price = 12345,
                            Rocket = rocket1
                        });
                        db.SaveChanges();
                    });
                });

            _client = _factory.CreateClient();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Test, Order(1)]
        public async Task GetRockets_ReturnsAll()
        {
            var res = await _client.GetAsync("/api/rockets");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

            var wrapper = await res.Content.ReadFromJsonAsync<Resourсe<IEnumerable<RocketDto>>>();
            var list = wrapper!.Data.ToList();
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(list.Any(r => r.Name == "Falcon 1"));
            Assert.IsTrue(list.Any(r => r.Name == "Soyuz"));
        }

        [Test, Order(2)]
        public async Task GetRocket_ById_ReturnsRocket()
        {
            var res = await _client.GetAsync("/api/rockets/1");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

            var wrapper = await res.Content.ReadFromJsonAsync<Resourсe<RocketDto>>();
            Assert.AreEqual("Falcon 1", wrapper!.Data.Name);
        }

        [Test, Order(3)]
        public async Task GetRocket_NotFound_Returns404()
        {
            var res = await _client.GetAsync("/api/rockets/999");
            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Test, Order(4)]
        public async Task PostRocket_CreatesRocket_ReturnsCreated()
        {
            var dto = new RocketDto { Name = "Starship", IsActive = true };
            var res = await _client.PostAsJsonAsync("/api/rockets", dto);
            Assert.AreEqual(HttpStatusCode.Created, res.StatusCode);

            var wrapper = await res.Content.ReadFromJsonAsync<Resourсe<RocketDto>>();
            Assert.AreEqual("Starship", wrapper!.Data.Name);
            Assert.IsTrue(wrapper.Data.IsActive);
        }

        [Test, Order(5)]
        public async Task PutRocket_UpdatesRocket_ReturnsNoContent()
        {
            var postDto = new RocketDto { Name = "TempRocket", IsActive = false };
            var postRes = await _client.PostAsJsonAsync("/api/rockets", postDto);
            var created = (await postRes.Content.ReadFromJsonAsync<Resourсe<RocketDto>>())!.Data;
            var updatedDto = new RocketDto { Id = created.Id, Name = "TempRocketUpdated", IsActive = true };

            var putRes = await _client.PutAsJsonAsync($"/api/rockets/{created.Id}", updatedDto);
            Assert.AreEqual(HttpStatusCode.NoContent, putRes.StatusCode);

            var getRes = await _client.GetAsync($"/api/rockets/{created.Id}");
            var got = (await getRes.Content.ReadFromJsonAsync<Resourсe<RocketDto>>())!.Data;
            Assert.AreEqual("TempRocketUpdated", got.Name);
            Assert.IsTrue(got.IsActive);
        }

        [Test, Order(6)]
        public async Task DeleteRocket_RemovesRocket_ReturnsNoContent()
        {
            var dto = new RocketDto { Name = "ToBeDeleted", IsActive = true };
            var postRes = await _client.PostAsJsonAsync("/api/rockets", dto);
            var created = (await postRes.Content.ReadFromJsonAsync<Resourсe<RocketDto>>())!.Data;

            var delRes = await _client.DeleteAsync($"/api/rockets/{created.Id}");
            Assert.AreEqual(HttpStatusCode.NoContent, delRes.StatusCode);
        }

        [Test, Order(7)]
        public async Task GetMissionsByRocketId_ReturnsMissions()
        {
            var res = await _client.GetAsync("/api/rockets/1/missions");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

            var wrapper = await res.Content.ReadFromJsonAsync<Resourсe<IEnumerable<MissionDto>>>();
            var missions = wrapper!.Data.ToList();
            Assert.AreEqual(1, missions.Count);
            Assert.AreEqual("TestMission", missions[0].MissionName);
        }

        [Test, Order(8)]
        public async Task GetMissionsByRocketId_NotFound_Returns404()
        {
            var res = await _client.GetAsync("/api/rockets/999/missions");
            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
        }
    }
}
