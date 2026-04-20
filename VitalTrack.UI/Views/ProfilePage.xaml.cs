using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.UI.Views;

public partial class ProfilePage : Page
{
    private readonly UserProfileDto _user;
    private readonly IHealthCalculationService _calc;
    private readonly IUserService _userSvc;
    private bool _suppressUpdate;

    public ProfilePage(UserProfileDto user)
    {
        InitializeComponent();
        _user    = user;
        _calc    = App.Services.GetRequiredService<IHealthCalculationService>();
        _userSvc = App.Services.GetRequiredService<IUserService>();
        LoadProfile();
    }

    private void LoadProfile()
    {
        _suppressUpdate = true;

        var initials = string.Concat(_user.FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(w => w[0]));

        HeroAvatar.Text   = initials.ToUpper();
        HeroFullName.Text = _user.FullName;
        HeroUsername.Text = $"@{_user.Username}";
        HeroGender.Text   = _user.Gender switch
        {
            Gender.Female => "♀ Female",
            Gender.Male   => "♂ Male",
            _             => "⚧ Other"
        };
        HeroRole.Text = _user.Role.ToString();

        HeightBox.Text        = _user.HeightCm.ToString("F0");
        WeightBox.Text        = _user.WeightKg.ToString("F1");
        BodyFatBox.Text       = _user.BodyFatPercent.ToString("F1");
        AgeBox.Text           = _user.Age.ToString();
        ActivityBox.SelectedIndex = (int)_user.ActivityLevel;

        _suppressUpdate = false;
        UpdateMetrics();
    }

    private void PhysicalStats_Changed(object sender, RoutedEventArgs e)
    {
        if (!_suppressUpdate) UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        if (!double.TryParse(HeightBox?.Text, out double h) || h <= 0)   return;
        if (!double.TryParse(WeightBox?.Text, out double w) || w <= 0)   return;
        if (!int.TryParse(AgeBox?.Text,       out int age) || age <= 0)  return;

        var level = (ActivityLevel)(ActivityBox?.SelectedIndex ?? 2);
        var m     = _calc.Calculate(_user.Gender, h, w, age, level);

        // Hero quick-metrics
        QuickBmi.Text  = m.BMI.ToString("F1");
        QuickBmr.Text  = $"{m.BMR:N0} kcal";
        QuickTdee.Text = $"{m.TDEE:N0} kcal";

        // Detailed cards
        CalcBmi.Text    = m.BMI.ToString("F1");
        CalcBmr.Text    = $"{m.BMR:N0} kcal";
        CalcTdee.Text   = $"{m.TDEE:N0} kcal";
        BmiCatText.Text = $"{m.BMICategory}  (18.5 – 24.9 = Normal)";

        // Color BMI value by category
        CalcBmi.Foreground = m.BMICategory switch
        {
            "Normal"      => new SolidColorBrush(Color.FromRgb(0x00, 0xD4, 0xB4)),
            "Underweight" => new SolidColorBrush(Color.FromRgb(0xFF, 0xC9, 0x4D)),
            "Overweight"  => new SolidColorBrush(Color.FromRgb(0xFF, 0x80, 0x00)),
            _             => new SolidColorBrush(Color.FromRgb(0xFF, 0x5C, 0x5C))
        };

        // BMI bar (0–40 → 0–220px)
        BmiBar.Width = Math.Clamp(m.BMI / 40.0, 0, 1) * 220;

        // Macro goals display
        ProteinGoal.Text = $"{m.ProteinGoalG:F0}g / day";
        CarbGoal.Text    = $"{m.CarbGoalG:F0}g / day";
        FatGoal.Text     = $"{m.FatGoalG:F0}g / day";

        FormulaNote.Text = _user.Gender == Gender.Female
            ? "♀ Female BMR = 10×W + 6.25×H − 5×Age − 161  (Mifflin-St Jeor)"
            : "♂ Male BMR   = 10×W + 6.25×H − 5×Age + 5    (Mifflin-St Jeor)";
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(HeightBox.Text, out double h) || h <= 0 ||
            !double.TryParse(WeightBox.Text, out double w) || w <= 0 ||
            !double.TryParse(BodyFatBox.Text, out double bf) || bf < 0)
        {
            SaveStatusText.Foreground = Brushes.OrangeRed;
            SaveStatusText.Text       = "⚠ Invalid values — height and weight must be positive.";
            return;
        }

        var level  = (ActivityLevel)ActivityBox.SelectedIndex;
        var result = await _userSvc.UpdateProfileAsync(_user.Id, h, w, bf, level);

        if (result.Success)
        {
            SaveStatusText.Foreground = Brushes.MediumAquamarine;
            SaveStatusText.Text       = "✓ Profile saved — metrics recalculated!";
        }
        else
        {
            SaveStatusText.Foreground = Brushes.OrangeRed;
            SaveStatusText.Text       = $"⚠ {result.ErrorMessage}";
        }
    }
}
