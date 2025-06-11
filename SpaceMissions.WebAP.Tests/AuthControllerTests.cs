using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SpaceMissions.Infrastructure.Data;
using SpaceMissions.WebAP.DTOs;

namespace SpaceMissions.WebAP.Tests;

[TestFixture]
public class AuthControllerTests
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
                    // Убираем регистрацию реального DbContext
                    var descriptor = services.Single(d =>
                        d.ServiceType == typeof(DbContextOptions<SpaceMissionsDbContext>));
                    services.Remove(descriptor);

                    // Регистрируем InMemory БД
                    services.AddDbContext<SpaceMissionsDbContext>(opts =>
                        opts.UseInMemoryDatabase("TestDb"));

                    // Инициализируем (чистая база каждый запуск)
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<SpaceMissionsDbContext>();
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();
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

    private const string TestUsername = "user1";
    private const string TestPassword = "Pass@123";

    [Test, Order(1)]
    public void Register_NewUser_ReturnsOk()
    {
        var dto = new UserLoginDto { Username = TestUsername, Password = TestPassword };
        var resp = _client!.PostAsJsonAsync("/api/auth/register", dto).Result;

        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        var text = resp.Content.ReadAsStringAsync().Result;
        StringAssert.Contains("Пользователь зарегистрирован", text);
    }

    [Test, Order(2)]
    public void Register_SameUser_ReturnsBadRequest()
    {
        var dto = new UserLoginDto { Username = TestUsername, Password = TestPassword };
        var resp = _client!.PostAsJsonAsync("/api/auth/register", dto).Result;

        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Test, Order(3)]
    public void Login_Valid_ReturnsToken()
    {
        var dto = new UserLoginDto { Username = TestUsername, Password = TestPassword };
        var resp = _client!.PostAsJsonAsync("/api/auth/login", dto).Result;

        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        var json = resp.Content.ReadAsStringAsync().Result;
        StringAssert.Contains("token", json);
    }

    [Test, Order(4)]
    public void Login_Invalid_ReturnsUnauthorized()
    {
        var dto = new UserLoginDto { Username = TestUsername, Password = "WrongPass" };
        var resp = _client!.PostAsJsonAsync("/api/auth/login", dto).Result;

        Assert.AreEqual(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
