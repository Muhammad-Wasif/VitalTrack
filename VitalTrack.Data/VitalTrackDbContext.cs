using Microsoft.EntityFrameworkCore;
using VitalTrack.Data.Entities;

namespace VitalTrack.Data;

public class VitalTrackDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<WorkoutSession> WorkoutSessions { get; set; }
    public DbSet<NutritionLog> NutritionLogs { get; set; }
    public DbSet<ChatLog> ChatLogs { get; set; }

    public VitalTrackDbContext(DbContextOptions<VitalTrackDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User configuration
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150);
            e.Property(u => u.Gender).HasConversion<string>();
            e.Property(u => u.Role).HasConversion<string>();
            e.Property(u => u.ActivityLevel).HasConversion<string>();
        });

        // WorkoutSession → User (Many-to-One)
        modelBuilder.Entity<WorkoutSession>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.ExerciseName).HasMaxLength(100).IsRequired();
            e.HasOne(w => w.User)
             .WithMany(u => u.WorkoutSessions)
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // NutritionLog → User (Many-to-One)
        modelBuilder.Entity<NutritionLog>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.FoodName).HasMaxLength(150).IsRequired();
            e.Property(n => n.MealType).HasMaxLength(20);
            e.HasOne(n => n.User)
             .WithMany(u => u.NutritionLogs)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatLog → User (Many-to-One)
        modelBuilder.Entity<ChatLog>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.User)
             .WithMany(u => u.ChatLogs)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed default admin — FIXED: use a pre-computed static BCrypt hash.
        // Hash of "Admin@2025" with work factor 11. Regenerate with:
        //   BCrypt.Net.BCrypt.HashPassword("Admin@2025", workFactor: 11)
        const string adminHash = "$2a$11$K7GQTwq5HZphRK6bRYFh3.Qa5EqT8YHN2a1dEpRmzXN6aZ9uT5jOa";
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            FullName = "System Administrator",
            PasswordHash = adminHash,
            Email = "admin@vitaltrack.com",
            Gender = Gender.Male,
            Role = UserRole.Admin,
            Age = 30,
            HeightCm = 175,
            WeightKg = 75,
            BodyFatPercent = 15,
            ActivityLevel = ActivityLevel.ModeratelyActive,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
