using VitalTrack.Data.Entities;

namespace VitalTrack.Business.DTOs;

// Auth DTOs
public record LoginRequest(string Username, string Password);
public record RegisterRequest(
    string Username,
    string FullName,
    string Email,
    string Password,
    Gender Gender,
    UserRole Role,
    int Age,
    double HeightCm,
    double WeightKg,
    string? AdminSecretCode = null
);

// Profile & Metrics
public record UserProfileDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    Gender Gender,
    UserRole Role,
    int Age,
    double HeightCm,
    double WeightKg,
    double BodyFatPercent,
    ActivityLevel ActivityLevel
);

public record HealthMetricsDto(
    double BMI,
    string BMICategory,
    double BMR,
    double TDEE,
    double ProteinGoalG,
    double CarbGoalG,
    double FatGoalG
);

// Workout DTOs
public record LogWorkoutRequest(
    string ExerciseName,
    string MuscleGroup,
    int Sets,
    int Reps,
    double WeightKg,
    int DurationMinutes,
    string Notes = ""
);

public record WorkoutSessionDto(
    int Id,
    string ExerciseName,
    string MuscleGroup,
    int Sets,
    int Reps,
    double WeightKg,
    int DurationMinutes,
    double CaloriesBurned,
    string Notes,
    DateTime LoggedAt
);

// Nutrition DTOs
public record LogNutritionRequest(
    string FoodName,
    string MealType,
    double Calories,
    double ProteinG,
    double CarbsG,
    double FatG,
    double ServingGrams
);

public record NutritionLogDto(
    int Id,
    string FoodName,
    string MealType,
    double Calories,
    double ProteinG,
    double CarbsG,
    double FatG,
    double ServingGrams,
    DateTime LoggedAt
);

public record DailyNutritionSummary(
    double TotalCalories,
    double TotalProteinG,
    double TotalCarbsG,
    double TotalFatG,
    List<NutritionLogDto> Entries
);

// Exercise API response model
public record ExerciseApiItem(
    string Name,
    string BodyPart,
    string Equipment,
    string GifUrl,
    string Target
);

// Food API response model
public record FoodApiItem(
    string FoodName,
    double CaloriesPer100g,
    double ProteinPer100g,
    double CarbsPer100g,
    double FatPer100g
);

// Chat
public record ChatMessage(string Role, string Content);
public record ChatResponse(string Reply, bool IsError = false);

// Admin
public record AdminUserSummary(
    int Id,
    string Username,
    string FullName,
    string Email,
    Gender Gender,
    UserRole Role,
    int TotalWorkouts,
    int TotalNutritionLogs,
    DateTime CreatedAt
);

// Service result wrapper
public record ServiceResult<T>(bool Success, T? Data, string? ErrorMessage = null)
{
    public static ServiceResult<T> Ok(T data) => new(true, data);
    public static ServiceResult<T> Fail(string error) => new(false, default, error);
}
