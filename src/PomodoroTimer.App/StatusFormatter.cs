using PomodoroTimer.Core;

namespace PomodoroTimer.App;

/// <summary>状態・残り時間・サイクル数の表示文字列を作る。</summary>
public static class StatusFormatter
{
    /// <summary>状態名。例：「作業中」「待機（昼休憩）」。</summary>
    public static string PhaseName(TimerStatus status) => status.Phase switch
    {
        TimerPhase.Working => "作業中",
        TimerPhase.ShortBreak => "休憩",
        TimerPhase.LongBreak => "長い休憩",
        TimerPhase.Off => "OFF",
        _ => status.IdleReason switch
        {
            IdleReason.LunchBreak => "待機（昼休憩）",
            IdleReason.Holiday => "待機（休日）",
            _ => "待機（時間外）",
        },
    };

    /// <summary>残り時間を mm:ss で表す。動いていないときは「--:--」。1秒未満は切り上げる（開始直後に 25:00 と出るように）。</summary>
    public static string Remaining(TimerStatus status)
    {
        if (!status.IsRunning)
        {
            return "--:--";
        }

        var totalSeconds = (int)Math.Ceiling(status.Remaining.TotalSeconds);
        return $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
    }

    /// <summary>サイクル数。例：「2 / 4」。動いていないときは「- / 4」。</summary>
    public static string Cycle(TimerStatus status) =>
        status.IsRunning
            ? $"{status.Cycle} / {TimerSettings.CyclesPerSet}"
            : $"- / {TimerSettings.CyclesPerSet}";

    /// <summary>1行の要約。例：「作業中 18:32（2/4）」「待機（昼休憩）」。</summary>
    public static string Summary(TimerStatus status) =>
        status.IsRunning
            ? $"{PhaseName(status)} {Remaining(status)}（{status.Cycle}/{TimerSettings.CyclesPerSet}）"
            : PhaseName(status);
}
