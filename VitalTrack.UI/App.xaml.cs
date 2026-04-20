using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using VitalTrack.Business.Interfaces;
using VitalTrack.Business.Services;
using VitalTrack.Data;
using VitalTrack.UI.Views;

namespace VitalTrack.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        // Load .env file — must be first so GetEnvironmentVariable reads correct values
        DotEnv.Load();

        var services = new ServiceCollection();

        // ── DATA TIER ──────────────────────────────────────────────
        var connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                   ?? "Server=localhost;Database=VitalTrackDb;Trusted_Connection=True;TrustServerCertificate=True;";

        // FIX #2: Use AddDbContextFactory so scoped DbContext is safe to resolve
        // in transient UI services without a scope container.
        services.AddDbContext<VitalTrackDbContext>(opts =>
            opts.UseSqlServer(connStr),
            ServiceLifetime.Transient);   // Transient avoids root-scope DI violation in WPF

        // ── HTTP CLIENTS (external APIs) ───────────────────────────
        services.AddHttpClient("HuggingFace", c =>
        {
            c.BaseAddress = new Uri("https://api-inference.huggingface.co/");
            c.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("ExerciseDB", c =>
        {
            c.BaseAddress = new Uri("https://exercisedb.p.rapidapi.com/");
            var key = (Environment.GetEnvironmentVariable("EXERCISEDB_API_KEY") ?? "").Trim();
            if (!string.IsNullOrEmpty(key))
            {
                c.DefaultRequestHeaders.Add("X-RapidAPI-Key", key);
                c.DefaultRequestHeaders.Add("X-RapidAPI-Host", "exercisedb.p.rapidapi.com");
            }
            c.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient("OpenFoodFacts", c =>
        {
            c.BaseAddress = new Uri("https://world.openfoodfacts.org/");
            c.DefaultRequestHeaders.Add("User-Agent", "VitalTrack/1.0");
            c.Timeout = TimeSpan.FromSeconds(15);
        });

        // ── BUSINESS TIER ──────────────────────────────────────────
        // FIX #2: Transient lifetime matches DbContext lifetime, avoiding scope issues
        services.AddTransient<IAuthService,              AuthService>();
        services.AddTransient<IHealthCalculationService, HealthCalculationService>();
        services.AddTransient<IUserService,              UserService>();
        services.AddTransient<IWorkoutService,           WorkoutService>();
        services.AddTransient<INutritionService,         NutritionService>();
        services.AddTransient<IChatbotService,           ChatbotService>();

        Services = services.BuildServiceProvider();

        // Auto-migrate database on startup
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitalTrackDbContext>();
        try { db.Database.Migrate(); }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Database migration failed:\n\n{ex.Message}\n\nCheck your DB_CONNECTION_STRING in .env",
                "VitalTrack — DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // FIX #21: App.xaml has no StartupUri; we show LoginWindow manually here
        var loginWindow = new LoginWindow();
        loginWindow.Show();
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"UNHANDLED EXCEPTION:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}", 
            "VitalTrack Crash Reporter", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true; // Prevent silent crash
    }
}
