using Domain;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<SensorType> SensorTypes => Set<SensorType>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Actuator> Actuators => Set<Actuator>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<DeviceConfiguration> DeviceConfigurations => Set<DeviceConfiguration>();
    public DbSet<ActuatorCommand> ActuatorCommands => Set<ActuatorCommand>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Device>()
                .HasIndex(d => d.HardwareId)
                .IsUnique();

        modelBuilder.Entity<Sensor>()
                .HasIndex(s => new { s.DeviceId, s.Channel })
                .IsUnique();

        modelBuilder.Entity<Actuator>()
                .HasIndex(s => new { s.DeviceId, s.Channel })
                .IsUnique();

        modelBuilder.Entity<SensorType>()
                .HasIndex(s => s.Code)
                .IsUnique();

        modelBuilder.Entity<Measurement>()
                .HasKey(s => new { s.SensorId, s.Time });

        modelBuilder.Entity<DeviceConfiguration>()
                .Property(c => c.Payload)
                .HasColumnType("jsonb");

        modelBuilder.Entity<ActuatorCommand>()
                .Property(c => c.Payload)
                .HasColumnType("jsonb");

        modelBuilder.Entity<Measurement>()
                .Property(c => c.Time)
                .HasColumnType("timestamptz");

        modelBuilder.Entity<Measurement>()
                .Property(c => c.Value)
                .HasColumnType("double precision");

        modelBuilder.Entity<Measurement>()
                .Property(c => c.Quality)
                .HasColumnType("smallint");
    }
}
