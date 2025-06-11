using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceMissions.Infrastructure.Data;

namespace SpaceMissions.WebAP.Tests;

public class TestAuthWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
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
    }
}