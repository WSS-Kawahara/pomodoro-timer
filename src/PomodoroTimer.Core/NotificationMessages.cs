namespace PomodoroTimer.Core;

/// <summary>切り替わりごとの通知文言。</summary>
public static class NotificationMessages
{
    public const string WorkStart = "作業開始";
    public const string ShortBreak = "休憩しましょう";
    public const string LongBreak = "長い休憩です";
    public const string WorkResume = "作業再開";
    public const string LunchBreak = "お昼休憩です";
    public const string EndOfDay = "お疲れさまでした";

    /// <summary>
    /// 前回の状態から今回の状態に切り替わったときの通知文言を返す。通知しない場合は null。
    /// スリープ明けなどで複数の切り替わりをまたいだ場合も、今回の状態の分だけを返す。
    /// </summary>
    /// <param name="previous">前回の状態。起動直後は null。</param>
    /// <param name="current">今回の状態。</param>
    public static string? For(TimerStatus? previous, TimerStatus current)
    {
        var wasRunning = previous?.IsRunning ?? false;

        return current.Phase switch
        {
            TimerPhase.Working => previous?.Phase is TimerPhase.ShortBreak or TimerPhase.LongBreak
                ? WorkResume
                : WorkStart,
            TimerPhase.ShortBreak => ShortBreak,
            TimerPhase.LongBreak => LongBreak,
            TimerPhase.Idle when wasRunning && current.IdleReason == IdleReason.LunchBreak => LunchBreak,
            TimerPhase.Idle when wasRunning && current.IdleReason == IdleReason.OutsideWorkingHours => EndOfDay,
            _ => null,
        };
    }
}
