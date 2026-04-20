using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;
using VitalTrack.Data.Entities;

namespace VitalTrack.UI.Views;

public partial class LoginWindow : Window
{
    private readonly IAuthService _auth;
    private UserRole _selectedRole = UserRole.StandardUser;

    public LoginWindow()
    {
        InitializeComponent();
        _auth = App.Services.GetRequiredService<IAuthService>();
    }

    // ── Tab switching ────────────────────────────────────────────────────────
    private void ShowLogin_Click(object sender, RoutedEventArgs e)
    {
        LoginPanel.Visibility  = Visibility.Visible;
        SignupPanel.Visibility = Visibility.Collapsed;
        LoginTabHighlight.SetValue(Grid.ColumnProperty, 0);
        LoginTabBtn.Foreground  = System.Windows.Media.Brushes.White;
        SignupTabBtn.Foreground = FindResource("TextMutedBrush") as System.Windows.Media.Brush;
    }

    private void ShowSignup_Click(object sender, RoutedEventArgs e)
    {
        LoginPanel.Visibility  = Visibility.Collapsed;
        SignupPanel.Visibility = Visibility.Visible;
        LoginTabHighlight.SetValue(Grid.ColumnProperty, 1);
        SignupTabBtn.Foreground = System.Windows.Media.Brushes.White;
        LoginTabBtn.Foreground  = FindResource("TextMutedBrush") as System.Windows.Media.Brush;
    }

    // ── Role selection ───────────────────────────────────────────────────────
    private void SelectRoleUser(object sender, MouseButtonEventArgs e)
    {
        _selectedRole = UserRole.StandardUser;
        RoleUserBorder.Background  = System.Windows.Media.Brushes.Transparent;
        RoleUserBorder.SetValue(Border.BackgroundProperty,
            new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)FindResource("AccentPinkColor")) { Opacity = 0.1 });
        RoleUserBorder.BorderBrush = FindResource("AccentPinkBrush") as System.Windows.Media.Brush;
        RoleAdminBorder.Background = FindResource("BgTertiaryBrush") as System.Windows.Media.Brush;
        RoleAdminBorder.BorderBrush = FindResource("BorderBrush") as System.Windows.Media.Brush;
        AdminCodePanel.Visibility  = Visibility.Collapsed;
    }

    private void SelectRoleAdmin(object sender, MouseButtonEventArgs e)
    {
        _selectedRole = UserRole.Admin;
        RoleAdminBorder.Background  = System.Windows.Media.Brushes.Transparent;
        RoleUserBorder.Background   = FindResource("BgTertiaryBrush") as System.Windows.Media.Brush;
        RoleUserBorder.BorderBrush  = FindResource("BorderBrush") as System.Windows.Media.Brush;
        AdminCodePanel.Visibility   = Visibility.Visible;
    }

    // ── Login ────────────────────────────────────────────────────────────────
    private void Login_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Login_Click(sender, e);
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        LoginErrorText.Visibility = Visibility.Collapsed;
        var result = await _auth.LoginAsync(
            new LoginRequest(LoginUsernameBox.Text.Trim(), LoginPasswordBox.Password));

        if (!result.Success)
        {
            LoginErrorText.Text       = result.ErrorMessage;
            LoginErrorText.Visibility = Visibility.Visible;
            return;
        }

        OpenMainWindow(result.Data!);
    }

    // ── Register ─────────────────────────────────────────────────────────────
    private async void Signup_Click(object sender, RoutedEventArgs e)
    {
        SignupErrorText.Visibility = Visibility.Collapsed;

        if (!int.TryParse(SuAgeBox.Text, out int age))
        {
            SignupErrorText.Text       = "Please enter a valid age.";
            SignupErrorText.Visibility = Visibility.Visible;
            return;
        }

        var gender = SuGenderBox.SelectedIndex switch
        {
            1 => Gender.Female,
            2 => Gender.Other,
            _ => Gender.Male
        };

        var request = new RegisterRequest(
            Username:        SuUsernameBox.Text.Trim(),
            FullName:        SuFullNameBox.Text.Trim(),
            Email:           SuEmailBox.Text.Trim(),
            Password:        SuPasswordBox.Password,
            Gender:          gender,
            Role:            _selectedRole,
            Age:             age,
            HeightCm:        170,
            WeightKg:        70,
            AdminSecretCode: SuAdminCodeBox.Password
        );

        var result = await _auth.RegisterAsync(request);

        if (!result.Success)
        {
            SignupErrorText.Text       = result.ErrorMessage;
            SignupErrorText.Visibility = Visibility.Visible;
            return;
        }

        OpenMainWindow(result.Data!);
    }

    private void OpenMainWindow(UserProfileDto user)
    {
        var main = new MainWindow(user);
        main.Show();
        Close();
    }

    // ── Window chrome ────────────────────────────────────────────────────────
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
