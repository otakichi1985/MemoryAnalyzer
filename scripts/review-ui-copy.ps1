[CmdletBinding()]
param(
    [string]$SourceRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "src\MemoryAnalyzer.App")
)

$sharedTool = Join-Path (Split-Path -Parent $PSScriptRoot) "context\workflows\tools\review-ui-copy.ps1"
if (-not (Test-Path -LiteralPath $sharedTool)) {
    throw "共通のUI文言レビュー工具が見つかりません: $sharedTool"
}

& $sharedTool -SourceRoot $SourceRoot
exit $LASTEXITCODE
