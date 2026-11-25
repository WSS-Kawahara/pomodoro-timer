using System.Windows;
using System.Windows.Threading;
using PomodoroTimer.App.ViewModels;
using PomodoroTimer.Core;

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

    /// <summary>終了処理中か。メイン画面はこの間だけ本当に閉じる。</summary>
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

        // ON/OFF や設定の保存で状態が変わったら、トレイとメイン画面をすぐ更新する
        _controller.StateChanged += (_, _) => RefreshViews();

        _mainViewModel = new MainViewModel(_controller, ShowSettingsWindow);
        _trayIcon = new TrayIconService(_controller, ShowMainWindow, ShowSettingsWindow, ExitApplication);

        // 起動直後に1回計算してから、1秒ごとに再計算する（UIスレッドで動くタイマー）
        Tick();
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
        // Windows のサインアウト・シャットダウン時は、メイン画面を隠さずに閉じる
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
            _settingsWindow = new SettingsWindow(new SettingsViewModel(_controller!, _repository!));
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        BringToFront(_settingsWindow);
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
        IsExiting = true;
        Shutdown();
    }
}
