namespace VitalTrack.Data.Entities;

public class NutritionLog
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string FoodName { get; set; } = string.Empty;
    public string MealType { get; set; } = "Breakfast"; // Breakfast/Lunch/Dinner/Snack
    public double Calories { get; set; }
    public double ProteinG { get; set; }
    public double CarbsG { get; set; }
    public double FatG { get; set; }
    public double ServingGrams { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
