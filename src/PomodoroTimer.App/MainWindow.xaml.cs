using System.ComponentModel;
using System.Windows;

namespace PomodoroTimer.App;

/// <summary>メイン画面。</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>×ボタンではアプリを終了せず、ウィンドウを隠すだけにする。</summary>
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (Application.Current is App { IsExiting: true })
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
