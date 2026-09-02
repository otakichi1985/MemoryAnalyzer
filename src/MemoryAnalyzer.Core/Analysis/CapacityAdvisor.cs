using MemoryAnalyzer.Core.Monitoring;
using MemoryAnalyzer.Core.History;

namespace MemoryAnalyzer.Core.Analysis;

public sealed class CapacityAdvisor
{
    public CapacityAssessment Assess(
        SystemMemorySnapshot memory,
        TimeSpan highPressureDuration,
        long actionableWorkingSetBytes,
        MemoryPressureHistorySummary? history = null)
    {
        var immediateRisk = memory.CommitUsedPercent >= 97
            || (memory.CommitUsedPercent >= 95 && memory.PhysicalAvailableBytes <= 1024L * 1024 * 1024);
        var severePressure = memory.PhysicalAvailablePercent < 10 && memory.CommitUsedPercent >= 90;
        var sustained = highPressureDuration >= TimeSpan.FromMinutes(5);
        var appActionsCouldMatter = memory.PhysicalTotalBytes > 0
            && actionableWorkingSetBytes >= memory.PhysicalTotalBytes * 0.15;

        if (immediateRisk)
        {
            return new CapacityAssessment(
                CapacityStatus.CriticalAction,
                "すぐに重いアプリを閉じる",
                "Windowsが新しく確保できるメモリが尽きかけています。増設判断を待つ段階ではなく、保存して重いアプリを閉じるのが先です。必要な作業を同時に続けたい場合は、その後にRAM増設を検討します。");
        }

        if (severePressure && sustained && !appActionsCouldMatter)
        {
            return new CapacityAssessment(
                CapacityStatus.ConsiderUpgrade,
                "RAM増設を検討する目安です",
                "空きRAMが少ない状態が5分以上続き、アプリ整理だけでは大きく戻りにくい見込みです。必要なアプリを同時に使いたいなら増設が有効です。");
        }

        if (severePressure && sustained)
        {
            return new CapacityAssessment(
                CapacityStatus.AppActionFirst,
                "まずアプリを整理する",
                "閉じてもよい候補がまとまった量のメモリを使っています。必要なアプリを閉じたくない場合は、増設も現実的な選択です。");
        }

        if (severePressure)
        {
            return new CapacityAssessment(
                CapacityStatus.Observe,
                "RAM増設が必要か確認中",
                "一時的な負荷か確認しています。この状態が5分以上続くかを見てから判断します。");
        }

        if (history?.HasCriticalPressure == true)
        {
            return new CapacityAssessment(
                CapacityStatus.ConsiderUpgrade,
                "RAMの増設をおすすめします",
                $"過去7日に危険域を{history.CriticalSampleCount}回記録しました。現在が落ち着いていても、同じアプリを同時に使い続けたいならRAM増設が有効です。直近の危険域は {history.MostRecentCriticalAt: M/d HH:mm} です。");
        }

        if (memory.PhysicalAvailablePercent < 20 || memory.CommitUsedPercent >= 88)
        {
            return new CapacityAssessment(
                CapacityStatus.Observe,
                "しばらく様子を見る",
                "メモリ不足とまでは言えません。空きRAMが10%未満へ下がり続けるかを確認します。");
        }

        return new CapacityAssessment(
            CapacityStatus.Comfortable,
            "増設を急ぐ状態ではない",
            "現在は再利用可能なメモリが残っています。まずは重いアプリが本当に必要か確認すれば十分です。");
    }
}
