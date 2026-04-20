using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.UI.Views;

public partial class MainWindow : Window
{
    private readonly UserProfileDto _currentUser;
    private readonly IChatbotService _chat;
    private Button? _activeNavBtn;
    private bool _isChatLoading;

    public MainWindow(UserProfileDto user)
    {
        InitializeComponent();
        _currentUser = user;
        _chat        = App.Services.GetRequiredService<IChatbotService>();

        SetupUserInfo();
        NavigateTo("Dashboard", NavDashboard);
    }

    private void SetupUserInfo()
    {
        var initials = string.Concat(_currentUser.FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(w => w[0]));

        SidebarAvatarText.Text = initials.ToUpper();
        SidebarFullName.Text   = _currentUser.FullName;
        SidebarRole.Text       = _currentUser.Role == UserRole.Admin ? "🛡 Administrator" : "👤 Standard User";

        if (_currentUser.Role == UserRole.Admin)
            NavAdmin.Visibility = Visibility.Visible;
    }

    // ── Navigation (public so DashboardPage can call it) ────────────────────
    public void NavigateToPage(string page)
    {
        var btn = page switch
        {
            "Dashboard" => NavDashboard,
            "Profile"   => NavProfile,
            "Nutrition" => NavNutrition,
            "Workouts"  => NavWorkouts,
            "Admin"     => NavAdmin,
            _           => NavDashboard
        };
        NavigateTo(page, btn);
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            NavigateTo(btn.Tag?.ToString() ?? "Dashboard", btn);
    }

    private void NavigateTo(string page, Button navBtn)
    {
        if (_activeNavBtn != null)
        {
            _activeNavBtn.Background = Brushes.Transparent;
            _activeNavBtn.Foreground = (Brush)FindResource("TextMutedBrush");
        }

        _activeNavBtn            = navBtn;
        navBtn.Background        = new SolidColorBrush(Color.FromArgb(30, 224, 64, 162));
        navBtn.Foreground        = (Brush)FindResource("TextPrimaryBrush");
        PageTitleText.Text       = page switch
        {
            "Dashboard" => "Dashboard",
            "Profile"   => "My Profile",
            "Nutrition" => "Nutrition Log",
            "Workouts"  => "Workouts",
            "Admin"     => "Admin Panel",
            _           => page
        };

        Page pageObj = page switch
        {
            "Dashboard" => new DashboardPage(_currentUser),
            "Profile"   => new ProfilePage(_currentUser),
            "Nutrition" => new NutritionPage(_currentUser),
            "Workouts"  => new WorkoutsPage(_currentUser),
            "Admin"     => new AdminPage(_currentUser),
            _           => new DashboardPage(_currentUser)
        };

        MainFrame.Navigate(pageObj);
    }

    // ── Chatbot ──────────────────────────────────────────────────────────────
    private void ChatFab_Click(object sender, RoutedEventArgs e)
        => ChatWindow.Visibility = ChatWindow.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;

    private void CloseChat_Click(object sender, RoutedEventArgs e)
        => ChatWindow.Visibility = Visibility.Collapsed;

    private void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !_isChatLoading)
            _ = SendChatAsync();
    }

    private async void SendChat_Click(object sender, RoutedEventArgs e)
        => await SendChatAsync();

    private async Task SendChatAsync()
    {
        var msg = ChatInputBox.Text.Trim();
        if (string.IsNullOrEmpty(msg) || _isChatLoading) return;

        _isChatLoading    = true;
        ChatInputBox.Text = "";

        AddChatBubble(msg, isUser: true);

        // Typing indicator
        var typingBubble = AddChatBubble("FitAI is thinking…", isUser: false, isTyping: true);

        ChatResponse response;
        try { response = await _chat.SendMessageAsync(_currentUser.Id, msg); }
        catch { response = new ChatResponse("Sorry, I had trouble connecting. Please try again."); }

        ChatMessagesPanel.Children.Remove(typingBubble);
        AddChatBubble(response.Reply, isUser: false);

        _isChatLoading = false;
    }

    private Border AddChatBubble(string text, bool isUser, bool isTyping = false)
    {
        var border = new Border
        {
            CornerRadius        = new CornerRadius(isUser ? 14 : 14, 14, isUser ? 4 : 14, isUser ? 14 : 4),
            Padding             = new Thickness(12, 8, 12, 8),
            Margin              = new Thickness(isUser ? 40 : 0, 0, isUser ? 0 : 40, 8),
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            MaxWidth            = 290,
            Opacity             = isTyping ? 0.6 : 1.0
        };

        border.Background = isUser
            ? new LinearGradientBrush(
                Color.FromRgb(0x7C, 0x4D, 0xFF),
                Color.FromRgb(0xE0, 0x40, 0xA2),
                new System.Windows.Point(0, 0), new System.Windows.Point(1, 1))
            : (Brush)FindResource("BgTertiaryBrush");

        border.Child = new TextBlock
        {
            Text         = text,
            Foreground   = Brushes.White,
            FontSize     = 13,
            TextWrapping = TextWrapping.Wrap,
            LineHeight   = 20
        };

        ChatMessagesPanel.Children.Add(border);
        ChatScrollViewer.ScrollToBottom();
        return border;
    }

    // ── Window chrome ────────────────────────────────────────────────────────
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var login = new LoginWindow();
        login.Show();
        Close();
    }
}
