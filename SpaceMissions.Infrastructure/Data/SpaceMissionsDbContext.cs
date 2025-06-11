using Microsoft.EntityFrameworkCore;
using SpaceMissions.Core.Entities;

namespace SpaceMissions.Infrastructure.Data;

public class SpaceMissionsDbContext : DbContext
{
    public SpaceMissionsDbContext(DbContextOptions<SpaceMissionsDbContext> options)
        : base(options) { }

    public DbSet<Rocket> Rockets { get; set; }
    public DbSet<Mission> Missions { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rocket>(entity =>
        {
            entity.ToTable("Rockets");
            
            entity.HasKey(r => r.Id);
            
            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(r => r.IsActive)
                .IsRequired();
                
            entity.HasIndex(r => r.Name)
                .IsUnique()
                .HasDatabaseName("IX_Rockets_Name");
            entity.HasMany(r => r.Missions)
                .WithOne(m => m.Rocket)
                .HasForeignKey(m => m.RocketId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Mission>(entity =>
        {
            entity.ToTable("Missions");
            
            entity.HasKey(m => m.Id);
            
            entity.Property(m => m.Company)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(m => m.Location)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(m => m.LaunchDateTime)
                .IsRequired();
                
            entity.Property(m => m.MissionName)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(m => m.MissionStatus)
                .HasMaxLength(50);
                
            entity.Property(m => m.Price)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(null); 
            entity.HasIndex(m => m.RocketId);
            entity.HasIndex(m => m.LaunchDateTime)
                .HasDatabaseName("IX_Missions_LaunchDate"); 
        });
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            
            entity.HasKey(u => u.Id);
            
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Username");
            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasColumnType("bytea"); 
                
            entity.Property(u => u.PasswordSalt)
                .IsRequired()
                .HasColumnType("bytea");
                

            entity.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>()
            .HaveMaxLength(200);
    }
}