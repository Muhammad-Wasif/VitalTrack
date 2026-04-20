using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.UI.Views;

/// <summary>
/// Full meal-logging dialog: food search via Open Food Facts API (gender-aware fallback),
/// per-field nutrient entry, meal type selection, serving size scaling.
/// Built in code-behind (no .xaml) so the UI is fully self-contained.
/// </summary>
public class LogMealDialog : Window
{
    private readonly UserProfileDto _user;
    private readonly INutritionService _svc;

    private TextBox _foodBox = null!, _calBox = null!, _protBox = null!,
                    _carbBox = null!, _fatBox = null!, _servBox = null!;
    private ComboBox _mealBox = null!;
    private TextBlock _errorText = null!, _statusText = null!;
    private ListBox _searchList = null!;
    private Button _searchBtn = null!;
    private List<FoodApiItem> _apiResults = new();

    public LogMealDialog(UserProfileDto user)
    {
        _user = user;
        _svc  = App.Services.GetRequiredService<INutritionService>();
        BuildUI();
    }

    // ── UI construction ──────────────────────────────────────────────────────
    private void BuildUI()
    {
        Title                 = "Log Meal";
        Width                 = 520;
        Height                = 620;
        WindowStyle           = WindowStyle.None;
        AllowsTransparency    = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background            = Brushes.Transparent;
        ResizeMode            = ResizeMode.NoResize;

        var root = new Border
        {
            CornerRadius    = new CornerRadius(16),
            Background      = Res("BgSecondaryBrush"),
            BorderBrush     = Res("BorderBrush"),
            BorderThickness = new Thickness(1)
        };
        Content = root;

        var outer = new Grid { Margin = new Thickness(28) };
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Child = outer;

        // Header
        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
        headerGrid.Children.Add(new TextBlock
        {
            Text       = "Log Meal",
            FontFamily = new FontFamily("Segoe UI Black"),
            FontSize   = 18,
            Foreground = Res("TextPrimaryBrush")
        });
        var closeBtn = MakeBtn("✕", "GhostButtonStyle");
        closeBtn.Width = closeBtn.Height = 30;
        closeBtn.HorizontalAlignment = HorizontalAlignment.Right;
        closeBtn.VerticalAlignment   = VerticalAlignment.Top;
        closeBtn.Click              += (_, _) => Close();
        headerGrid.Children.Add(closeBtn);
        Grid.SetRow(headerGrid, 0);
        outer.Children.Add(headerGrid);

        // Form scroll area
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var form   = new StackPanel();
        scroll.Content = form;
        Grid.SetRow(scroll, 1);
        outer.Children.Add(scroll);

        // Food search row
        AddLabel(form, "SEARCH FOOD (API)");
        var searchRow = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        searchRow.ColumnDefinitions.Add(new ColumnDefinition());
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        _foodBox = new TextBox { Style = Sty("InputFieldStyle") };
        _foodBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) _ = SearchFood(); };
        Grid.SetColumn(_foodBox, 0);
        _searchBtn = MakeBtn("Search", "PrimaryButtonStyle");
        _searchBtn.Margin = new Thickness(8, 0, 0, 0);
        _searchBtn.Height = 40;
        _searchBtn.Click += async (_, _) => await SearchFood();
        Grid.SetColumn(_searchBtn, 1);
        searchRow.Children.Add(_foodBox);
        searchRow.Children.Add(_searchBtn);
        form.Children.Add(searchRow);

        // Status text (shows "Searching…" / "X results found")
        _statusText = new TextBlock
        {
            FontSize   = 11,
            Foreground = Res("TextMutedBrush"),
            Margin     = new Thickness(0, 0, 0, 4),
            Visibility = Visibility.Collapsed
        };
        form.Children.Add(_statusText);

        // Search results listbox
        _searchList = new ListBox
        {
            Height          = 110,
            Margin          = new Thickness(0, 0, 0, 14),
            Background      = Res("BgTertiaryBrush"),
            Foreground      = Res("TextPrimaryBrush"),
            BorderThickness = new Thickness(1),
            BorderBrush     = Res("BorderBrush"),
            FontSize        = 12,
            Visibility      = Visibility.Collapsed
        };
        _searchList.SelectionChanged += SearchResult_Selected;
        form.Children.Add(_searchList);

        // Meal type
        AddLabel(form, "MEAL TYPE");
        _mealBox = new ComboBox { Style = Sty("ComboFieldStyle"), Margin = new Thickness(0, 0, 0, 14) };
        foreach (var m in new[] { "Breakfast", "Lunch", "Dinner", "Snack" })
            _mealBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = m });
        _mealBox.SelectedIndex = 0;
        form.Children.Add(_mealBox);

        // Nutrient fields — 5 columns
        var nutrGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        for (int i = 0; i < 9; i++)
            nutrGrid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = i % 2 == 1 ? new GridLength(8) : new GridLength(1, GridUnitType.Star) });

        void AddNum(string label, ref TextBox box, int col, string def = "0")
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text       = label,
                FontSize   = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Res("TextMutedBrush"),
                Margin     = new Thickness(0, 0, 0, 6)
            });
            box = new TextBox { Style = Sty("InputFieldStyle"), Text = def };
            box.GotFocus  += (_, _) => { if (box.Text == "0") box.Text = ""; };
            box.LostFocus += (_, _) => { if (string.IsNullOrWhiteSpace(box.Text)) box.Text = "0"; };
            sp.Children.Add(box);
            Grid.SetColumn(sp, col);
            nutrGrid.Children.Add(sp);
        }

        AddNum("CALORIES",    ref _calBox,  0, "0");
        AddNum("PROTEIN (g)", ref _protBox, 2, "0");
        AddNum("CARBS (g)",   ref _carbBox, 4, "0");
        AddNum("FAT (g)",     ref _fatBox,  6, "0");
        AddNum("SERVING (g)", ref _servBox, 8, "100");
        form.Children.Add(nutrGrid);

        // Serving note
        form.Children.Add(new TextBlock
        {
            Text       = "💡 Nutrients are per the serving size entered above.",
            FontSize   = 11,
            Foreground = Res("TextMutedBrush"),
            Margin     = new Thickness(0, 0, 0, 12)
        });

        // Error text
        _errorText = new TextBlock
        {
            Foreground  = Brushes.OrangeRed,
            FontSize    = 12,
            Visibility  = Visibility.Collapsed,
            TextWrapping = TextWrapping.Wrap,
            Margin      = new Thickness(0, 0, 0, 8)
        };
        form.Children.Add(_errorText);

        // Footer buttons
        var btnGrid = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var cancelBtn = MakeBtn("Cancel", "GhostButtonStyle");
        cancelBtn.Height = 44;
        cancelBtn.Click += (_, _) => Close();
        var saveBtn = MakeBtn("💾  Log Meal", "PrimaryButtonStyle");
        saveBtn.Height = 44;
        saveBtn.Click += async (_, _) => await Save();
        Grid.SetColumn(cancelBtn, 0);
        Grid.SetColumn(saveBtn, 2);
        btnGrid.Children.Add(cancelBtn);
        btnGrid.Children.Add(saveBtn);
        Grid.SetRow(btnGrid, 2);
        outer.Children.Add(btnGrid);
    }

    // ── Search ───────────────────────────────────────────────────────────────
    private async Task SearchFood()
    {
        var q = _foodBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) return;

        _searchBtn.IsEnabled       = false;
        _statusText.Text           = "Searching Open Food Facts…";
        _statusText.Visibility     = Visibility.Visible;
        _searchList.Visibility     = Visibility.Collapsed;

        try
        {
            _apiResults = await _svc.SearchFoodAsync(q, _user.Gender);
            _searchList.Items.Clear();
            foreach (var item in _apiResults)
                _searchList.Items.Add($"{item.FoodName}  —  {item.CaloriesPer100g:F0} kcal / 100g");

            _statusText.Text        = _apiResults.Count > 0
                ? $"✓ {_apiResults.Count} results — click to fill fields"
                : "No results found. Enter nutrients manually.";
            _searchList.Visibility  = _apiResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            _statusText.Text = "⚠ API unavailable — using built-in suggestions.";
        }
        finally
        {
            _searchBtn.IsEnabled = true;
        }
    }

    private void SearchResult_Selected(object sender, SelectionChangedEventArgs e)
    {
        int idx = _searchList.SelectedIndex;
        if (idx < 0 || idx >= _apiResults.Count) return;

        var item        = _apiResults[idx];
        _foodBox.Text   = item.FoodName;
        _calBox.Text    = item.CaloriesPer100g.ToString("F0");
        _protBox.Text   = item.ProteinPer100g.ToString("F1");
        _carbBox.Text   = item.CarbsPer100g.ToString("F1");
        _fatBox.Text    = item.FatPer100g.ToString("F1");
        _servBox.Text   = "100";
        _searchList.Visibility  = Visibility.Collapsed;
        _statusText.Visibility  = Visibility.Collapsed;
    }

    // ── Save ─────────────────────────────────────────────────────────────────
    private async Task Save()
    {
        _errorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(_foodBox.Text))
        {
            ShowError("Food name is required.");
            return;
        }
        if (!double.TryParse(_calBox.Text, out double cal) || cal < 0 ||
            !double.TryParse(_protBox.Text, out double prot) ||
            !double.TryParse(_carbBox.Text, out double carb) ||
            !double.TryParse(_fatBox.Text, out double fat) ||
            !double.TryParse(_servBox.Text, out double serv) || serv <= 0)
        {
            ShowError("Please enter valid non-negative numbers for all nutrient fields. Serving must be > 0.");
            return;
        }

        var meal = (_mealBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Snack";
        var req  = new LogNutritionRequest(_foodBox.Text.Trim(), meal, cal, prot, carb, fat, serv);

        var result = await _svc.LogMealAsync(_user.Id, req);
        if (result.Success)
            Close();
        else
            ShowError(result.ErrorMessage ?? "Failed to log meal.");
    }

    private void ShowError(string msg)
    {
        _errorText.Text       = msg;
        _errorText.Visibility = Visibility.Visible;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static void AddLabel(StackPanel p, string text)
        => p.Children.Add(new TextBlock
        {
            Text       = text,
            FontSize   = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)App.Current.Resources["TextMutedBrush"],
            Margin     = new Thickness(0, 0, 0, 6)
        });

    private static Button MakeBtn(string label, string styleKey)
        => new Button { Content = label, Style = Sty(styleKey) };

    private static Style Sty(string key)
        => (Style)App.Current.Resources[key];

    private static Brush Res(string key)
        => (Brush)App.Current.Resources[key];
}
