namespace PomodoroTimer.Core;

/// <summary>ON/OFF の管理、再計算、切り替わりの検知と通知依頼を行う。</summary>
/// <remarks>Core はタイマーを持たない。App 側から1秒ごとに <see cref="Tick"/> を呼ぶ。</remarks>
public sealed class TimerController
{
    private readonly IClock _clock;
    private readonly IHolidayCalendar _holidays;
    private TimerSettings _settings;

    /// <summary>起動した時刻、または ON にした時刻。</summary>
    private DateTime _baseTime;

    /// <summary>OFF にした日付。ON のときは null。</summary>
    private DateOnly? _offDate;

    /// <summary>前回の計算結果。まだ一度も計算していなければ null。</summary>
    private TimerStatus? _previous;

    public TimerController(IClock clock, IHolidayCalendar holidays, TimerSettings settings)
    {
        _clock = clock;
        _holidays = holidays;
        _settings = settings;
        _baseTime = clock.Now;
    }

    /// <summary>通知を出してほしいときに文言付きで発生する。</summary>
    public event EventHandler<string>? NotificationRequested;

    /// <summary>状態が切り替わったときに発生する。</summary>
    public event EventHandler<TimerStatus>? StateChanged;

    /// <summary>最新の計算結果。</summary>
    public TimerStatus Status { get; private set; } = TimerStatus.Idle(IdleReason.OutsideWorkingHours);

    /// <summary>現在の設定。</summary>
    public TimerSettings Settings => _settings;

    /// <summary>ON か。</summary>
    public bool IsOn => _offDate is null;

    /// <summary>今日の年の祝日データがなく、更新が必要か。</summary>
    public bool NeedsHolidayDataUpdate { get; private set; }

    /// <summary>再計算する。App から1秒ごとに呼ぶ。</summary>
    public void Tick() => Update(_clock.Now);

    /// <summary>OFF にする。</summary>
    public void TurnOff()
    {
        var now = _clock.Now;
        _offDate = DateOnly.FromDateTime(now);
        Update(now);
    }

    /// <summary>ON にする。就業時間内ならその時点から1サイクル目を始める。</summary>
    public void TurnOn()
    {
        var now = _clock.Now;
        _offDate = null;
        _baseTime = now;
        Update(now);
    }

    /// <summary>
    /// 設定を差し替える。タイマーの計算に関わる設定（就業時間・昼休憩・長い休憩の長さ）が変わったときだけ、
    /// ON にし直したときと同じ扱いにする（OFF 中なら ON になり、就業時間内ならその時点から1サイクル目を始める）。
    /// それ以外（ミニ表示）だけの変更では、タイマーの状態・ON/OFF・通知に影響させない。
    /// </summary>
    public void UpdateSettings(TimerSettings settings)
    {
        var timerSettingsChanged = !HasSameTimerSettings(_settings, settings);
        _settings = settings;

        if (!timerSettingsChanged)
        {
            return;
        }

        var now = _clock.Now;
        _offDate = null;
        _baseTime = now;

        // OFF から ON にしたときと同じ通知になるよう、前回の状態を OFF とみなす
        _previous = TimerStatus.Off;
        Update(now);
    }

    /// <summary>タイマーの計算に関わる設定がすべて同じか。</summary>
    private static bool HasSameTimerSettings(TimerSettings a, TimerSettings b) =>
        a.WorkStart == b.WorkStart
        && a.WorkEnd == b.WorkEnd
        && a.LunchStart == b.LunchStart
        && a.LunchEnd == b.LunchEnd
        && a.LongBreakMinutes == b.LongBreakMinutes;

    private void Update(DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        // OFF は日付が変わったら解除する
        if (_offDate is { } offDate && offDate != today)
        {
            _offDate = null;
        }

        NeedsHolidayDataUpdate = !_holidays.HasDataFor(today.Year);

        var current = IsOn
            ? PomodoroCalculator.Calculate(now, _settings, _baseTime, _holidays)
            : TimerStatus.Off;

        var previous = _previous;
        Status = current;
        _previous = current;

        if (previous is not null && previous.IsSameState(current))
        {
            return;
        }

        StateChanged?.Invoke(this, current);

        if (NotificationMessages.For(previous, current, today) is { } message)
        {
            NotificationRequested?.Invoke(this, message);
        }
    }
}
