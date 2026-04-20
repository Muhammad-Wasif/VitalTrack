using VitalTrack.Business.DTOs;
using VitalTrack.Data.Entities;

namespace VitalTrack.Business.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<UserProfileDto>> LoginAsync(LoginRequest request);
    Task<ServiceResult<UserProfileDto>> RegisterAsync(RegisterRequest request);
}

public interface IHealthCalculationService
{
    HealthMetricsDto Calculate(Gender gender, double heightCm, double weightKg, int age, ActivityLevel level);
    double CalcCaloriesBurned(string exerciseName, int durationMinutes, double weightKg, Gender gender);
}

public interface IUserService
{
    Task<ServiceResult<UserProfileDto>> GetProfileAsync(int userId);
    Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(int userId, double heightCm, double weightKg, double bodyFat, ActivityLevel level);
    Task<List<AdminUserSummary>> GetAllUsersAsync(); // Admin only
}

public interface IWorkoutService
{
    Task<ServiceResult<WorkoutSessionDto>> LogSessionAsync(int userId, LogWorkoutRequest request);
    Task<List<WorkoutSessionDto>> GetUserSessionsAsync(int userId, DateTime? from = null);
    Task<List<WorkoutSessionDto>> GetAllSessionsAsync(); // Admin only
    Task<bool> DeleteSessionAsync(int sessionId, int requestingUserId, bool isAdmin);
    Task<List<ExerciseApiItem>> GetGenderSuggestionsAsync(Gender gender);
}

public interface INutritionService
{
    Task<ServiceResult<NutritionLogDto>> LogMealAsync(int userId, LogNutritionRequest request);
    Task<DailyNutritionSummary> GetDailySummaryAsync(int userId, DateTime date);
    Task<List<NutritionLogDto>> GetAllLogsAsync(); // Admin only
    Task<bool> DeleteLogAsync(int logId, int requestingUserId, bool isAdmin);
    Task<List<FoodApiItem>> SearchFoodAsync(string query, Gender gender);
}

public interface IChatbotService
{
    Task<ChatResponse> SendMessageAsync(int userId, string message);
    Task<List<ChatMessage>> GetHistoryAsync(int userId, int limit = 20);
}
