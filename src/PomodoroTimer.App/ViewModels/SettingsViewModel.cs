using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimer.Core;

namespace PomodoroTimer.App.ViewModels;

/// <summary>設定画面の入力・入力チェック・保存。</summary>
public partial class SettingsViewModel : ObservableObject
{
    /// <summary>時刻の入力形式（例：9:00）。</summary>
    private const string TimeFormat = "H:mm";

    private readonly TimerController _controller;
    private readonly SettingsRepository _repository;

    [ObservableProperty]
    private string _workStartText;

    [ObservableProperty]
    private string _workEndText;

    [ObservableProperty]
    private string _lunchStartText;

    [ObservableProperty]
    private string _lunchEndText;

    [ObservableProperty]
    private string _longBreakMinutesText;

    [ObservableProperty]
    private bool _showMiniWidget;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = "";

    public SettingsViewModel(TimerController controller, SettingsRepository repository)
    {
        _controller = controller;
        _repository = repository;

        var s = controller.Settings;
        _workStartText = FormatTime(s.WorkStart);
        _workEndText = FormatTime(s.WorkEnd);
        _lunchStartText = FormatTime(s.LunchStart);
        _lunchEndText = FormatTime(s.LunchEnd);
        _longBreakMinutesText = s.LongBreakMinutes.ToString(CultureInfo.InvariantCulture);
        _showMiniWidget = s.ShowMiniWidget;
    }

    /// <summary>画面を閉じてほしいときに発生する。</summary>
    public event EventHandler? CloseRequested;

    /// <summary>エラーを表示中か。</summary>
    public bool HasError => ErrorMessage.Length > 0;

    [RelayCommand]
    private void Save()
    {
        var errors = new List<string>();
        var workStart = ParseTime(WorkStartText, "就業開始時刻", errors);
        var workEnd = ParseTime(WorkEndText, "就業終了時刻", errors);
        var lunchStart = ParseTime(LunchStartText, "昼休憩の開始時刻", errors);
        var lunchEnd = ParseTime(LunchEndText, "昼休憩の終了時刻", errors);

        if (!int.TryParse(LongBreakMinutesText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var longBreakMinutes))
        {
            errors.Add("長い休憩の長さは分を整数で入力してください。");
        }

        if (errors.Count > 0)
        {
            ShowErrors(errors);
            return;
        }

        var settings = _controller.Settings with
        {
            WorkStart = workStart,
            WorkEnd = workEnd,
            LunchStart = lunchStart,
            LunchEnd = lunchEnd,
            LongBreakMinutes = longBreakMinutes,
            ShowMiniWidget = ShowMiniWidget,
        };

        var validationErrors = settings.Validate();
        if (validationErrors.Count > 0)
        {
            ShowErrors(validationErrors);
            return;
        }

        try
        {
            _repository.Save(settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowErrors([$"設定を保存できませんでした。{ex.Message}"]);
            return;
        }

        // 計算に関わる設定が変わっていれば、ON にし直したときと同じ扱いになる（判定は Controller 側）
        _controller.UpdateSettings(settings);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void ShowErrors(IEnumerable<string> errors) => ErrorMessage = string.Join(Environment.NewLine, errors);

    private static string FormatTime(TimeOnly time) => time.ToString(TimeFormat, CultureInfo.InvariantCulture);

    /// <summary>「9:00」の形式の時刻を読む。読めなければエラーを足して既定値を返す。</summary>
    private static TimeOnly ParseTime(string text, string label, List<string> errors)
    {
        if (TimeOnly.TryParseExact(text.Trim(), TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time;
        }

        errors.Add($"{label}は「9:00」の形式で入力してください。");
        return default;
    }
}
