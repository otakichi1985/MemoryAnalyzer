using MemoryAnalyzer.Core.Aggregation;
using MemoryAnalyzer.Core.Monitoring;

namespace MemoryAnalyzer.Core.Analysis;

public sealed class RecommendationEngine
{
    private const long OneGigabyte = 1024L * 1024 * 1024;

    public ApplicationAdvice Analyze(
        ApplicationSnapshot application,
        ApplicationTrend trend,
        SystemMemorySnapshot systemMemory)
    {
        if (application.IsProtected)
        {
            return new ApplicationAdvice(
                "Windowsの動作や安全を支えるプロセスです。",
                "操作せず、そのままにする",
                "終了しても安全な節約にはなりません。",
                "停止するとWindowsが不安定になる可能性があります。",
                SafetyLevel.DoNotOperate,
                0);
        }

        if (trend.IsGrowing)
        {
            return new ApplicationAdvice(
                $"監視中に{FormatBytes(trend.ChangeBytes)}増え続けています。",
                "作業を保存してからアプリを再起動する",
                $"現在使っている専用メモリは{FormatBytes(application.PrivateBytes)}です。",
                "未保存の作業や進行中の処理が失われる可能性があります。",
                SafetyLevel.CheckFirst,
                100);
        }

        if (application.Category == ApplicationCategory.Browser && application.WorkingSetBytes >= 700L * 1024 * 1024)
        {
            return new ApplicationAdvice(
                $"{application.ProcessCount}個の関連プロセスでメモリを使っています。",
                "使っていないタブとウィンドウを閉じる",
                $"現在のRAM使用量{FormatBytes(application.WorkingSetBytes)}の一部を戻せます。",
                "入力途中のページや再生中の内容が閉じる場合があります。",
                SafetyLevel.Safe,
                85);
        }

        if (application.Category == ApplicationCategory.Development && application.WorkingSetBytes >= OneGigabyte)
        {
            return new ApplicationAdvice(
                "開発用の処理がまとまった量のメモリを使っています。",
                "使っていない開発作業を終了する",
                $"現在のRAM使用量は{FormatBytes(application.WorkingSetBytes)}です。",
                "ビルドや解析が動いている場合は中断されます。",
                SafetyLevel.CheckFirst,
                80);
        }

        if (application.WorkingSetBytes >= OneGigabyte)
        {
            return new ApplicationAdvice(
                "このアプリだけで1GB以上のRAMを使っています。",
                "アプリ内の不要な画面や作業を閉じる",
                $"現在のRAM使用量は{FormatBytes(application.WorkingSetBytes)}です。",
                "閉じる内容によっては未保存データを失う可能性があります。",
                SafetyLevel.CheckFirst,
                70);
        }

        if (application.VisibleWindowCount == 0 && application.WorkingSetBytes >= 400L * 1024 * 1024)
        {
            return new ApplicationAdvice(
                "見えている画面がない状態でメモリを使っています。",
                "必要な常駐アプリか確認する",
                $"常駐を止めると、最大{FormatBytes(application.WorkingSetBytes)}程度を減らせる可能性があります。",
                "通知やバックグラウンド処理が止まる可能性があります。",
                SafetyLevel.Advanced,
                55);
        }

        var pressureHigh = systemMemory.CommitUsedPercent >= 80 || systemMemory.PhysicalUsedPercent >= 85;
        return new ApplicationAdvice(
            pressureHigh ? "システム全体は余裕が少なめですが、このアプリの優先度は低めです。" : "目立った増加や高い消費は見つかっていません。",
            "今は何もしない",
            "操作による節約効果は小さい見込みです。",
            "なし",
            SafetyLevel.Safe,
            pressureHigh ? 20 : 5);
    }

    private static string FormatBytes(long bytes)
    {
        var absolute = Math.Abs(bytes);
        if (absolute >= OneGigabyte)
        {
            return $"{bytes / (double)OneGigabyte:0.0}GB";
        }

        return $"{bytes / (1024d * 1024):0}MB";
    }
}
