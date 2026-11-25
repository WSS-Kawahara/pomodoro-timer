using PomodoroTimer.Core;

namespace PomodoroTimer.Core.Tests;

public class TimerSettingsTests
{
    [Fact]
    public void 初期値は仕様どおり()
    {
        var settings = new TimerSettings();
        Assert.Equal(new TimeOnly(9, 0), settings.WorkStart);
        Assert.Equal(new TimeOnly(17, 45), settings.WorkEnd);
        Assert.Equal(new TimeOnly(12, 0), settings.LunchStart);
        Assert.Equal(new TimeOnly(13, 0), settings.LunchEnd);
        Assert.Equal(15, settings.LongBreakMinutes);
        Assert.True(settings.ShowMiniWidget);
    }

    [Fact]
    public void 初期値は入力チェックを通る()
    {
        Assert.Empty(new TimerSettings().Validate());
    }

    [Theory]
    [InlineData(17, 45, 9, 0)]
    [InlineData(9, 0, 9, 0)]
    public void 就業開始が終了より前でなければエラー(int startH, int startM, int endH, int endM)
    {
        var settings = new TimerSettings
        {
            WorkStart = new TimeOnly(startH, startM),
            WorkEnd = new TimeOnly(endH, endM),
        };
        Assert.NotEmpty(settings.Validate());
    }

    [Theory]
    [InlineData(13, 0, 12, 0)]
    [InlineData(12, 0, 12, 0)]
    public void 昼休憩の開始が終了より前でなければエラー(int startH, int startM, int endH, int endM)
    {
        var settings = new TimerSettings
        {
            LunchStart = new TimeOnly(startH, startM),
            LunchEnd = new TimeOnly(endH, endM),
        };
        Assert.NotEmpty(settings.Validate());
    }

    [Theory]
    [InlineData(8, 30, 12, 0)]  // 開始が就業開始より前
    [InlineData(17, 0, 18, 0)]  // 終了が就業終了より後
    public void 昼休憩が就業時間内に収まっていなければエラー(int startH, int startM, int endH, int endM)
    {
        var settings = new TimerSettings
        {
            LunchStart = new TimeOnly(startH, startM),
            LunchEnd = new TimeOnly(endH, endM),
        };
        Assert.NotEmpty(settings.Validate());
    }

    [Fact]
    public void 昼休憩が就業時間の端ちょうどならエラーにしない()
    {
        var settings = new TimerSettings
        {
            LunchStart = new TimeOnly(9, 0),
            LunchEnd = new TimeOnly(17, 45),
        };
        Assert.Empty(settings.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void 長い休憩が1分未満ならエラー(int minutes)
    {
        Assert.NotEmpty(new TimerSettings { LongBreakMinutes = minutes }.Validate());
    }

    [Fact]
    public void 長い休憩が1分ならエラーにしない()
    {
        Assert.Empty(new TimerSettings { LongBreakMinutes = 1 }.Validate());
    }
}
