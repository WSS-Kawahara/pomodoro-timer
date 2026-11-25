using PomodoroTimer.Core;

namespace PomodoroTimer.Core.Tests;

public class PomodoroCalculatorTests
{
    // 2026/11/25 は水曜日
    private static readonly DateOnly Wednesday = new(2026, 11, 25);
    private static readonly TimerSettings Defaults = new();
    private static readonly FakeHolidayCalendar NoHolidays = new();

    private static DateTime At(int hour, int minute, int second = 0) =>
        Wednesday.ToDateTime(new TimeOnly(hour, minute, second));

    private static TimerStatus Calc(DateTime now, DateTime? baseTime = null, TimerSettings? settings = null, IHolidayCalendar? holidays = null) =>
        PomodoroCalculator.Calculate(now, settings ?? Defaults, baseTime, holidays ?? NoHolidays);

    private static void AssertStatus(TimerStatus status, TimerPhase phase, int cycle, TimeSpan remaining)
    {
        Assert.Equal(phase, status.Phase);
        Assert.Equal(cycle, status.Cycle);
        Assert.Equal(remaining, status.Remaining);
        Assert.Equal(IdleReason.None, status.IdleReason);
    }

    private static void AssertIdle(TimerStatus status, IdleReason reason)
    {
        Assert.Equal(TimerPhase.Idle, status.Phase);
        Assert.Equal(reason, status.IdleReason);
        Assert.Equal(0, status.Cycle);
        Assert.Equal(TimeSpan.Zero, status.Remaining);
        Assert.Null(status.PhaseStart);
    }

    [Fact]
    public void 就業開始ちょうどは1サイクル目の作業中で残り25分()
    {
        var status = Calc(At(9, 0));
        AssertStatus(status, TimerPhase.Working, 1, TimeSpan.FromMinutes(25));
        Assert.Equal(At(9, 0), status.PhaseStart);
    }

    [Fact]
    public void 作業終了の1秒前はまだ作業中()
    {
        AssertStatus(Calc(At(9, 24, 59)), TimerPhase.Working, 1, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void 作業終了ちょうどは休憩でサイクル数は直前の作業のもの()
    {
        var status = Calc(At(9, 25));
        AssertStatus(status, TimerPhase.ShortBreak, 1, TimeSpan.FromMinutes(5));
        Assert.Equal(At(9, 25), status.PhaseStart);
    }

    [Fact]
    public void 休憩明けは2サイクル目の作業中()
    {
        AssertStatus(Calc(At(9, 30)), TimerPhase.Working, 2, TimeSpan.FromMinutes(25));
    }

    [Theory]
    [InlineData(9, 55, TimerPhase.ShortBreak, 2)]
    [InlineData(10, 0, TimerPhase.Working, 3)]
    [InlineData(10, 25, TimerPhase.ShortBreak, 3)]
    [InlineData(10, 30, TimerPhase.Working, 4)]
    public void サイクルが順に進む(int hour, int minute, TimerPhase phase, int cycle)
    {
        var status = Calc(At(hour, minute));
        Assert.Equal(phase, status.Phase);
        Assert.Equal(cycle, status.Cycle);
    }

    [Fact]
    public void 作業4の後は長い休憩()
    {
        AssertStatus(Calc(At(10, 55)), TimerPhase.LongBreak, 4, TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void 長い休憩の後は1サイクル目に戻る()
    {
        AssertStatus(Calc(At(11, 10)), TimerPhase.Working, 1, TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void 長い休憩の長さを変えると次のセットの開始がずれる()
    {
        var settings = new TimerSettings { LongBreakMinutes = 20 };
        AssertStatus(Calc(At(11, 14, 59), settings: settings), TimerPhase.LongBreak, 4, TimeSpan.FromSeconds(1));
        AssertStatus(Calc(At(11, 15), settings: settings), TimerPhase.Working, 1, TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void 残り時間は区間の終了で打ち切られる()
    {
        // 2セット目：11:10 作業1、11:35 休憩、11:40 作業2（本来 12:05 まで）→ 昼休憩開始の 12:00 で打ち切る
        AssertStatus(Calc(At(11, 50)), TimerPhase.Working, 2, TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void 昼休憩開始の1秒前は残り1秒()
    {
        AssertStatus(Calc(At(11, 59, 59)), TimerPhase.Working, 2, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(12, 0, 0)]
    [InlineData(12, 30, 0)]
    [InlineData(12, 59, 59)]
    public void 昼休憩中は待機(int hour, int minute, int second)
    {
        AssertIdle(Calc(At(hour, minute, second)), IdleReason.LunchBreak);
    }

    [Fact]
    public void 昼休憩明けは1サイクル目から再開()
    {
        var status = Calc(At(13, 0));
        AssertStatus(status, TimerPhase.Working, 1, TimeSpan.FromMinutes(25));
        Assert.Equal(At(13, 0), status.PhaseStart);
    }

    [Fact]
    public void 終業の1秒前は残り1秒()
    {
        // 午後は 13:00 開始。1セット 130分なので 3セット目が 17:20 開始、その作業1が 17:20〜17:45
        AssertStatus(Calc(At(17, 44, 59)), TimerPhase.Working, 1, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(17, 45, 0)]
    [InlineData(23, 59, 59)]
    [InlineData(0, 0, 0)]
    [InlineData(8, 59, 59)]
    public void 就業時間外は待機(int hour, int minute, int second)
    {
        AssertIdle(Calc(At(hour, minute, second)), IdleReason.OutsideWorkingHours);
    }

    [Theory]
    [InlineData(2026, 11, 28)] // 土曜
    [InlineData(2026, 11, 29)] // 日曜
    public void 土日は待機(int year, int month, int day)
    {
        var now = new DateOnly(year, month, day).ToDateTime(new TimeOnly(10, 0));
        AssertIdle(Calc(now), IdleReason.Holiday);
    }

    [Fact]
    public void 祝日は待機()
    {
        var holiday = new DateOnly(2026, 11, 23); // 月曜・勤労感謝の日
        var now = holiday.ToDateTime(new TimeOnly(10, 0));
        AssertIdle(Calc(now, holidays: new FakeHolidayCalendar(holiday)), IdleReason.Holiday);
    }

    [Fact]
    public void 就業時間内に起動したらその時刻から1サイクル目()
    {
        var launched = At(10, 7);
        var status = Calc(launched, baseTime: launched);
        AssertStatus(status, TimerPhase.Working, 1, TimeSpan.FromMinutes(25));
        Assert.Equal(launched, status.PhaseStart);

        AssertStatus(Calc(At(10, 32), baseTime: launched), TimerPhase.ShortBreak, 1, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void 午前に起動しても午後は昼休憩明けから1サイクル目()
    {
        var status = Calc(At(13, 0), baseTime: At(10, 7));
        AssertStatus(status, TimerPhase.Working, 1, TimeSpan.FromMinutes(25));
        Assert.Equal(At(13, 0), status.PhaseStart);
    }

    [Fact]
    public void 昼休憩中に起動したら待機で昼休憩明けから作業()
    {
        var launched = At(12, 30);
        AssertIdle(Calc(launched, baseTime: launched), IdleReason.LunchBreak);
        AssertStatus(Calc(At(13, 0), baseTime: launched), TimerPhase.Working, 1, TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void 前日の起動時刻は区間の始まりより早いので影響しない()
    {
        var yesterday = At(10, 7).AddDays(-1);
        AssertStatus(Calc(At(9, 0), baseTime: yesterday), TimerPhase.Working, 1, TimeSpan.FromMinutes(25));
    }
}
