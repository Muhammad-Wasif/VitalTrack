using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.UI.Views;

public partial class WorkoutsPage : Page
{
    private readonly UserProfileDto _user;
    private readonly IWorkoutService _workoutSvc;

    public WorkoutsPage(UserProfileDto user)
    {
        InitializeComponent();
        _user       = user;
        _workoutSvc = App.Services.GetRequiredService<IWorkoutService>();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        // FIX #5: Directly find the TextBlock child of GenderExTag Border
        if (GenderExTag.Child is TextBlock genderTb)
            genderTb.Text = _user.Gender == Gender.Female ? "♀ Female Program" : "♂ Male Program";

        // Load gender-specific exercise suggestions
        var suggestions = await _workoutSvc.GetGenderSuggestionsAsync(_user.Gender);
        ExerciseSuggestionsGrid.ItemsSource = suggestions;

        // Load workout session history
        await RefreshSessionsGrid();
    }

    private async Task RefreshSessionsGrid()
    {
        var sessions = await _workoutSvc.GetUserSessionsAsync(_user.Id);
        SessionsGrid.ItemsSource = sessions.Select(s => new
        {
            DateDisplay     = s.LoggedAt.Date == DateTime.Today ? "Today"
                            : s.LoggedAt.Date == DateTime.Today.AddDays(-1) ? "Yesterday"
                            : s.LoggedAt.ToString("MMM dd"),
            s.ExerciseName,
            s.MuscleGroup,
            s.Sets,
            s.Reps,
            WeightDisplay   = s.WeightKg > 0 ? $"{s.WeightKg} kg" : "—",
            DurationDisplay = $"{s.DurationMinutes} min",
            CaloriesDisplay = $"{s.CaloriesBurned:F0} kcal"
        }).ToList();
    }

    // FIX #18: Card click pre-fills the Log Session dialog with the exercise name
    private void ExerciseCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string exerciseName)
        {
            var dialog = new LogWorkoutDialog(_user, exerciseName);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
            _ = RefreshSessionsGrid();
        }
    }

    // FIX #6: Proper reload after dialog closes
    private async void LogSession_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LogWorkoutDialog(_user);
        dialog.Owner = Window.GetWindow(this);
        dialog.ShowDialog();           // result doesn't matter — always refresh
        await RefreshSessionsGrid();   // FIX: direct async call, not the broken Loaded hack
    }
}
