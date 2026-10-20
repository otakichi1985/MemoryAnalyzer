[CmdletBinding()]
param(
    [string]$SourceRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "src\MemoryAnalyzer.App")
)

# 2026-10-20: context\workflows のジャンクションを廃止したため、共有の道具を直接指す
$sharedTool = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "_shared\workflows\tools\review-ui-copy.ps1"
if (-not (Test-Path -LiteralPath $sharedTool)) {
    throw "共通のUI文言レビュー工具が見つかりません: $sharedTool"
}

& $sharedTool -SourceRoot $SourceRoot
exit $LASTEXITCODE
