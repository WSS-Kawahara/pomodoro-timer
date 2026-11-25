using System.Windows;
using PomodoroTimer.App.ViewModels;

namespace PomodoroTimer.App;

/// <summary>設定画面。</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
