namespace PomodoroTimer.Core;

/// <summary>ある時刻におけるタイマーの計算結果。</summary>
/// <param name="Phase">状態。</param>
/// <param name="Remaining">残り時間。待機・OFF のときは 0。</param>
/// <param name="Cycle">何サイクル目か（1〜4）。休憩中は直前の作業のサイクル数。待機・OFF のときは 0。</param>
/// <param name="IdleReason">待機の理由。待機以外は <see cref="IdleReason.None"/>。</param>
/// <param name="PhaseStart">現在のフェーズの開始時刻。待機・OFF のときは null。</param>
public sealed record TimerStatus(
    TimerPhase Phase,
    TimeSpan Remaining,
    int Cycle,
    IdleReason IdleReason,
    DateTime? PhaseStart)
{
    /// <summary>OFF の状態。</summary>
    public static TimerStatus Off { get; } = new(TimerPhase.Off, TimeSpan.Zero, 0, IdleReason.None, null);

    /// <summary>待機の状態を作る。</summary>
    public static TimerStatus Idle(IdleReason reason) => new(TimerPhase.Idle, TimeSpan.Zero, 0, reason, null);

    /// <summary>作業中・休憩・長い休憩のいずれかか。</summary>
    public bool IsRunning => Phase is TimerPhase.Working or TimerPhase.ShortBreak or TimerPhase.LongBreak;

    /// <summary>
    /// 残り時間を除いて同じ状態か。
    /// 状態の種類・サイクル数・待機の理由・フェーズの開始時刻のどれかが違えば別の状態とみなす。
    /// </summary>
    public bool IsSameState(TimerStatus other) =>
        Phase == other.Phase
        && Cycle == other.Cycle
        && IdleReason == other.IdleReason
        && PhaseStart == other.PhaseStart;
}
