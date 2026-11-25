using PomodoroTimer.Core;

namespace PomodoroTimer.Core.Tests;

public class TimerControllerTests
{
    // 2026/11/25 は水曜日
    private static readonly DateOnly Wednesday = new(2026, 11, 25);

    private static DateTime At(int hour, int minute, int second = 0) =>
        Wednesday.ToDateTime(new TimeOnly(hour, minute, second));

    private sealed class Harness
    {
        public FakeClock Clock { get; }
        public TimerController Controller { get; }
        public List<string> Notifications { get; } = [];
        public List<TimerStatus> Changes { get; } = [];

        public Harness(DateTime launched, IHolidayCalendar? holidays = null)
        {
            Clock = new FakeClock(launched);
            Controller = new TimerController(Clock, holidays ?? new FakeHolidayCalendar(), new TimerSettings());
            Controller.NotificationRequested += (_, message) => Notifications.Add(message);
            Controller.StateChanged += (_, status) => Changes.Add(status);
        }

        /// <summary>時計を進めて Tick する。</summary>
        public void TickAt(DateTime now)
        {
            Clock.Now = now;
            Controller.Tick();
        }

        /// <summary>from から to まで1秒ずつ Tick する。</summary>
        public void TickEverySecond(DateTime from, DateTime to)
        {
            for (var t = from; t <= to; t = t.AddSeconds(1))
            {
                TickAt(t);
            }
        }
    }

    [Fact]
    public void 就業時間内に起動したら作業開始を1回だけ通知()
    {
        var h = new Harness(At(10, 7));
        h.TickEverySecond(At(10, 7), At(10, 7, 30));

        Assert.Equal([NotificationMessages.WorkStart], h.Notifications);
        Assert.Equal(TimerPhase.Working, h.Controller.Status.Phase);
        Assert.Equal(TimeSpan.FromMinutes(25) - TimeSpan.FromSeconds(30), h.Controller.Status.Remaining);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(12, 30)]
    [InlineData(18, 0)]
    public void 就業時間外や昼休憩中に起動したら通知しない(int hour, int minute)
    {
        var h = new Harness(At(hour, minute));
        h.TickAt(At(hour, minute));

        Assert.Empty(h.Notifications);
        Assert.Equal(TimerPhase.Idle, h.Controller.Status.Phase);
    }

    [Fact]
    public void 就業開始時刻になったら作業開始を通知()
    {
        var h = new Harness(At(8, 59, 58));
        h.TickEverySecond(At(8, 59, 58), At(9, 0, 1));

        Assert.Equal([NotificationMessages.WorkStart], h.Notifications);
    }

    [Fact]
    public void 一日の切り替わりを順に通知する()
    {
        var h = new Harness(At(9, 0));
        h.TickEverySecond(At(9, 0), At(11, 10));

        Assert.Equal(
            [
                NotificationMessages.WorkStart,   // 9:00
                NotificationMessages.ShortBreak,  // 9:25
                NotificationMessages.WorkResume,  // 9:30
                NotificationMessages.ShortBreak,  // 9:55
                NotificationMessages.WorkResume,  // 10:00
                NotificationMessages.ShortBreak,  // 10:25
                NotificationMessages.WorkResume,  // 10:30
                NotificationMessages.LongBreak,   // 10:55
                NotificationMessages.WorkResume,  // 11:10
            ],
            h.Notifications);
    }

    [Fact]
    public void 昼休憩の開始と明けを通知する()
    {
        var h = new Harness(At(11, 59, 58));
        h.TickEverySecond(At(11, 59, 58), At(12, 0, 1));
        h.TickEverySecond(At(12, 59, 58), At(13, 0, 1));

        Assert.Equal(
            [NotificationMessages.WorkStart, NotificationMessages.LunchBreak, NotificationMessages.WorkStart],
            h.Notifications);
    }

    [Fact]
    public void 終業時刻にお疲れさまでしたを通知する()
    {
        var h = new Harness(At(17, 44, 58));
        h.TickEverySecond(At(17, 44, 58), At(17, 45, 1));

        Assert.Equal([NotificationMessages.WorkStart, NotificationMessages.EndOfDay], h.Notifications);
        Assert.Equal(IdleReason.OutsideWorkingHours, h.Controller.Status.IdleReason);
    }

    [Fact]
    public void 状態が変わらないTickでは通知もStateChangedも出ない()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(9, 0));
        h.Notifications.Clear();
        h.Changes.Clear();

        h.TickEverySecond(At(9, 0, 1), At(9, 24, 59));

        Assert.Empty(h.Notifications);
        Assert.Empty(h.Changes);
        Assert.Equal(TimeSpan.FromSeconds(1), h.Controller.Status.Remaining);
    }

    [Fact]
    public void 状態が変わったらStateChangedが出る()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(9, 24, 59));
        h.TickAt(At(9, 25));

        Assert.Equal([TimerPhase.Working, TimerPhase.ShortBreak], h.Changes.Select(s => s.Phase));
    }

    [Theory]
    [InlineData(10, 0)]  // 作業中
    [InlineData(9, 27)]  // 休憩
    [InlineData(11, 0)]  // 長い休憩
    [InlineData(12, 30)] // 待機
    public void OFFにすると通知なしでOFFになりTickしてもOFFのまま(int hour, int minute)
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(hour, minute));
        h.Notifications.Clear();

        h.Controller.TurnOff();
        Assert.Equal(TimerPhase.Off, h.Controller.Status.Phase);
        Assert.False(h.Controller.IsOn);

        h.TickEverySecond(At(13, 0), At(13, 0, 5));
        h.TickAt(At(17, 45));

        Assert.Empty(h.Notifications);
        Assert.Equal(TimerPhase.Off, h.Controller.Status.Phase);
    }

    [Fact]
    public void 就業時間内にONにするとその時刻から1サイクル目で作業開始を通知()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(9, 0));
        h.Controller.TurnOff();
        h.Notifications.Clear();

        h.Clock.Now = At(10, 17);
        h.Controller.TurnOn();

        Assert.True(h.Controller.IsOn);
        Assert.Equal([NotificationMessages.WorkStart], h.Notifications);
        Assert.Equal(TimerPhase.Working, h.Controller.Status.Phase);
        Assert.Equal(1, h.Controller.Status.Cycle);
        Assert.Equal(At(10, 17), h.Controller.Status.PhaseStart);

        h.TickAt(At(10, 42));
        Assert.Equal(TimerPhase.ShortBreak, h.Controller.Status.Phase);
    }

    [Theory]
    [InlineData(12, 30, IdleReason.LunchBreak)]
    [InlineData(18, 0, IdleReason.OutsideWorkingHours)]
    public void 就業時間外や昼休憩中にONにすると待機で通知しない(int hour, int minute, IdleReason reason)
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(9, 0));
        h.Controller.TurnOff();
        h.Notifications.Clear();

        h.Clock.Now = At(hour, minute);
        h.Controller.TurnOn();

        Assert.Empty(h.Notifications);
        Assert.Equal(TimerPhase.Idle, h.Controller.Status.Phase);
        Assert.Equal(reason, h.Controller.Status.IdleReason);
    }

    [Fact]
    public void 昼休憩中にONにすると昼休憩明けから1サイクル目()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(9, 0));
        h.Controller.TurnOff();
        h.Clock.Now = At(12, 30);
        h.Controller.TurnOn();

        h.TickAt(At(13, 0));
        Assert.Equal(TimerPhase.Working, h.Controller.Status.Phase);
        Assert.Equal(At(13, 0), h.Controller.Status.PhaseStart);
    }

    [Fact]
    public void OFFは日付が変わると通知なしで解除され翌朝に作業開始()
    {
        var h = new Harness(At(10, 0));
        h.TickAt(At(10, 0));
        h.Controller.TurnOff();
        h.Notifications.Clear();

        h.TickAt(At(23, 59, 59));
        Assert.Equal(TimerPhase.Off, h.Controller.Status.Phase);

        var nextDay = Wednesday.AddDays(1);
        h.TickAt(nextDay.ToDateTime(new TimeOnly(0, 0)));
        Assert.True(h.Controller.IsOn);
        Assert.Equal(TimerPhase.Idle, h.Controller.Status.Phase);
        Assert.Equal(IdleReason.OutsideWorkingHours, h.Controller.Status.IdleReason);
        Assert.Empty(h.Notifications);

        h.TickAt(nextDay.ToDateTime(new TimeOnly(9, 0)));
        Assert.Equal([NotificationMessages.WorkStart], h.Notifications);
        Assert.Equal(nextDay.ToDateTime(new TimeOnly(9, 0)), h.Controller.Status.PhaseStart);
    }

    [Fact]
    public void 金曜にOFFにしても月曜には解除されている()
    {
        var friday = new DateOnly(2026, 11, 27);
        var monday = new DateOnly(2026, 11, 30);
        var h = new Harness(friday.ToDateTime(new TimeOnly(10, 0)));
        h.TickAt(friday.ToDateTime(new TimeOnly(10, 0)));
        h.Controller.TurnOff();
        h.Notifications.Clear();

        // 土日はスリープしていた想定で、月曜の朝に初めて Tick する
        h.TickAt(monday.ToDateTime(new TimeOnly(9, 30)));

        Assert.True(h.Controller.IsOn);
        Assert.Equal(TimerPhase.Working, h.Controller.Status.Phase);
        Assert.Equal(2, h.Controller.Status.Cycle);
        Assert.Equal([NotificationMessages.WorkStart], h.Notifications);
    }

    [Fact]
    public void スリープ明けで複数の切り替わりをまたいでも現在の状態の分を1回だけ通知()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(9, 10));
        h.Notifications.Clear();

        // 9:10（作業1）→ 10:27（作業3の後の休憩）
        h.TickAt(At(10, 27));

        Assert.Equal([NotificationMessages.ShortBreak], h.Notifications);
        Assert.Equal(3, h.Controller.Status.Cycle);
    }

    [Fact]
    public void スリープ明けで休憩から作業に戻ったら作業再開()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(9, 27));
        h.Notifications.Clear();

        // 9:27（休憩）→ 10:40（作業4）
        h.TickAt(At(10, 40));

        Assert.Equal([NotificationMessages.WorkResume], h.Notifications);
    }

    [Fact]
    public void スリープ明けで作業中から別の作業中になったら作業開始()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(10, 40));
        h.Notifications.Clear();

        // 10:40（午前の作業4）→ 14:00（午後の作業3）
        h.TickAt(At(14, 0));

        Assert.Equal([NotificationMessages.WorkStart], h.Notifications);
        Assert.Equal(3, h.Controller.Status.Cycle);
    }

    [Fact]
    public void スリープ明けで作業中から昼休憩になったらお昼休憩です()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(11, 0));
        h.Notifications.Clear();

        h.TickAt(At(12, 30));

        Assert.Equal([NotificationMessages.LunchBreak], h.Notifications);
    }

    [Fact]
    public void 作業中のままスリープして翌朝に復帰したらお疲れさまでしたを通知しない()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(17, 30));
        h.Notifications.Clear();

        // 水曜 17:30（作業中）→ 木曜 8:50（始業前）
        h.TickAt(Wednesday.AddDays(1).ToDateTime(new TimeOnly(8, 50)));

        Assert.Empty(h.Notifications);
        Assert.Equal(IdleReason.OutsideWorkingHours, h.Controller.Status.IdleReason);
    }

    [Fact]
    public void 金曜に作業中のままスリープして月曜朝に復帰したらお疲れさまでしたを通知しない()
    {
        var friday = new DateOnly(2026, 11, 27);
        var monday = new DateOnly(2026, 11, 30);
        var h = new Harness(friday.ToDateTime(new TimeOnly(9, 0)));
        h.TickAt(friday.ToDateTime(new TimeOnly(17, 30)));
        h.Notifications.Clear();

        h.TickAt(monday.ToDateTime(new TimeOnly(8, 50)));

        Assert.Empty(h.Notifications);
        Assert.Equal(IdleReason.OutsideWorkingHours, h.Controller.Status.IdleReason);
    }

    [Fact]
    public void 作業中のままスリープして翌日の昼休憩中に復帰したらお昼休憩ですを通知しない()
    {
        var h = new Harness(At(9, 0));
        h.TickAt(At(11, 0));
        h.Notifications.Clear();

        // 水曜 11:00（作業中）→ 木曜 12:30（昼休憩中）
        h.TickAt(Wednesday.AddDays(1).ToDateTime(new TimeOnly(12, 30)));

        Assert.Empty(h.Notifications);
        Assert.Equal(IdleReason.LunchBreak, h.Controller.Status.IdleReason);
    }

    [Fact]
    public void 休日は待機で通知しない()
    {
        var saturday = new DateOnly(2026, 11, 28);
        var h = new Harness(saturday.ToDateTime(new TimeOnly(10, 0)));
        h.TickAt(saturday.ToDateTime(new TimeOnly(10, 0)));

        Assert.Empty(h.Notifications);
        Assert.Equal(IdleReason.Holiday, h.Controller.Status.IdleReason);
    }

    [Fact]
    public void 祝日データがない年なら更新が必要()
    {
        var holidays = new FakeHolidayCalendar { YearsWithData = [2026] };
        var now = new DateTime(2027, 1, 4, 10, 0, 0);
        var h = new Harness(now, holidays);
        h.TickAt(now);

        Assert.True(h.Controller.NeedsHolidayDataUpdate);
    }

    [Fact]
    public void 祝日データがある年なら更新は不要()
    {
        var holidays = new FakeHolidayCalendar { YearsWithData = [2026] };
        var h = new Harness(At(10, 0), holidays);
        h.TickAt(At(10, 0));

        Assert.False(h.Controller.NeedsHolidayDataUpdate);
    }
}
