using PomodoroTimer.Core;

namespace PomodoroTimer.App;

/// <summary>実際の現在時刻を返す時計。</summary>
public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
