using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.UI.Views;

public partial class AdminPage : Page
{
    private readonly UserProfileDto _currentUser = null!;
    private readonly IUserService _userSvc = null!;
    private readonly IWorkoutService _workoutSvc = null!;
    private readonly INutritionService _nutritionSvc = null!;

    public AdminPage(UserProfileDto user)
    {
        InitializeComponent();

        if (user.Role != UserRole.Admin)
        {
            Loaded += (_, _) => NavigationService?.GoBack();
            return;
        }

        _currentUser  = user;
        _userSvc      = App.Services.GetRequiredService<IUserService>();
        _workoutSvc   = App.Services.GetRequiredService<IWorkoutService>();
        _nutritionSvc = App.Services.GetRequiredService<INutritionService>();

        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var users = await _userSvc.GetAllUsersAsync();
        UserCountText.Text    = $"{users.Count} registered users";
        UsersGrid.ItemsSource = users.Select(u => new
        {
            u.Username,
            u.FullName,
            u.Email,
            Gender     = u.Gender.ToString(),
            Role       = u.Role.ToString(),
            u.TotalWorkouts,
            u.TotalNutritionLogs,
            JoinedDate = u.CreatedAt.ToString("MMM dd, yyyy")
        }).ToList();

        var sessions = await _workoutSvc.GetAllSessionsAsync();
        AllSessionsGrid.ItemsSource = sessions.Take(100).Select(s => new
        {
            s.ExerciseName,
            CaloriesDisplay = $"{s.CaloriesBurned:F0} kcal",
            DateDisplay     = s.LoggedAt.ToString("MMM dd")
        }).ToList();

        var logs = await _nutritionSvc.GetAllLogsAsync();
        AllNutritionGrid.ItemsSource = logs.Take(100).Select(n => new
        {
            n.FoodName,
            n.MealType,
            CalDisplay = $"{n.Calories:F0} kcal"
        }).ToList();

        // Summary stats
        TotalSessionsText.Text  = $"{sessions.Count} total sessions";
        TotalNutritionText.Text = $"{logs.Count} total meals logged";
        TotalCalBurned.Text     = $"{sessions.Sum(s => s.CaloriesBurned):N0} kcal burned (all users)";
    }
}
