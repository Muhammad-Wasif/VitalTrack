namespace VitalTrack.Data.Entities;

public class User
{
    public int Id { get; set; }

    // Separate Username (login ID) and FullName (display name)
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Gender is CRITICAL — drives all BMR/TDEE/nutrition calculations
    public Gender Gender { get; set; } = Gender.Male;
    public UserRole Role { get; set; } = UserRole.StandardUser;

    public int Age { get; set; }
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public double BodyFatPercent { get; set; }
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.ModeratelyActive;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
    public ICollection<NutritionLog> NutritionLogs { get; set; } = new List<NutritionLog>();
    public ICollection<ChatLog> ChatLogs { get; set; } = new List<ChatLog>();
}

public enum Gender { Male, Female, Other }

public enum UserRole { StandardUser, Admin }

public enum ActivityLevel
{
    Sedentary = 0,
    LightlyActive = 1,
    ModeratelyActive = 2,
    VeryActive = 3,
    ExtraActive = 4
}
