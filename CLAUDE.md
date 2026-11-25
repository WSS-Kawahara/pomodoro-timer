# PomodoroTimer

会社のWindows PCで使う、自分用のポモドーロタイマー常駐アプリ。
仕様はすべて `docs/spec.md` にある。実装前に必ず読み、仕様にない動きを勝手に追加しないこと。
仕様に書かれていない判断が必要になったら、実装を止めて質問すること。

## 技術スタック

- C# / WPF / .NET 10
- TargetFramework は App のみ `net10.0-windows10.0.17763.0`（トースト通知ライブラリの要件）。Core と Core.Tests は `net10.0`
- タスクトレイ：WinForms の `NotifyIcon`（App プロジェクトで `UseWindowsForms` を有効にする）
- Windows通知：`Microsoft.Toolkit.Uwp.Notifications`
- MVVM：`CommunityToolkit.Mvvm`
- テスト：xUnit
- 祝日CSV（内閣府、Shift_JIS）の読み込みには `System.Text.Encoding.CodePages` を使う

## プロジェクト構成

```
PomodoroTimer.sln
src/PomodoroTimer.Core/        ロジックのみ。WPF・WinForms・Windows APIを参照しない
src/PomodoroTimer.App/         WPFの画面とWindows連携
tests/PomodoroTimer.Core.Tests/ Coreのテスト
docs/spec.md                   仕様書
```

ファイルごとの役割は `docs/spec.md` の「詳細設計」を参照。

## 開発ルール

- Core はテスト駆動で作る。テストを先に書き、失敗を確認してから実装する
- 現在時刻は必ず `IClock` 経由で取得する。`DateTime.Now` を直接呼ばない
- `PomodoroCalculator` は副作用のない計算だけを行う（状態を持たない）
- 時刻まわりの境界（区間の開始ちょうど・終了ちょうど、昼休憩をまたぐ、祝日、土日、OFFの翌日解除など）は必ずテストで押さえる
- 作業の区切りごとに `dotnet build` と `dotnet test` を実行し、すべて通ることを確認する
- コメントとコミットメッセージは日本語で書く
- 1回の作業で大きく作りすぎない。指示されたフェーズの範囲だけを実装する

## よく使うコマンド

```
dotnet build
dotnet test
dotnet run --project src/PomodoroTimer.App
dotnet publish src/PomodoroTimer.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
