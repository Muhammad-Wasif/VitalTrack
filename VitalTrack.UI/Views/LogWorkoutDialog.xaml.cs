using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using VitalTrack.Business.DTOs;
using VitalTrack.Business.Interfaces;

namespace VitalTrack.UI.Views;

public partial class LogWorkoutDialog : Window
{
    private readonly UserProfileDto _user;
    private readonly IWorkoutService _workoutSvc;

    public LogWorkoutDialog(UserProfileDto user, string? prefilledExercise = null)
    {
        InitializeComponent();
        _user       = user;
        _workoutSvc = App.Services.GetRequiredService<IWorkoutService>();

        if (!string.IsNullOrEmpty(prefilledExercise))
            ExerciseBox.Text = prefilledExercise;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(ExerciseBox.Text))
        {
            ErrorText.Text       = "Exercise name is required.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (!int.TryParse(SetsBox.Text, out int sets) ||
            !int.TryParse(RepsBox.Text, out int reps) ||
            !double.TryParse(WeightBox.Text, out double weight) ||
            !int.TryParse(DurationBox.Text, out int duration))
        {
            ErrorText.Text       = "Please enter valid numbers for Sets, Reps, Weight, and Duration.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        var request = new LogWorkoutRequest(
            ExerciseName:    ExerciseBox.Text.Trim(),
            MuscleGroup:     (MuscleBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "General",
            Sets:            sets,
            Reps:            reps,
            WeightKg:        weight,
            DurationMinutes: duration,
            Notes:           NotesBox.Text.Trim()
        );

        var result = await _workoutSvc.LogSessionAsync(_user.Id, request);

        if (result.Success)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            ErrorText.Text       = result.ErrorMessage;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
