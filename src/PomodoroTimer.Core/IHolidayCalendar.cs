namespace PomodoroTimer.Core;

/// <summary>祝日判定の窓口。</summary>
public interface IHolidayCalendar
{
    /// <summary>指定日が祝日（国民の祝日・休日）か。データがない年は false。</summary>
    bool IsHoliday(DateOnly date);

    /// <summary>指定年の祝日データを持っているか。</summary>
    bool HasDataFor(int year);
}
