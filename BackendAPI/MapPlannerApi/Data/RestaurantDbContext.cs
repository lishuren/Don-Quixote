using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Data;

/// <summary>
/// Entity Framework Core database context for the restaurant robot system.
/// </summary>
public class RestaurantDbContext : DbContext
{
    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<RobotTask> Tasks => Set<RobotTask>();
    public DbSet<TableEntity> Tables => Set<TableEntity>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Robot configuration
        modelBuilder.Entity<Robot>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.OwnsOne(e => e.Position);
            
            entity.HasOne(e => e.CurrentTask)
                .WithOne()
                .HasForeignKey<Robot>(e => e.CurrentTaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // RobotTask configuration
        modelBuilder.Entity<RobotTask>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.OwnsOne(e => e.StartPosition);
            entity.OwnsOne(e => e.TargetPosition);

            entity.HasOne(e => e.Robot)
                .WithMany(r => r.Tasks)
                .HasForeignKey(e => e.RobotId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetTable)
                .WithMany()
                .HasForeignKey(e => e.TargetTableId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // TableEntity configuration
        modelBuilder.Entity<TableEntity>(entity =>
        {
            entity.ToTable("Tables");
            entity.HasIndex(e => e.Status);
            entity.OwnsOne(e => e.Center);

            entity.HasOne(e => e.Zone)
                .WithMany(z => z.Tables)
                .HasForeignKey(e => e.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Guest configuration
        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.QueuePosition);

            entity.HasOne(e => e.Table)
                .WithMany(t => t.Guests)
                .HasForeignKey(e => e.TableId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Reservation)
                .WithOne(r => r.Guest)
                .HasForeignKey<Guest>(e => e.ReservationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Zone configuration
        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
        });

        // Checkpoint configuration
        modelBuilder.Entity<Checkpoint>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.OwnsOne(e => e.Position);

            entity.HasOne(e => e.Zone)
                .WithMany(z => z.Checkpoints)
                .HasForeignKey(e => e.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Alert configuration
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.IsAcknowledged);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Robot)
                .WithMany()
                .HasForeignKey(e => e.RobotId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Table)
                .WithMany()
                .HasForeignKey(e => e.TableId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Reservation configuration
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasIndex(e => e.ReservationTime);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ConfirmationCode).IsUnique();

            entity.HasOne(e => e.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(e => e.TableId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SystemConfig configuration
        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.Key);
        });

        // Seed default configuration
        SeedDefaultConfig(modelBuilder);
    }

    private static void SeedDefaultConfig(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemConfig>().HasData(
            new SystemConfig { Key = ConfigKeys.PixelsPerMeter, Value = "100", ValueType = "double", Description = "Map scale: pixels per meter" },
            new SystemConfig { Key = ConfigKeys.DefaultRobotSpeed, Value = "0.5", ValueType = "double", Description = "Default robot speed in m/s" },
            new SystemConfig { Key = ConfigKeys.LowBatteryThreshold, Value = "20", ValueType = "int", Description = "Low battery alert threshold %" },
            new SystemConfig { Key = ConfigKeys.MaxConcurrentTasks, Value = "3", ValueType = "int", Description = "Max concurrent tasks per robot" },
            new SystemConfig { Key = ConfigKeys.TaskRetryLimit, Value = "3", ValueType = "int", Description = "Task retry limit before failure" },
            new SystemConfig { Key = ConfigKeys.AlertRetentionDays, Value = "30", ValueType = "int", Description = "Days to retain resolved alerts" },
            new SystemConfig { Key = ConfigKeys.ReservationLeadTimeMinutes, Value = "15", ValueType = "int", Description = "Minutes before reservation to mark table" },
            new SystemConfig { Key = ConfigKeys.WaitlistEnabled, Value = "true", ValueType = "bool", Description = "Enable guest waitlist feature" }
        );
    }
}
