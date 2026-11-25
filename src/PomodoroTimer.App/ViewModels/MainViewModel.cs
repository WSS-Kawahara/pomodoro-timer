using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimer.Core;

namespace PomodoroTimer.App.ViewModels;

/// <summary>メイン画面の表示と ON/OFF・設定ボタン。</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly TimerController _controller;
    private readonly Action _openSettings;

    [ObservableProperty]
    private string _phaseText = "";

    [ObservableProperty]
    private string _remainingText = "";

    [ObservableProperty]
    private string _cycleText = "";

    [ObservableProperty]
    private string _onOffButtonText = "";

    [ObservableProperty]
    private bool _needsHolidayDataUpdate;

    /// <param name="controller">タイマー。</param>
    /// <param name="openSettings">設定画面を開く処理。</param>
    public MainViewModel(TimerController controller, Action openSettings)
    {
        _controller = controller;
        _openSettings = openSettings;
        Refresh();
    }

    /// <summary>最新の状態で表示を更新する。</summary>
    public void Refresh()
    {
        var status = _controller.Status;
        PhaseText = StatusFormatter.PhaseName(status);
        RemainingText = StatusFormatter.Remaining(status);
        CycleText = StatusFormatter.Cycle(status);
        OnOffButtonText = _controller.IsOn ? "OFFにする" : "ONにする";
        NeedsHolidayDataUpdate = _controller.NeedsHolidayDataUpdate;
    }

    [RelayCommand]
    private void ToggleOnOff()
    {
        if (_controller.IsOn)
        {
            _controller.TurnOff();
        }
        else
        {
            _controller.TurnOn();
        }
    }

    [RelayCommand]
    private void OpenSettings() => _openSettings();
}
