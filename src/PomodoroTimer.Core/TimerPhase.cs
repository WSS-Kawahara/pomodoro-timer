namespace PomodoroTimer.Core;

/// <summary>タイマーの状態の種類。</summary>
public enum TimerPhase
{
    /// <summary>待機（時間外・休日・昼休憩中）。理由は <see cref="IdleReason"/> で区別する。</summary>
    Idle,

    /// <summary>作業中（25分）。</summary>
    Working,

    /// <summary>休憩（1〜3サイクル目の作業後、5分）。</summary>
    ShortBreak,

    /// <summary>長い休憩（4サイクル目の作業後、設定値）。</summary>
    LongBreak,

    /// <summary>ユーザーが手動で停止した状態。</summary>
    Off,
}

/// <summary>待機の理由。</summary>
public enum IdleReason
{
    /// <summary>待機ではない。</summary>
    None,

    /// <summary>就業時間外。</summary>
    OutsideWorkingHours,

    /// <summary>土日・祝日。</summary>
    Holiday,

    /// <summary>昼休憩中。</summary>
    LunchBreak,
}
