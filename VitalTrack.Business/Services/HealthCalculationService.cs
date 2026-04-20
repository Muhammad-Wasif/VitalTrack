using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.Business.Services;

/// <summary>
/// All calculations are gender-aware using Mifflin-St Jeor equations.
/// Male BMR   = 10W + 6.25H - 5A + 5
/// Female BMR = 10W + 6.25H - 5A - 161
/// </summary>
public class HealthCalculationService : IHealthCalculationService
{
    private static readonly Dictionary<ActivityLevel, double> ActivityMultipliers = new()
    {
        [ActivityLevel.Sedentary]        = 1.2,
        [ActivityLevel.LightlyActive]    = 1.375,
        [ActivityLevel.ModeratelyActive] = 1.55,
        [ActivityLevel.VeryActive]       = 1.725,
        [ActivityLevel.ExtraActive]      = 1.9,
    };

    public HealthMetricsDto Calculate(Gender gender, double heightCm, double weightKg, int age, ActivityLevel level)
    {
        var bmi    = CalcBMI(weightKg, heightCm);
        var bmr    = CalcBMR(gender, weightKg, heightCm, age);
        var tdee   = Math.Round(bmr * ActivityMultipliers[level]);

        // Gender-specific macro split:
        // Males:   Protein 30% / Carbs 45% / Fat 25%
        // Females: Protein 25% / Carbs 50% / Fat 25%
        double proteinRatio = gender == Gender.Female ? 0.25 : 0.30;
        double carbRatio    = gender == Gender.Female ? 0.50 : 0.45;
        double fatRatio     = 0.25;

        return new HealthMetricsDto(
            BMI:          Math.Round(bmi, 1),
            BMICategory:  GetBMICategory(bmi),
            BMR:          Math.Round(bmr),
            TDEE:         tdee,
            ProteinGoalG: Math.Round((tdee * proteinRatio) / 4, 1), // 4 kcal/g
            CarbGoalG:    Math.Round((tdee * carbRatio)    / 4, 1),
            FatGoalG:     Math.Round((tdee * fatRatio)     / 9, 1)  // 9 kcal/g
        );
    }

    public double CalcCaloriesBurned(string exerciseName, int durationMinutes, double weightKg, Gender gender)
    {
        // MET-based estimation, gender-adjusted (females burn ~5-10% less due to lean mass)
        var metValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["Running"]           = 9.8,
            ["Bench Press"]       = 5.0,
            ["Deadlift"]          = 6.0,
            ["Barbell Squat"]     = 5.5,
            ["HIIT"]              = 10.0,
            ["Cycling"]           = 7.5,
            ["Pull-ups"]          = 5.0,
            ["Hip Thrusts"]       = 4.5,
            ["Romanian Deadlift"] = 5.2,
            ["Cable Rows"]        = 4.0,
            ["Yoga"]              = 3.0,
            ["Walking"]           = 3.5,
        };

        double met = metValues.TryGetValue(exerciseName, out var m) ? m : 5.0;
        double genderFactor = gender == Gender.Female ? 0.93 : 1.0;
        double hours = durationMinutes / 60.0;

        return Math.Round(met * weightKg * hours * genderFactor);
    }

    private static double CalcBMI(double weightKg, double heightCm)
        => weightKg / Math.Pow(heightCm / 100.0, 2);

    private static double CalcBMR(Gender gender, double weightKg, double heightCm, int age)
    {
        double base_ = (10 * weightKg) + (6.25 * heightCm) - (5 * age);
        return gender == Gender.Female ? base_ - 161 : base_ + 5;
    }

    private static string GetBMICategory(double bmi) => bmi switch
    {
        < 18.5 => "Underweight",
        < 25.0 => "Normal",
        < 30.0 => "Overweight",
        _      => "Obese"
    };
}
