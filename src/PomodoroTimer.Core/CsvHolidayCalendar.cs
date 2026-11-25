using System.Globalization;
using System.Text;

namespace PomodoroTimer.Core;

/// <summary>内閣府の祝日CSV（Shift_JIS、埋め込みリソース）を読む祝日判定。</summary>
public sealed class CsvHolidayCalendar : IHolidayCalendar
{
    private const string ResourceName = "PomodoroTimer.Core.syukujitsu.csv";
    private const int ShiftJisCodePage = 932;

    private readonly HashSet<DateOnly> _holidays;
    private readonly HashSet<int> _years;

    private CsvHolidayCalendar(HashSet<DateOnly> holidays)
    {
        _holidays = holidays;
        _years = [.. holidays.Select(d => d.Year)];
    }

    /// <summary>埋め込みリソースの祝日CSVから作る。</summary>
    public static CsvHolidayCalendar LoadEmbedded()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var stream = typeof(CsvHolidayCalendar).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"埋め込みリソース {ResourceName} が見つかりません。");
        using var reader = new StreamReader(stream, Encoding.GetEncoding(ShiftJisCodePage));
        return Parse(reader);
    }

    /// <summary>CSV のテキストから作る。日付として読めない行（見出し・空行）は読み飛ばす。</summary>
    public static CsvHolidayCalendar Parse(TextReader reader)
    {
        var holidays = new HashSet<DateOnly>();
        while (reader.ReadLine() is { } line)
        {
            var dateText = line.Split(',')[0].Trim();
            if (DateOnly.TryParseExact(dateText, "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                holidays.Add(date);
            }
        }

        return new CsvHolidayCalendar(holidays);
    }

    public bool IsHoliday(DateOnly date) => _holidays.Contains(date);

    public bool HasDataFor(int year) => _years.Contains(year);
}
