using PomodoroTimer.Core;

namespace PomodoroTimer.Core.Tests;

public class CsvHolidayCalendarTests
{
    private static readonly CsvHolidayCalendar Calendar = CsvHolidayCalendar.LoadEmbedded();

    [Theory]
    [InlineData(2026, 1, 1)]   // 元日
    [InlineData(2026, 11, 23)] // 勤労感謝の日
    [InlineData(2026, 5, 6)]   // 振替休日
    [InlineData(1955, 1, 1)]   // CSV の先頭行
    [InlineData(2027, 11, 23)] // CSV の最終行
    public void CSVにある日は祝日(int year, int month, int day)
    {
        Assert.True(Calendar.IsHoliday(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2026, 11, 25)]
    [InlineData(2026, 1, 2)]
    public void CSVにない日は祝日ではない(int year, int month, int day)
    {
        Assert.False(Calendar.IsHoliday(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(1955)]
    [InlineData(2026)]
    [InlineData(2027)]
    public void CSVにある年はデータあり(int year)
    {
        Assert.True(Calendar.HasDataFor(year));
    }

    [Theory]
    [InlineData(1954)]
    [InlineData(2028)]
    public void CSVにない年はデータなし(int year)
    {
        Assert.False(Calendar.HasDataFor(year));
    }

    [Fact]
    public void データがない年の日付は祝日ではない()
    {
        Assert.False(Calendar.IsHoliday(new DateOnly(2028, 1, 1)));
    }

    [Fact]
    public void 見出し行と空行は読み飛ばす()
    {
        var csv = "国民の祝日・休日月日,国民の祝日・休日名称\r\n2030/1/1,元日\r\n\r\n";
        var calendar = CsvHolidayCalendar.Parse(new StringReader(csv));
        Assert.True(calendar.IsHoliday(new DateOnly(2030, 1, 1)));
        Assert.True(calendar.HasDataFor(2030));
        Assert.False(calendar.HasDataFor(2029));
    }
}
