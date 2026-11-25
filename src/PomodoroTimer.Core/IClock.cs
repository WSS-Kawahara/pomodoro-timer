namespace PomodoroTimer.Core;

/// <summary>現在時刻の窓口。現在時刻は必ずここから取得する。</summary>
public interface IClock
{
    /// <summary>現在のローカル時刻。</summary>
    DateTime Now { get; }
}
