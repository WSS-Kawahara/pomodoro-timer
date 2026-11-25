using System.Diagnostics;
using Microsoft.Toolkit.Uwp.Notifications;

namespace PomodoroTimer.App;

/// <summary>Windows通知（トースト）を出す。通知音は Windows 標準。</summary>
public sealed class ToastNotifier
{
    private const string Title = "ポモドーロタイマー";

    // 同じタグとグループを付け、新しい通知で古い通知を置き換える（通知センターには最新の1件だけ残す）
    private const string Tag = "status";
    private const string Group = "PomodoroTimer";

    /// <summary>通知を出す。</summary>
    public void Show(string message)
    {
        try
        {
            // 音を指定しないので、Windows 標準の通知音が鳴る
            new ToastContentBuilder()
                .AddText(Title)
                .AddText(message)
                .Show(toast =>
                {
                    toast.Tag = Tag;
                    toast.Group = Group;
                });
        }
        catch (Exception ex)
        {
            // 通知が出せなくてもタイマーは止めない
            Debug.WriteLine($"通知を出せませんでした: {ex}");
        }
    }
}
