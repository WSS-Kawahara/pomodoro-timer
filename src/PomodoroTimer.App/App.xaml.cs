using System.Windows;
using System.Windows.Threading;
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
    private TrayIconService? _trayIcon;
    private ToastNotifier? _notifier;

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

        // 設定の保存・読み込みはまだ行わず、初期値を使う
        var settings = new TimerSettings();
        _controller = new TimerController(new SystemClock(), CsvHolidayCalendar.LoadEmbedded(), settings);

        _notifier = new ToastNotifier();
        _controller.NotificationRequested += (_, message) => _notifier.Show(message);

        _trayIcon = new TrayIconService(_controller, onExit: Shutdown);

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
        _timer?.Stop();
        _trayIcon?.Dispose();

        if (_singleInstanceMutex is not null)
        {
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
        }

        base.OnExit(e);
    }

    private void Tick()
    {
        _controller!.Tick();
        _trayIcon!.Refresh();
    }
}
