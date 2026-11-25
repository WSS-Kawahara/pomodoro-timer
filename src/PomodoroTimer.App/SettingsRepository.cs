using System.Diagnostics;
using System.IO;
using System.Text.Json;
using PomodoroTimer.Core;
using DrawingPoint = System.Drawing.Point;

namespace PomodoroTimer.App;

/// <summary>
/// 設定の JSON 保存・読み込み。保存先は %AppData%\PomodoroTimer\settings.json。
/// タイマーの設定（TimerSettings）と、画面の都合の値（ミニ表示の位置）を同じファイルに別の項目として持つ。
/// </summary>
public sealed class SettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    /// <summary>最後に読み込んだ・保存した内容。片方だけ保存するときも、もう片方を消さないために持つ。</summary>
    private SettingsData _data = new();

    public SettingsRepository()
        : this(DefaultPath)
    {
    }

    public SettingsRepository(string path)
    {
        _path = path;
    }

    /// <summary>既定の保存先。</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PomodoroTimer",
        "settings.json");

    /// <summary>ミニ表示の位置（画面のピクセル座標）。保存されていなければ null。<see cref="Load"/> のあとに使う。</summary>
    public DrawingPoint? MiniWidgetPosition =>
        _data is { MiniWidgetX: { } x, MiniWidgetY: { } y } ? new DrawingPoint(x, y) : null;

    /// <summary>
    /// 設定を読み込む。ファイルがない・壊れている場合は初期値を返す。
    /// タイマーの設定が入力チェックを通らない場合は、タイマーの設定だけ初期値にする。
    /// </summary>
    public TimerSettings Load()
    {
        _data = ReadFile() ?? new SettingsData();

        var settings = _data.ToSettings();
        if (settings.Validate().Count > 0)
        {
            settings = new TimerSettings();
            _data.SetTimerSettings(settings);
        }

        return settings;
    }

    /// <summary>タイマーの設定を保存する。ミニ表示の位置はそのまま残す。失敗した場合は例外を投げる。</summary>
    public void Save(TimerSettings settings)
    {
        var data = _data.Clone();
        data.SetTimerSettings(settings);
        Write(data);
        _data = data;
    }

    /// <summary>ミニ表示の位置を保存する。タイマーの設定はそのまま残す。失敗した場合は例外を投げる。</summary>
    public void SaveMiniWidgetPosition(DrawingPoint position)
    {
        var data = _data.Clone();
        data.MiniWidgetX = position.X;
        data.MiniWidgetY = position.Y;
        Write(data);
        _data = data;
    }

    private SettingsData? ReadFile()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(_path), JsonOptions)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            Debug.WriteLine($"設定を読み込めなかったため初期値を使います: {ex}");
            return null;
        }
    }

    private void Write(SettingsData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        // 書き込み途中で落ちてもファイルが壊れないよう、一時ファイルに書いてから置き換える
        var tempPath = _path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(tempPath, _path, overwrite: true);
    }

    /// <summary>JSON に書き出す形。項目が欠けていれば初期値を使う（古いファイルも読める）。</summary>
    private sealed class SettingsData
    {
        private static readonly TimerSettings Defaults = new();

        // タイマーの設定
        public TimeOnly WorkStart { get; set; } = Defaults.WorkStart;
        public TimeOnly WorkEnd { get; set; } = Defaults.WorkEnd;
        public TimeOnly LunchStart { get; set; } = Defaults.LunchStart;
        public TimeOnly LunchEnd { get; set; } = Defaults.LunchEnd;
        public int LongBreakMinutes { get; set; } = Defaults.LongBreakMinutes;
        public bool ShowMiniWidget { get; set; } = Defaults.ShowMiniWidget;
        public bool LaunchAtLogin { get; set; } = Defaults.LaunchAtLogin;

        // 画面の都合の値：ミニ表示の位置（画面のピクセル座標）
        public int? MiniWidgetX { get; set; }
        public int? MiniWidgetY { get; set; }

        public SettingsData Clone() => (SettingsData)MemberwiseClone();

        public void SetTimerSettings(TimerSettings s)
        {
            WorkStart = s.WorkStart;
            WorkEnd = s.WorkEnd;
            LunchStart = s.LunchStart;
            LunchEnd = s.LunchEnd;
            LongBreakMinutes = s.LongBreakMinutes;
            ShowMiniWidget = s.ShowMiniWidget;
            LaunchAtLogin = s.LaunchAtLogin;
        }

        public TimerSettings ToSettings() => new()
        {
            WorkStart = WorkStart,
            WorkEnd = WorkEnd,
            LunchStart = LunchStart,
            LunchEnd = LunchEnd,
            LongBreakMinutes = LongBreakMinutes,
            ShowMiniWidget = ShowMiniWidget,
            LaunchAtLogin = LaunchAtLogin,
        };
    }
}
