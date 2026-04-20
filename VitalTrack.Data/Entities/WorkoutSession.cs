namespace VitalTrack.Data.Entities;

public class WorkoutSession
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public double WeightKg { get; set; }
    public int DurationMinutes { get; set; }
    public double CaloriesBurned { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
