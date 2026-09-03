# Memory Analyzer

Windows 11 でメモリを多く使っている原因を「どのアプリが、いつから、どれだけ増えたか」として把握し、
安全に減らせる候補と具体的な対策を、専門知識なしで判断できるようにするデスクトップアプリです。

## できること

- 空きRAM・コミット使用率を3秒ごとに表示
- 同名プロセスをアプリ単位に束ねたメモリ使用量と増減傾向
- 推奨対策、効果の目安、注意点、安全段階を日本語で表示
- 過去7日の最大・平均・最近値をローカル保存して再起動後も確認
- 物理RAMを重複しない5区分のゲージで表示
- ×ボタンで通知領域へ隠れても履歴記録を継続

## 必要環境

- Windows 11
- .NET 8 Desktop Runtime（[ダウンロード](https://dotnet.microsoft.com/download/dotnet/8.0)）

## ダウンロード

最新版は [Releases](https://github.com/otakichi1985/MemoryAnalyzer/releases) から
`MemoryAnalyzer-<version>.zip` をダウンロードし、任意のフォルダに展開して
`MemoryAnalyzer.App.exe` を起動してください。

アプリは起動時に新しいバージョンの有無を自動で確認し、あれば画面上部に知らせます。
「更新する」を押すとダウンロードして最新版へ差し替えます。

## ビルド

```sh
dotnet publish src\MemoryAnalyzer.App\MemoryAnalyzer.App.csproj -c Release -o dist\MemoryAnalyzer
```

## 新バージョンの公開

PowerShell 7（pwsh）で次を実行します（起動中のアプリは自動で終了し、zip・内容チェック付きで公開されます）。

```sh
pwsh -File scripts\create-release.ps1 -Version 1.2.0 -Notes "変更点の説明"
```

## テスト

```sh
dotnet test tests\MemoryAnalyzer.Core.Tests\MemoryAnalyzer.Core.Tests.csproj
```

## 技術構成

- C# / .NET 8
- WPF（WindowsデスクトップUI）
- Windows標準APIを利用した読み取り専用のプロセス監視
