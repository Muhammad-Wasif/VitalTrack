using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.UI.Views;

public partial class NutritionPage : Page
{
    private readonly UserProfileDto _user;
    private readonly INutritionService _nutritionSvc;
    private readonly IHealthCalculationService _calc;
    private NutritionLogDto? _selectedLog;

    public NutritionPage(UserProfileDto user)
    {
        InitializeComponent();
        _user         = user;
        _nutritionSvc = App.Services.GetRequiredService<INutritionService>();
        _calc         = App.Services.GetRequiredService<IHealthCalculationService>();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        var summary = await _nutritionSvc.GetDailySummaryAsync(_user.Id, DateTime.Today);
        var metrics = _calc.Calculate(_user.Gender, _user.HeightCm, _user.WeightKg, _user.Age, _user.ActivityLevel);

        ConsumedKcal.Text  = summary.TotalCalories.ToString("N0");
        TotalProtein.Text  = $"{summary.TotalProteinG:F0}g";
        CalorieGoal.Text   = metrics.TDEE.ToString("N0");
        RemainingKcal.Text = Math.Max(0, metrics.TDEE - summary.TotalCalories).ToString("N0");

        if (GenderDietTag.Child is TextBlock genderTb)
            genderTb.Text = _user.Gender == Gender.Female ? "♀ Female Macros" : "♂ Male Macros";

        NutritionGrid.ItemsSource = summary.Entries.Select(n => new
        {
            Id          = n.Id,
            n.FoodName,
            n.MealType,
            CalDisplay  = $"{n.Calories:F0} kcal",
            ProtDisplay = $"{n.ProteinG:F1}g",
            CarbDisplay = $"{n.CarbsG:F1}g",
            FatDisplay  = $"{n.FatG:F1}g",
            ServDisplay = $"{n.ServingGrams:F0}g"
        }).ToList();
    }

    private void LogMeal_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LogMealDialog(_user);
        dialog.Owner = Window.GetWindow(this);
        dialog.ShowDialog();
        _ = LoadDataAsync();
    }

    private async void DeleteMeal_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int logId)
        {
            var ok = await _nutritionSvc.DeleteLogAsync(logId, _user.Id, _user.Role == UserRole.Admin);
            if (ok) await LoadDataAsync();
        }
    }
}
