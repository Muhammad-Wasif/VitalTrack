using Microsoft.EntityFrameworkCore;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data;
using VitalTrack.Data.Entities;

namespace VitalTrack.Business.Services;

public class UserService : IUserService
{
    private readonly VitalTrackDbContext _db;

    public UserService(VitalTrackDbContext db) => _db = db;

    public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ServiceResult<UserProfileDto>.Fail("User not found.");
        return ServiceResult<UserProfileDto>.Ok(MapToDto(user));
    }

    public async Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(int userId, double heightCm, double weightKg, double bodyFat, ActivityLevel level)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ServiceResult<UserProfileDto>.Fail("User not found.");

        user.HeightCm      = heightCm;
        user.WeightKg      = weightKg;
        user.BodyFatPercent = bodyFat;
        user.ActivityLevel  = level;
        user.UpdatedAt      = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult<UserProfileDto>.Ok(MapToDto(user));
    }

    public async Task<List<AdminUserSummary>> GetAllUsersAsync()
    {
        // FIX: Materialize entities first; then project in-memory to avoid EF translation error
        var users = await _db.Users
            .Include(u => u.WorkoutSessions)
            .Include(u => u.NutritionLogs)
            .ToListAsync();

        return users.Select(u => new AdminUserSummary(
            u.Id, u.Username, u.FullName, u.Email, u.Gender, u.Role,
            u.WorkoutSessions.Count,
            u.NutritionLogs.Count,
            u.CreatedAt
        )).ToList();
    }

    private static UserProfileDto MapToDto(User u) => new(
        u.Id, u.Username, u.FullName, u.Email,
        u.Gender, u.Role, u.Age, u.HeightCm, u.WeightKg,
        u.BodyFatPercent, u.ActivityLevel
    );
}
