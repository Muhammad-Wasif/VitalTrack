namespace VitalTrack.Data.Entities;

public class ChatLog
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string UserMessage { get; set; } = string.Empty;
    public string BotResponse { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
