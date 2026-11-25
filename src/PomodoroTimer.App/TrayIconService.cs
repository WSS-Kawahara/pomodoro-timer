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
    /// <param name="onOpenMain">「画面を開く」・ダブルクリックのときの処理。</param>
    /// <param name="onOpenSettings">「設定」が選ばれたときの処理。</param>
    /// <param name="onExit">「終了」が選ばれたときの処理。</param>
    public TrayIconService(TimerController controller, Action onOpenMain, Action onOpenSettings, Action onExit)
    {
        _controller = controller;

        var openItem = new ToolStripMenuItem("画面を開く");
        openItem.Click += (_, _) => onOpenMain();

        _onOffItem = new ToolStripMenuItem("ON/OFF");
        _onOffItem.Click += (_, _) => ToggleOnOff();

        var settingsItem = new ToolStripMenuItem("設定");
        settingsItem.Click += (_, _) => onOpenSettings();

        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (_, _) => onExit();

        var menu = new ContextMenuStrip();
        menu.Items.Add(openItem);
        menu.Items.Add(_onOffItem);
        menu.Items.Add(settingsItem);
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
        _notifyIcon.DoubleClick += (_, _) => onOpenMain();
    }

    /// <summary>最新の状態でツールチップとメニューを更新する。</summary>
    public void Refresh()
    {
        if (_disposed)
        {
            return;
        }

        // NotifyIcon.Text は 127 文字まで
        _notifyIcon.Text = $"{AppName}\n{StatusFormatter.Summary(_controller.Status)}";
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
    }
}
