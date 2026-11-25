namespace PomodoroTimer.Core;

/// <summary>設定値。</summary>
public sealed record TimerSettings
{
    /// <summary>作業の長さ（固定）。</summary>
    public static readonly TimeSpan WorkDuration = TimeSpan.FromMinutes(25);

    /// <summary>休憩の長さ（固定）。</summary>
    public static readonly TimeSpan ShortBreakDuration = TimeSpan.FromMinutes(5);

    /// <summary>長い休憩までのサイクル数（固定）。</summary>
    public const int CyclesPerSet = 4;

    /// <summary>就業開始時刻。</summary>
    public TimeOnly WorkStart { get; init; } = new(9, 0);

    /// <summary>就業終了時刻。</summary>
    public TimeOnly WorkEnd { get; init; } = new(17, 45);

    /// <summary>昼休憩の開始時刻。</summary>
    public TimeOnly LunchStart { get; init; } = new(12, 0);

    /// <summary>昼休憩の終了時刻。</summary>
    public TimeOnly LunchEnd { get; init; } = new(13, 0);

    /// <summary>長い休憩の長さ（分）。</summary>
    public int LongBreakMinutes { get; init; } = 15;

    /// <summary>ミニ表示を表示するか。</summary>
    public bool ShowMiniWidget { get; init; } = true;

    /// <summary>長い休憩の長さ。</summary>
    public TimeSpan LongBreakDuration => TimeSpan.FromMinutes(LongBreakMinutes);

    /// <summary>入力チェックを行い、エラーメッセージの一覧を返す。問題がなければ空。</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (WorkStart >= WorkEnd)
        {
            errors.Add("就業開始時刻は就業終了時刻より前にしてください。");
        }

        if (LunchStart >= LunchEnd)
        {
            errors.Add("昼休憩の開始時刻は終了時刻より前にしてください。");
        }

        if (LunchStart < WorkStart || LunchEnd > WorkEnd)
        {
            errors.Add("昼休憩は就業時間内に収めてください。");
        }

        if (LongBreakMinutes < 1)
        {
            errors.Add("長い休憩は1分以上にしてください。");
        }

        return errors;
    }
}
