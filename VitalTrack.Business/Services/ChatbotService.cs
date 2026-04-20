using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data;
using VitalTrack.Data.Entities;

namespace VitalTrack.Business.Services;

public class ChatbotService : IChatbotService
{
    private readonly VitalTrackDbContext _db;
    private readonly HttpClient _http;
    private readonly string _hfApiKey;

    // STRICT SYSTEM PROMPT — AI only answers fitness/nutrition/running
    private const string SystemPrompt =
        "You are FitAI, a specialist health assistant embedded in VitalTrack. " +
        "You ONLY answer questions about: nutrition, meal planning, calorie counting, macros, " +
        "gym workouts, exercise technique, training programs, running, cardio, and general fitness. " +
        "If someone asks about anything else (politics, coding, weather, etc.), politely say: " +
        "'I can only help with nutrition, fitness, gym routines, and running. " +
        "Please ask me something health-related!' " +
        "Keep answers concise, practical, and science-based.";

    public ChatbotService(VitalTrackDbContext db, IHttpClientFactory factory)
    {
        _db     = db;
        _http   = factory.CreateClient("HuggingFace");
        _hfApiKey = Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY") ?? "";
    }

    public async Task<ChatResponse> SendMessageAsync(int userId, string message)
    {
        string reply;
        try
        {
            reply = await CallHuggingFaceAsync(message);
        }
        catch
        {
            // Fallback: rule-based responses
            reply = GetRuleBasedReply(message);
        }

        // Persist to DB (ChatLog table)
        _db.ChatLogs.Add(new ChatLog
        {
            UserId      = userId,
            UserMessage = message,
            BotResponse = reply,
            CreatedAt   = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new ChatResponse(reply);
    }

    public async Task<List<ChatMessage>> GetHistoryAsync(int userId, int limit = 20)
    {
        var logs = await _db.ChatLogs
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var messages = new List<ChatMessage>();
        foreach (var log in logs)
        {
            messages.Add(new ChatMessage("user", log.UserMessage));
            messages.Add(new ChatMessage("assistant", log.BotResponse));
        }
        return messages;
    }

    private async Task<string> CallHuggingFaceAsync(string userMessage)
    {
        var model  = "mistralai/Mistral-7B-Instruct-v0.1";
        var url    = $"https://api-inference.huggingface.co/models/{model}";
        var prompt = $"[INST] <<SYS>>{SystemPrompt}<</SYS>>\n{userMessage} [/INST]";

        var payload = new { inputs = prompt, parameters = new { max_new_tokens = 200, temperature = 0.7 } };
        var json    = JsonSerializer.Serialize(payload);

        // FIX #13: Use HttpRequestMessage to set per-request headers safely (thread-safe)
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        if (!string.IsNullOrEmpty(_hfApiKey))
            request.Headers.Add("Authorization", $"Bearer {_hfApiKey}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync();
        using var doc  = JsonDocument.Parse(resultJson);

        if (doc.RootElement.ValueKind == JsonValueKind.Array &&
            doc.RootElement.GetArrayLength() > 0)
        {
            var generated = doc.RootElement[0].GetProperty("generated_text").GetString() ?? "";
            var instEnd   = generated.LastIndexOf("[/INST]", StringComparison.Ordinal);
            return instEnd >= 0 ? generated[(instEnd + 7)..].Trim() : generated.Trim();
        }

        return "I'm having trouble connecting right now. Please try again shortly.";
    }

    // Rule-based fallback when API is unavailable
    private static string GetRuleBasedReply(string message)
    {
        var lower = message.ToLowerInvariant();

        if (lower.Contains("protein"))
            return "For muscle building, aim for 1.8–2.2g of protein per kg of bodyweight. " +
                   "Your profile has already auto-calculated your personalized protein goal!";

        if (lower.Contains("calori") || lower.Contains("tdee") || lower.Contains("bmr"))
            return "Your TDEE is your total daily calorie expenditure. " +
                   "To lose weight: eat 300–500 kcal below TDEE. " +
                   "To build muscle: eat 200–300 kcal above TDEE.";

        if (lower.Contains("run") || lower.Contains("cardio"))
            return "For beginners, run 3×/week at an easy conversational pace. " +
                   "Increase weekly mileage by no more than 10% to prevent injury.";

        if (lower.Contains("workout") || lower.Contains("exercise") || lower.Contains("gym"))
            return "For strength gains, focus on progressive overload — " +
                   "gradually increase weight, reps, or sets each week. " +
                   "Your gender-specific exercise suggestions are on the Workouts tab!";

        if (lower.Contains("bmi") || lower.Contains("weight"))
            return "BMI is a general indicator. Combine it with body fat % for a complete picture. " +
                   "Your auto-calculated BMI updates whenever you save your profile measurements.";

        return "I can only help with nutrition, fitness, gym routines, and running. " +
               "Please ask me something health-related!";
    }
}
