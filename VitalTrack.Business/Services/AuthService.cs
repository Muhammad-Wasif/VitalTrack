using Microsoft.EntityFrameworkCore;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data;
using VitalTrack.Data.Entities;

namespace VitalTrack.Business.Services;

public class AuthService : IAuthService
{
    private readonly VitalTrackDbContext _db;
    private static string AdminSecretCode =>
        Environment.GetEnvironmentVariable("ADMIN_SECRET_CODE") ?? "VITALTRACK_ADMIN_2025";

    public AuthService(VitalTrackDbContext db) => _db = db;

    public async Task<ServiceResult<UserProfileDto>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return ServiceResult<UserProfileDto>.Fail("Username and password are required.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username.Trim().ToLower());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ServiceResult<UserProfileDto>.Fail("Invalid username or password.");

        return ServiceResult<UserProfileDto>.Ok(MapToDto(user));
    }

    public async Task<ServiceResult<UserProfileDto>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return ServiceResult<UserProfileDto>.Fail("Username is required.");
        if (string.IsNullOrWhiteSpace(request.FullName))
            return ServiceResult<UserProfileDto>.Fail("Full name is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return ServiceResult<UserProfileDto>.Fail("Password must be at least 6 characters.");
        if (request.Age < 10 || request.Age > 120)
            return ServiceResult<UserProfileDto>.Fail("Please enter a valid age (10-120).");

        var normalizedUsername = request.Username.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Username == normalizedUsername))
            return ServiceResult<UserProfileDto>.Fail("That username is already taken.");

        if (request.Role == UserRole.Admin)
        {
            if (string.IsNullOrWhiteSpace(request.AdminSecretCode) ||
                request.AdminSecretCode.Trim() != AdminSecretCode)
                return ServiceResult<UserProfileDto>.Fail("Invalid admin registration code.");
        }

        var user = new User
        {
            Username       = normalizedUsername,
            FullName       = request.FullName.Trim(),
            Email          = request.Email?.Trim() ?? string.Empty,
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Gender         = request.Gender,
            Role           = request.Role,
            Age            = request.Age,
            HeightCm       = request.HeightCm > 0 ? request.HeightCm : 170,
            WeightKg       = request.WeightKg > 0 ? request.WeightKg : 70,
            BodyFatPercent = 0,
            ActivityLevel  = ActivityLevel.ModeratelyActive,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return ServiceResult<UserProfileDto>.Ok(MapToDto(user));
    }

    public static UserProfileDto MapToDto(User u) => new(
        u.Id, u.Username, u.FullName, u.Email,
        u.Gender, u.Role, u.Age, u.HeightCm, u.WeightKg,
        u.BodyFatPercent, u.ActivityLevel
    );
}
