using System;
using System.Linq;
using System.Net;
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
    // Fake auth handler to bypass [Authorize]
    public class MissionsTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public MissionsTestAuthHandler(
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
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, "Test")));
        }
    }

    [TestFixture]
    public class MissionsControllerTests
    {
        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;

        [OneTimeSetUp]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Replace DbContext with InMemory
                        var descriptor = services.Single(d =>
                            d.ServiceType == typeof(DbContextOptions<SpaceMissionsDbContext>));
                        services.Remove(descriptor);
                        services.AddDbContext<SpaceMissionsDbContext>(opts =>
                            opts.UseInMemoryDatabase("TestDb_Missions"));

                        // Fake auth
                        services.AddAuthentication("Test")
                            .AddScheme<AuthenticationSchemeOptions, MissionsTestAuthHandler>(
                                "Test", _ => { });
                        services.PostConfigure<AuthenticationOptions>(opts =>
                            opts.DefaultScheme = "Test");

                        // Seed data
                        var sp = services.BuildServiceProvider();
                        using var scope = sp.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<SpaceMissionsDbContext>();
                        db.Database.EnsureDeleted();
                        db.Database.EnsureCreated();

                        var rocket = new Rocket { Id = 1, Name = "Falcon 9", IsActive = true };
                        db.Rockets.Add(rocket);
                        db.Missions.Add(new Mission
                        {
                            Id = 1,
                            MissionName = "Test Launch",
                            Company = "SpaceX",
                            Location = "Cape Canaveral",
                            LaunchDateTime = DateTime.UtcNow.AddDays(-1),
                            MissionStatus = "Success",
                            Price = 50000000,
                            Rocket = rocket
                        });
                        db.SaveChanges();
                    });
                });

            _client = _factory.CreateClient();
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Test, Order(1)]
        public async Task GetMissions_Default_ReturnsOne()
        {
            var res = await _client.GetAsync("/api/missions");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

            var wrapper = await res.Content.ReadFromJsonAsync<Resourсe<PaginatedResponseDto<MissionListItemDto>>>();
            Assert.NotNull(wrapper);
            Assert.AreEqual(1, wrapper!.Data.Items.Count);
        }

        [Test, Order(2)]
        public async Task GetMissions_FilterByCompany_ReturnsOnlySpaceX()
        {
            var res = await _client.GetAsync("/api/missions?Company=SpaceX");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

            var w = await res.Content.ReadFromJsonAsync<Resourсe<PaginatedResponseDto<MissionListItemDto>>>();
            Assert.NotNull(w);
            Assert.True(w!.Data.Items.All(m => m.Company == "SpaceX"));
        }

        [Test, Order(3)]
        public async Task GetMissions_FilterByStatus_ReturnsOnlySuccess()
        {
            var res = await _client.GetAsync("/api/missions?MissionStatus=Success");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

            var w = await res.Content.ReadFromJsonAsync<Resourсe<PaginatedResponseDto<MissionListItemDto>>>();
            Assert.NotNull(w);
            Assert.True(w!.Data.Items.All(m => m.MissionStatus == "Success"));
        }

        [Test, Order(4)]
        public async Task GetMissions_FilterByDateRange_ReturnsWithinDates()
        {
            var start = DateTime.UtcNow.AddDays(-2).ToString("o");
            var end = DateTime.UtcNow.ToString("o");
            var res = await _client.GetAsync($"/api/missions?StartDate={start}&EndDate={end}");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

            var w = await res.Content.ReadFromJsonAsync<Resourсe<PaginatedResponseDto<MissionListItemDto>>>();
            Assert.NotNull(w);
            Assert.True(w!.Data.Items.All(m =>
                m.LaunchDateTime >= DateTime.Parse(start).ToUniversalTime() &&
                m.LaunchDateTime <= DateTime.Parse(end).ToUniversalTime()));
        }

        [Test, Order(5)]
        public async Task GetMissions_SortAndPaginate_Works()
        {
            // seed more
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SpaceMissionsDbContext>();
                for (int i = 2; i <= 12; i++)
                {
                    db.Missions.Add(new Mission
                    {
                        MissionName = $"M{i}",
                        Company = i % 2 == 0 ? "A" : "B",
                        Location = "X",
                        LaunchDateTime = DateTime.UtcNow,
                        MissionStatus = "Planned",
                        Price = 1000,
                        RocketId = 1
                    });
                }
                await db.SaveChangesAsync();
            }

            var p2 = await _client.GetAsync("/api/missions?pageNumber=2&pageSize=5");
            Assert.AreEqual(HttpStatusCode.OK, p2.StatusCode);
            var wp2 = await p2.Content.ReadFromJsonAsync<Resourсe<PaginatedResponseDto<MissionListItemDto>>>();
            Assert.NotNull(wp2);
            Assert.AreEqual(2, wp2!.Data.PageNumber);
            Assert.AreEqual(5, wp2.Data.Items.Count);

            var sort = await _client.GetAsync("/api/missions?SortBy=company&SortDescending=true");
            Assert.AreEqual(HttpStatusCode.OK, sort.StatusCode);
            var ws = await sort.Content.ReadFromJsonAsync<Resourсe<PaginatedResponseDto<MissionListItemDto>>>();
            Assert.NotNull(ws);
            var items = ws!.Data.Items;
            Assert.AreEqual(items, items.OrderByDescending(m => m.Company).ToList());
        }

        [Test, Order(6)]
        public async Task GetMissionById_Existing_ReturnsOk()
        {
            var res = await _client.GetAsync("/api/missions/1");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            var w = await res.Content.ReadFromJsonAsync<Resourсe<MissionDto>>();
            Assert.NotNull(w);
            Assert.AreEqual(1, w!.Data.Id);
        }

        [Test, Order(7)]
        public async Task GetMissionById_NotExists_Returns404()
        {
            var res = await _client.GetAsync("/api/missions/999");
            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Test, Order(8)]
        public async Task Create_Update_Delete_Mission_Workflow()
        {
            // Create
            var create = new MissionDto
            {
                MissionName = "X",
                Company = "Y",
                Location = "Z",
                LaunchDateTime = DateTime.UtcNow,
                MissionStatus = "Planned",
                Price = 1,
                RocketId = 1,
                RocketName = "Falcon 9"
            };
            var pc = await _client.PostAsJsonAsync("/api/missions", create);
            Assert.AreEqual(HttpStatusCode.Created, pc.StatusCode);
            var wc = await pc.Content.ReadFromJsonAsync<Resourсe<MissionDto>>();
            Assert.NotNull(wc);
            var mid = wc!.Data.Id;

            // Update
            wc.Data.MissionName = "X2";
            wc.Data.RocketName = "Falcon 9";
            var pu = await _client.PutAsJsonAsync($"/api/missions/{mid}", wc.Data);
            Assert.AreEqual(HttpStatusCode.NoContent, pu.StatusCode);

            // Delete
            var pd = await _client.DeleteAsync($"/api/missions/{mid}");
            Assert.AreEqual(HttpStatusCode.NoContent, pd.StatusCode);
        }

        [Test, Order(9)]
        public async Task GetRocketByMissionId_Exists_ReturnsOk()
        {
            var res = await _client.GetAsync("/api/missions/1/rocket");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            var w = await res.Content.ReadFromJsonAsync<Resourсe<RocketDto>>();
            Assert.NotNull(w);
            Assert.AreEqual("Falcon 9", w!.Data.Name);
        }

        [Test, Order(10)]
        public async Task GetRocketByMissionId_NotExists_Returns404()
        {
            var res = await _client.GetAsync("/api/missions/999/rocket");
            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
        }
    }
}
