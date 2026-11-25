using PomodoroTimer.Core;

namespace PomodoroTimer.Core.Tests;

/// <summary>テスト用の時計。Now を自由に動かせる。</summary>
internal sealed class FakeClock(DateTime now) : IClock
{
    public DateTime Now { get; set; } = now;
}

/// <summary>テスト用の祝日判定。</summary>
internal sealed class FakeHolidayCalendar(params DateOnly[] holidays) : IHolidayCalendar
{
    private readonly HashSet<DateOnly> _holidays = [.. holidays];

    /// <summary>データを持つ年。null ならすべての年でデータありとする。</summary>
    public HashSet<int>? YearsWithData { get; init; }

    public bool IsHoliday(DateOnly date) => _holidays.Contains(date);

    public bool HasDataFor(int year) => YearsWithData is null || YearsWithData.Contains(year);
}
