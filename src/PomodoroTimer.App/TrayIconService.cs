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
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _onOffItem;
    private readonly ToolStripMenuItem _miniWidgetItem;
    private bool _disposed;

    /// <param name="controller">タイマー。</param>
    /// <param name="onOpenMain">「画面を開く」・ダブルクリックのときの処理。</param>
    /// <param name="onToggleMiniWidget">「ミニ表示を隠す／表示する」が選ばれたときの処理。</param>
    /// <param name="onOpenSettings">「設定」が選ばれたときの処理。</param>
    /// <param name="onExit">「終了」が選ばれたときの処理。</param>
    public TrayIconService(
        TimerController controller,
        Action onOpenMain,
        Action onToggleMiniWidget,
        Action onOpenSettings,
        Action onExit)
    {
        _controller = controller;

        var openItem = new ToolStripMenuItem("画面を開く");
        openItem.Click += (_, _) => onOpenMain();

        _onOffItem = new ToolStripMenuItem("ON/OFF");
        _onOffItem.Click += (_, _) => ToggleOnOff();

        _miniWidgetItem = new ToolStripMenuItem();
        _miniWidgetItem.Click += (_, _) => onToggleMiniWidget();

        var settingsItem = new ToolStripMenuItem("設定");
        settingsItem.Click += (_, _) => onOpenSettings();

        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (_, _) => onExit();

        _menu = new ContextMenuStrip();
        _menu.Items.Add(openItem);
        _menu.Items.Add(_onOffItem);
        _menu.Items.Add(_miniWidgetItem);
        _menu.Items.Add(settingsItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exitItem);

        // 開くたびに、今の状態に合わせて文言とチェックを更新する
        _menu.Opening += (_, _) => UpdateMenuItems();
        UpdateMenuItems();

        _notifyIcon = new NotifyIcon
        {
            // 仮のアイコン
            Icon = SystemIcons.Application,
            Text = AppName,
            ContextMenuStrip = _menu,
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
        UpdateMenuItems();
    }

    /// <summary>トレイと同じメニューを、指定した位置（画面のピクセル座標）に出す。ミニ表示の右クリック用。</summary>
    public void ShowMenu(Point screenPosition)
    {
        if (_disposed)
        {
            return;
        }

        _menu.Show(screenPosition);
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
        _notifyIcon.Dispose();
        _menu.Dispose();
    }

    private void UpdateMenuItems()
    {
        _onOffItem.Checked = _controller.IsOn;
        _miniWidgetItem.Text = _controller.Settings.ShowMiniWidget ? "ミニ表示を隠す" : "ミニ表示を表示する";
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
