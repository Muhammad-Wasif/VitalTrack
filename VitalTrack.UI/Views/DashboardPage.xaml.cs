using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.UI.Views;

public partial class DashboardPage : Page
{
    private readonly UserProfileDto _user;
    private readonly IHealthCalculationService _calc;
    private readonly IWorkoutService _workout;
    private readonly INutritionService _nutrition;

    public DashboardPage(UserProfileDto user)
    {
        InitializeComponent();
        _user      = user;
        _calc      = App.Services.GetRequiredService<IHealthCalculationService>();
        _workout   = App.Services.GetRequiredService<IWorkoutService>();
        _nutrition = App.Services.GetRequiredService<INutritionService>();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var hour     = DateTime.Now.Hour;
        var greeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
        GreetingText.Text = $"{greeting}, {_user.FullName.Split(' ')[0]} 👋";
        DateText.Text     = DateTime.Now.ToString("dddd, MMMM dd yyyy");

        var metrics = _calc.Calculate(_user.Gender, _user.HeightCm, _user.WeightKg, _user.Age, _user.ActivityLevel);

        BmiValue.Text    = metrics.BMI.ToString("F1");
        BmiCategory.Text = metrics.BMICategory;
        BmrValue.Text    = metrics.BMR.ToString("N0");
        TdeeValue.Text   = metrics.TDEE.ToString("N0");

        GenderNoteText.Text = _user.Gender == Gender.Female
            ? "♀ Female macro targets (25% Protein / 50% Carbs / 25% Fat)"
            : "♂ Male macro targets (30% Protein / 45% Carbs / 25% Fat)";

        var daily = await _nutrition.GetDailySummaryAsync(_user.Id, DateTime.Today);
        CaloriesToday.Text   = daily.TotalCalories.ToString("N0");
        CalorieGoalText.Text = $"Goal: {metrics.TDEE:N0} kcal";

        // Progress bar — max 200px wide
        double ratio = metrics.TDEE > 0 ? daily.TotalCalories / metrics.TDEE : 0;
        CalorieProgressFill.Width = Math.Min(200, ratio * 200);

        // Macro text + progress bars
        ProteinVal.Text = $"{daily.TotalProteinG:F0}g / {metrics.ProteinGoalG:F0}g";
        CarbsVal.Text   = $"{daily.TotalCarbsG:F0}g / {metrics.CarbGoalG:F0}g";
        FatVal.Text     = $"{daily.TotalFatG:F0}g / {metrics.FatGoalG:F0}g";

        const double MaxBarW = 160.0;
        ProteinBar.Width = metrics.ProteinGoalG > 0 ? Math.Min(MaxBarW, daily.TotalProteinG / metrics.ProteinGoalG * MaxBarW) : 0;
        CarbsBar.Width   = metrics.CarbGoalG   > 0 ? Math.Min(MaxBarW, daily.TotalCarbsG   / metrics.CarbGoalG   * MaxBarW) : 0;
        FatBar.Width     = metrics.FatGoalG    > 0 ? Math.Min(MaxBarW, daily.TotalFatG     / metrics.FatGoalG    * MaxBarW) : 0;

        // Recent sessions
        var sessions = await _workout.GetUserSessionsAsync(_user.Id, DateTime.Today.AddDays(-7));
        RecentSessionsGrid.ItemsSource = sessions.Take(5).Select(s => new
        {
            s.ExerciseName,
            SetsReps        = $"{s.Sets} × {s.Reps}",
            WeightDisplay   = s.WeightKg > 0 ? $"{s.WeightKg} kg" : "—",
            CaloriesDisplay = $"{s.CaloriesBurned:F0} kcal",
            DateDisplay     = s.LoggedAt.Date == DateTime.Today          ? "Today"
                            : s.LoggedAt.Date == DateTime.Today.AddDays(-1) ? "Yesterday"
                            : s.LoggedAt.ToString("MMM dd")
        }).ToList();
    }

    private void ViewAllWorkouts_Click(object sender, RoutedEventArgs e)
    {
        // Signal parent MainWindow to navigate
        if (Window.GetWindow(this) is MainWindow mw)
            mw.NavigateToPage("Workouts");
    }
}
