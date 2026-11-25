using System.Drawing;
using System.Windows.Forms;
using PomodoroTimer.Core;

namespace PomodoroTimer.App;

/// <summary>タスクトレイのアイコンとメニュー。</summary>
public sealed class TrayIconService : IDisposable
{
    private const string AppName = "ポモドーロタイマー";

    private readonly TimerController _controller;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _onOffItem;
    private bool _disposed;

    /// <param name="controller">タイマー。</param>
    /// <param name="onExit">「終了」が選ばれたときの処理。</param>
    public TrayIconService(TimerController controller, Action onExit)
    {
        _controller = controller;

        _onOffItem = new ToolStripMenuItem("ON/OFF");
        _onOffItem.Click += (_, _) => ToggleOnOff();

        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (_, _) => onExit();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_onOffItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            // 仮のアイコン
            Icon = SystemIcons.Application,
            Text = AppName,
            ContextMenuStrip = menu,
            Visible = true,
        };
    }

    /// <summary>最新の状態でツールチップとメニューを更新する。</summary>
    public void Refresh()
    {
        if (_disposed)
        {
            return;
        }

        // NotifyIcon.Text は 127 文字まで
        _notifyIcon.Text = $"{AppName}\n{FormatStatus(_controller.Status)}";
        _onOffItem.Checked = _controller.IsOn;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Visible を false にしてから破棄し、トレイにアイコンが残らないようにする
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
    }

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

        Refresh();
    }

    /// <summary>例：「作業中 18:32（2/4）」「待機（昼休憩）」。</summary>
    private static string FormatStatus(TimerStatus status) => status.Phase switch
    {
        TimerPhase.Working => $"作業中 {FormatRemaining(status.Remaining)}（{status.Cycle}/{TimerSettings.CyclesPerSet}）",
        TimerPhase.ShortBreak => $"休憩 {FormatRemaining(status.Remaining)}（{status.Cycle}/{TimerSettings.CyclesPerSet}）",
        TimerPhase.LongBreak => $"長い休憩 {FormatRemaining(status.Remaining)}（{status.Cycle}/{TimerSettings.CyclesPerSet}）",
        TimerPhase.Off => "OFF",
        _ => status.IdleReason switch
        {
            IdleReason.LunchBreak => "待機（昼休憩）",
            IdleReason.Holiday => "待機（休日）",
            _ => "待機（時間外）",
        },
    };

    /// <summary>残り時間を mm:ss で表す。1秒未満は切り上げる（開始直後に 25:00 と出るように）。</summary>
    private static string FormatRemaining(TimeSpan remaining)
    {
        var totalSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
        return $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
    }
}
