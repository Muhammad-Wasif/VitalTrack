using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data;
using VitalTrack.Data.Entities;

namespace VitalTrack.Business.Services;

public class WorkoutService : IWorkoutService
{
    private readonly VitalTrackDbContext _db;
    private readonly IHealthCalculationService _calc;
    private readonly HttpClient _http;

    public WorkoutService(VitalTrackDbContext db, IHealthCalculationService calc, IHttpClientFactory factory)
    {
        _db   = db;
        _calc = calc;
        _http = factory.CreateClient("ExerciseDB");
    }

    public async Task<ServiceResult<WorkoutSessionDto>> LogSessionAsync(int userId, LogWorkoutRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ServiceResult<WorkoutSessionDto>.Fail("User not found.");

        double calories = _calc.CalcCaloriesBurned(
            request.ExerciseName, request.DurationMinutes, user.WeightKg, user.Gender);

        var session = new WorkoutSession
        {
            UserId          = userId,
            ExerciseName    = request.ExerciseName,
            MuscleGroup     = request.MuscleGroup,
            Sets            = request.Sets,
            Reps            = request.Reps,
            WeightKg        = request.WeightKg,
            DurationMinutes = request.DurationMinutes,
            CaloriesBurned  = calories,
            Notes           = request.Notes,
            LoggedAt        = DateTime.UtcNow
        };

        _db.WorkoutSessions.Add(session);
        await _db.SaveChangesAsync();

        return ServiceResult<WorkoutSessionDto>.Ok(MapToDto(session));
    }

    public async Task<List<WorkoutSessionDto>> GetUserSessionsAsync(int userId, DateTime? from = null)
    {
        var query = _db.WorkoutSessions.Where(s => s.UserId == userId);
        if (from.HasValue) query = query.Where(s => s.LoggedAt >= from.Value);
        // FIX: Materialize first, then project via MapToDto (EF can't translate instance methods)
        var sessions = await query.OrderByDescending(s => s.LoggedAt).ToListAsync();
        return sessions.Select(MapToDto).ToList();
    }

    public async Task<List<WorkoutSessionDto>> GetAllSessionsAsync()
    {
        var sessions = await _db.WorkoutSessions.OrderByDescending(s => s.LoggedAt).ToListAsync();
        return sessions.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteSessionAsync(int sessionId, int requestingUserId, bool isAdmin)
    {
        var session = await _db.WorkoutSessions.FindAsync(sessionId);
        if (session == null) return false;
        if (!isAdmin && session.UserId != requestingUserId) return false;

        _db.WorkoutSessions.Remove(session);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ExerciseApiItem>> GetGenderSuggestionsAsync(Gender gender)
    {
        // Gender-specific target muscle groups
        // Males:   chest, back, legs, shoulders
        // Females: glutes, hamstrings, core, upper body
        string targetPart = gender == Gender.Female ? "upper%20legs" : "chest";

        try
        {
            // ExerciseDB free API: https://exercisedb.p.rapidapi.com
            // Key read from environment variable: EXERCISEDB_API_KEY
            var exercises = await _http.GetFromJsonAsync<List<ExerciseApiItem>>(
                $"exercises/bodyPart/{targetPart}?limit=8");
            return exercises ?? GetFallbackExercises(gender);
        }
        catch
        {
            return GetFallbackExercises(gender);
        }
    }

    private static List<ExerciseApiItem> GetFallbackExercises(Gender gender)
    {
        if (gender == Gender.Female)
            return new()
            {
                new("Hip Thrusts",         "glutes",     "barbell", "", "glutes"),
                new("Romanian Deadlift",   "hamstrings", "barbell", "", "hamstrings"),
                new("Cable Kickbacks",     "glutes",     "cable",   "", "glutes"),
                new("Leg Press",           "upper legs", "machine", "", "quadriceps"),
                new("Dumbbell Lunges",     "upper legs", "dumbbell","", "quadriceps"),
                new("Lateral Raises",      "shoulders",  "dumbbell","", "delts"),
                new("Seated Cable Row",    "back",       "cable",   "", "lats"),
                new("Steady-State Cardio", "cardio",     "none",    "", "cardiovascular"),
            };

        return new()
        {
            new("Bench Press",   "chest",     "barbell", "", "pectorals"),
            new("Deadlift",      "back",      "barbell", "", "spine"),
            new("Barbell Squat", "upper legs","barbell", "", "quadriceps"),
            new("Pull-ups",      "back",      "body",    "", "lats"),
            new("Overhead Press","shoulders", "barbell", "", "delts"),
            new("Tricep Dips",   "triceps",   "body",    "", "triceps"),
            new("Barbell Curl",  "biceps",    "barbell", "", "biceps"),
            new("HIIT Sprints",  "cardio",    "none",    "", "cardiovascular"),
        };
    }

    private static WorkoutSessionDto MapToDto(WorkoutSession s) => new(
        s.Id, s.ExerciseName, s.MuscleGroup,
        s.Sets, s.Reps, s.WeightKg,
        s.DurationMinutes, s.CaloriesBurned,
        s.Notes, s.LoggedAt
    );
}
