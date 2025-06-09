using Microsoft.EntityFrameworkCore;
using SpaceMissions.Core.Entities;

namespace SpaceMissions.Infrastructure.Data;

public class SpaceMissionsDbContext : DbContext
{
    public SpaceMissionsDbContext(DbContextOptions<SpaceMissionsDbContext> options)
        : base(options) { }

    public DbSet<Rocket> Rockets { get; set; } = null!;
    public DbSet<Mission> Missions { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rocket>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.IsActive).IsRequired();
        });

        modelBuilder.Entity<Mission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Company).IsRequired().HasMaxLength(100);
            e.Property(x => x.Location).IsRequired().HasMaxLength(200);
            e.Property(x => x.LaunchDateTime).IsRequired();
            e.Property(x => x.RocketName).IsRequired().HasMaxLength(100);
            e.Property(x => x.MissionName).IsRequired().HasMaxLength(200);
            e.Property(x => x.RocketStatus).HasMaxLength(50);
            e.Property(x => x.MissionStatus).HasMaxLength(50);
            e.Property(x => x.Price).HasPrecision(18, 2);

            e.HasOne(m => m.Rocket)
                .WithMany(r => r.Missions)
                .HasForeignKey(m => m.RocketId)
                .OnDelete(DeleteBehavior.SetNull); // если ракета удаляется, миссии сохраняются
        });
        
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Username).IsRequired().HasMaxLength(100);
            e.Property(u => u.PasswordHash).IsRequired().HasColumnType("bytea"); 
            e.Property(u => u.PasswordSalt).IsRequired().HasColumnType("bytea");
        });

    }
}