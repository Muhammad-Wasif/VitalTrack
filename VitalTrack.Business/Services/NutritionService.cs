using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data;
using VitalTrack.Data.Entities;

namespace VitalTrack.Business.Services;

public class NutritionService : INutritionService
{
    private readonly VitalTrackDbContext _db;
    private readonly HttpClient _http;

    public NutritionService(VitalTrackDbContext db, IHttpClientFactory factory)
    {
        _db   = db;
        _http = factory.CreateClient("OpenFoodFacts");
    }

    public async Task<ServiceResult<NutritionLogDto>> LogMealAsync(int userId, LogNutritionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FoodName))
            return ServiceResult<NutritionLogDto>.Fail("Food name is required.");
        if (request.Calories < 0)
            return ServiceResult<NutritionLogDto>.Fail("Calories cannot be negative.");

        var log = new NutritionLog
        {
            UserId       = userId,
            FoodName     = request.FoodName.Trim(),
            MealType     = request.MealType,
            Calories     = Math.Round(request.Calories, 1),
            ProteinG     = Math.Round(request.ProteinG, 1),
            CarbsG       = Math.Round(request.CarbsG, 1),
            FatG         = Math.Round(request.FatG, 1),
            ServingGrams = Math.Round(request.ServingGrams, 1),
            LoggedAt     = DateTime.UtcNow
        };

        _db.NutritionLogs.Add(log);
        await _db.SaveChangesAsync();
        return ServiceResult<NutritionLogDto>.Ok(MapToDto(log));
    }

    public async Task<DailyNutritionSummary> GetDailySummaryAsync(int userId, DateTime date)
    {
        var start   = date.Date;
        var end     = start.AddDays(1);
        var entries = await _db.NutritionLogs
            .Where(n => n.UserId == userId && n.LoggedAt >= start && n.LoggedAt < end)
            .OrderBy(n => n.LoggedAt)
            .ToListAsync();

        return new DailyNutritionSummary(
            TotalCalories: Math.Round(entries.Sum(e => e.Calories), 1),
            TotalProteinG: Math.Round(entries.Sum(e => e.ProteinG), 1),
            TotalCarbsG:   Math.Round(entries.Sum(e => e.CarbsG), 1),
            TotalFatG:     Math.Round(entries.Sum(e => e.FatG), 1),
            Entries:       entries.Select(MapToDto).ToList()
        );
    }

    public async Task<List<NutritionLogDto>> GetAllLogsAsync()
    {
        var logs = await _db.NutritionLogs.OrderByDescending(n => n.LoggedAt).ToListAsync();
        return logs.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteLogAsync(int logId, int requestingUserId, bool isAdmin)
    {
        var log = await _db.NutritionLogs.FindAsync(logId);
        if (log == null) return false;
        if (!isAdmin && log.UserId != requestingUserId) return false;

        _db.NutritionLogs.Remove(log);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<FoodApiItem>> SearchFoodAsync(string query, Gender gender)
    {
        try
        {
            // Open Food Facts — completely free, no API key needed
            var encoded = Uri.EscapeDataString(query);
            var url = $"https://world.openfoodfacts.org/cgi/search.pl"
                    + $"?search_terms={encoded}&search_simple=1&action=process"
                    + $"&json=1&page_size=10&fields=product_name,nutriments";

            _http.DefaultRequestHeaders.Remove("User-Agent");
            _http.DefaultRequestHeaders.Add("User-Agent", "VitalTrack/1.0 (health tracking app)");

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return GetFallbackFoods(gender);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var results = new List<FoodApiItem>();
            if (doc.RootElement.TryGetProperty("products", out var products))
            {
                foreach (var p in products.EnumerateArray())
                {
                    if (!p.TryGetProperty("product_name", out var nameEl)) continue;
                    var name = nameEl.GetString() ?? "";
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    if (!p.TryGetProperty("nutriments", out var n)) continue;

                    double kcal    = GetDouble(n, "energy-kcal_100g");
                    double protein = GetDouble(n, "proteins_100g");
                    double carbs   = GetDouble(n, "carbohydrates_100g");
                    double fat     = GetDouble(n, "fat_100g");

                    // Skip entries with zero data
                    if (kcal == 0 && protein == 0 && carbs == 0 && fat == 0) continue;

                    results.Add(new FoodApiItem(name, kcal, protein, carbs, fat));
                    if (results.Count >= 8) break;
                }
            }

            return results.Count > 0 ? results : GetFallbackFoods(gender);
        }
        catch
        {
            return GetFallbackFoods(gender);
        }
    }

    private static double GetDouble(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d))
                return Math.Round(d, 1);
        }
        return 0;
    }

    private static List<FoodApiItem> GetFallbackFoods(Gender gender)
    {
        if (gender == Gender.Female)
            return new()
            {
                new("Greek Yogurt (plain)",      59,  10.0, 3.6,  0.4),
                new("Quinoa (cooked)",           120, 4.4,  21.3, 1.9),
                new("Avocado",                   160, 2.0,  8.5,  14.7),
                new("Grilled Salmon (100g)",     182, 25.4, 0.0,  8.8),
                new("Kale Salad",                35,  2.9,  4.4,  0.5),
                new("Low-fat Cottage Cheese",    85,  12.4, 3.4,  1.2),
                new("Almond Milk (unsweetened)", 17,  0.6,  1.3,  1.0),
                new("Chia Seeds (10g serving)",  49,  1.7,  4.2,  3.1),
            };

        return new()
        {
            new("Oatmeal (100g, dry)",         380, 13.2, 67.7, 6.9),
            new("Grilled Chicken Breast",       165, 31.0, 0.0,  3.6),
            new("Brown Rice (cooked, 200g)",    216, 4.5,  44.4, 1.6),
            new("Whole Eggs (2 large)",         156, 12.6, 1.1, 10.6),
            new("Whey Protein Shake",           120, 24.0, 5.0,  2.0),
            new("Banana (1 medium)",            89,  1.1,  22.8, 0.3),
            new("Sirloin Steak (150g)",         207, 26.0, 0.0, 11.0),
            new("Sweet Potato (baked, 150g)",   130, 3.0,  30.0, 0.1),
        };
    }

    private static NutritionLogDto MapToDto(NutritionLog n) => new(
        n.Id, n.FoodName, n.MealType,
        n.Calories, n.ProteinG, n.CarbsG, n.FatG,
        n.ServingGrams, n.LoggedAt
    );
}
