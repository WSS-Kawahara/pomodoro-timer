using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using PomodoroTimer.App.ViewModels;
using PomodoroTimer.Core;
using DrawingPoint = System.Drawing.Point;
using WinFormsCursor = System.Windows.Forms.Cursor;

namespace PomodoroTimer.App;

/// <summary>起動処理・常駐・組み立て。</summary>
public partial class App : Application
{
    /// <summary>二重起動防止用の Mutex 名。同じログインセッション内で1つだけ起動できる。</summary>
    private const string SingleInstanceMutexName = @"Local\PomodoroTimer.SingleInstance.3F6A1C2E-8B4D-4E7A-9C1F-5D2B7E9A0C43";

    private Mutex? _singleInstanceMutex;
    private DispatcherTimer? _timer;
    private TimerController? _controller;
    private SettingsRepository? _repository;
    private TrayIconService? _trayIcon;
    private ToastNotifier? _notifier;
    private MainViewModel? _mainViewModel;
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private SettingsViewModel? _settingsViewModel;
    private MiniWidgetViewModel? _miniWidgetViewModel;
    private MiniWidgetWindow? _miniWidget;

    /// <summary>終了処理中か。メイン画面とミニ表示はこの間だけ本当に閉じる。</summary>
    internal bool IsExiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // すでに起動中なら、何もせず終了する
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        // 異常終了のときもトレイアイコンを残さない
        AppDomain.CurrentDomain.UnhandledException += (_, _) => _trayIcon?.Dispose();
        DispatcherUnhandledException += (_, _) => _trayIcon?.Dispose();

        _repository = new SettingsRepository();
        _controller = new TimerController(new SystemClock(), CsvHolidayCalendar.LoadEmbedded(), _repository.Load());

        _notifier = new ToastNotifier();
        _controller.NotificationRequested += (_, message) => _notifier.Show(message);

        // ON/OFF や設定の保存で状態が変わったら、トレイ・メイン画面・ミニ表示をすぐ更新する
        _controller.StateChanged += (_, _) => RefreshViews();

        _mainViewModel = new MainViewModel(_controller, ShowSettingsWindow);
        _miniWidgetViewModel = new MiniWidgetViewModel(_controller);
        _trayIcon = new TrayIconService(_controller, ShowMainWindow, ToggleMiniWidget, ShowSettingsWindow, ExitApplication);

        // 起動直後に1回計算してから、1秒ごとに再計算する（UIスレッドで動くタイマー）
        Tick();
        ApplyMiniWidgetVisibility();

        _timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        IsExiting = true;
        _timer?.Stop();
        _trayIcon?.Dispose();

        if (_singleInstanceMutex is not null)
        {
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
        }

        base.OnExit(e);
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        // Windows のサインアウト・シャットダウン時は、メイン画面やミニ表示を隠さずに閉じる
        IsExiting = true;
        base.OnSessionEnding(e);
    }

    private void Tick()
    {
        _controller!.Tick();
        RefreshViews();
    }

    private void RefreshViews()
    {
        _trayIcon?.Refresh();
        _mainViewModel?.Refresh();
        _miniWidgetViewModel?.Refresh();
    }

    /// <summary>メイン画面を開く。すでに開いていれば前面に出す。</summary>
    private void ShowMainWindow()
    {
        _mainWindow ??= new MainWindow { DataContext = _mainViewModel };
        BringToFront(_mainWindow);
    }

    /// <summary>設定画面を開く。すでに開いていれば前面に出す。</summary>
    private void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            _settingsViewModel = new SettingsViewModel(_controller!, _repository!);
            _settingsWindow = new SettingsWindow(_settingsViewModel);
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                _settingsViewModel = null;

                // 設定画面で「ミニ表示を表示する」が変わっていれば反映する
                ApplyMiniWidgetVisibility();
            };
        }

        BringToFront(_settingsWindow);
    }

    /// <summary>
    /// メニューからミニ表示を切り替える。設定の「ミニ表示を表示する」と同じ値として保存する。
    /// タイマーの計算に関わる設定は変えないので、タイマーの状態・ON/OFF・通知には影響しない。
    /// </summary>
    private void ToggleMiniWidget()
    {
        var settings = _controller!.Settings with { ShowMiniWidget = !_controller.Settings.ShowMiniWidget };

        try
        {
            _repository!.Save(settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 保存できなくても、表示の切り替えは行う
            Debug.WriteLine($"ミニ表示の設定を保存できませんでした: {ex}");
        }

        _controller.UpdateSettings(settings);

        // 設定画面を開いていれば、そのチェックも合わせる（あとで保存しても元に戻らないように）
        if (_settingsViewModel is not null)
        {
            _settingsViewModel.ShowMiniWidget = settings.ShowMiniWidget;
        }

        ApplyMiniWidgetVisibility();
        RefreshViews();
    }

    /// <summary>設定の「ミニ表示を表示する」に合わせて、ミニ表示を出す・隠す。</summary>
    private void ApplyMiniWidgetVisibility()
    {
        if (_controller!.Settings.ShowMiniWidget)
        {
            _miniWidget ??= new MiniWidgetWindow(
                _miniWidgetViewModel!,
                _repository!.MiniWidgetPosition,
                onClick: ShowMainWindow,
                onRightClick: () => _trayIcon?.ShowMenu(WinFormsCursor.Position),
                onMoved: SaveMiniWidgetPosition);
            _miniWidget.Show();
        }
        else
        {
            _miniWidget?.Hide();
        }
    }

    private void SaveMiniWidgetPosition(DrawingPoint position)
    {
        try
        {
            _repository!.SaveMiniWidgetPosition(position);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"ミニ表示の位置を保存できませんでした: {ex}");
        }
    }

    private static void BringToFront(Window window)
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    private void ExitApplication()
    {
        // ミニ表示とメイン画面は Alt+F4 や×ボタンでは閉じないが、終了処理中は閉じられるようにする
        IsExiting = true;
        Shutdown();
    }
}
