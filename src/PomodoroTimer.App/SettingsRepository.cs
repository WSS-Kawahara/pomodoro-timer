using System.Diagnostics;
using System.IO;
using System.Text.Json;
using PomodoroTimer.Core;

namespace PomodoroTimer.App;

/// <summary>設定の JSON 保存・読み込み。保存先は %AppData%\PomodoroTimer\settings.json。</summary>
public sealed class SettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

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

    /// <summary>設定を読み込む。ファイルがない・壊れている・値が入力チェックを通らない場合は初期値を返す。</summary>
    public TimerSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new TimerSettings();
            }

            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(_path), JsonOptions);
            if (data is null)
            {
                return new TimerSettings();
            }

            var settings = data.ToSettings();
            return settings.Validate().Count == 0 ? settings : new TimerSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            Debug.WriteLine($"設定を読み込めなかったため初期値を使います: {ex}");
            return new TimerSettings();
        }
    }

    /// <summary>設定を保存する。失敗した場合は例外を投げる。</summary>
    public void Save(TimerSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        // 書き込み途中で落ちてもファイルが壊れないよう、一時ファイルに書いてから置き換える
        var tempPath = _path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(SettingsData.From(settings), JsonOptions));
        File.Move(tempPath, _path, overwrite: true);
    }

    /// <summary>JSON に書き出す形。項目が欠けていれば初期値を使う。</summary>
    private sealed class SettingsData
    {
        private static readonly TimerSettings Defaults = new();

        public TimeOnly WorkStart { get; set; } = Defaults.WorkStart;
        public TimeOnly WorkEnd { get; set; } = Defaults.WorkEnd;
        public TimeOnly LunchStart { get; set; } = Defaults.LunchStart;
        public TimeOnly LunchEnd { get; set; } = Defaults.LunchEnd;
        public int LongBreakMinutes { get; set; } = Defaults.LongBreakMinutes;
        public bool ShowMiniWidget { get; set; } = Defaults.ShowMiniWidget;
        public bool LaunchAtLogin { get; set; } = Defaults.LaunchAtLogin;

        public static SettingsData From(TimerSettings s) => new()
        {
            WorkStart = s.WorkStart,
            WorkEnd = s.WorkEnd,
            LunchStart = s.LunchStart,
            LunchEnd = s.LunchEnd,
            LongBreakMinutes = s.LongBreakMinutes,
            ShowMiniWidget = s.ShowMiniWidget,
            LaunchAtLogin = s.LaunchAtLogin,
        };

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
