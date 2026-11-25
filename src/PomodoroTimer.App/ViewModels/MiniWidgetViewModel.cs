using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PomodoroTimer.Core;

namespace PomodoroTimer.App.ViewModels;

/// <summary>ミニ表示の状態の色と表示文字列。</summary>
public partial class MiniWidgetViewModel : ObservableObject
{
    // 状態の色：作業中＝赤、休憩＝緑、長い休憩＝青、待機＝灰、OFF＝濃い灰
    private static readonly Brush WorkingBrush = Frozen(Color.FromRgb(0xE5, 0x39, 0x35));
    private static readonly Brush ShortBreakBrush = Frozen(Color.FromRgb(0x43, 0xA0, 0x47));
    private static readonly Brush LongBreakBrush = Frozen(Color.FromRgb(0x1E, 0x88, 0xE5));
    private static readonly Brush IdleBrush = Frozen(Color.FromRgb(0x9E, 0x9E, 0x9E));
    private static readonly Brush OffBrush = Frozen(Color.FromRgb(0x61, 0x61, 0x61));

    private readonly TimerController _controller;

    [ObservableProperty]
    private string _text = "";

    [ObservableProperty]
    private Brush _statusBrush = IdleBrush;

    public MiniWidgetViewModel(TimerController controller)
    {
        _controller = controller;
        Refresh();
    }

    /// <summary>最新の状態で表示を更新する。</summary>
    public void Refresh()
    {
        var status = _controller.Status;
        Text = StatusFormatter.Compact(status);
        StatusBrush = status.Phase switch
        {
            TimerPhase.Working => WorkingBrush,
            TimerPhase.ShortBreak => ShortBreakBrush,
            TimerPhase.LongBreak => LongBreakBrush,
            TimerPhase.Off => OffBrush,
            _ => IdleBrush,
        };
    }

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
