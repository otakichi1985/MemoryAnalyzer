param(
    [string]$ArtifactsDir = "",
    [switch]$KeepOpen
)

# Release版を実際に起動し、画面が表示されて主要な文字が読めるかを自動確認するスモークテスト。
# 使い方: pwsh -File scripts\verify-gui.ps1
$ErrorActionPreference = "Stop"

if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw "PowerShell 7 (pwsh) で実行してください。"
}

$Root = Split-Path $PSScriptRoot -Parent
$AppPath = Join-Path $Root "dist\MemoryAnalyzer\MemoryAnalyzer.App.exe"
$AgentPath = Join-Path $Root "dist\MemoryAnalyzer\MemoryAnalyzer.Agent.exe"
if ([string]::IsNullOrEmpty($ArtifactsDir)) { $ArtifactsDir = Join-Path $Root "verify\artifacts" }
New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Drawing

# 期待する表示の一部（これが読めれば画面が描画されている）
$Expected = @("MEMORY ANALYZER", "空きRAM", "CAPACITY CHECK", "現在使用中のRAM", "起動中のプロセス・アプリ")

$Failures = [System.Collections.Generic.List[string]]::new()

Write-Host "1. 既存プロセスを終了中..."
Get-Process -Name "MemoryAnalyzer.App", "MemoryAnalyzer.Agent" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "2. Release版を起動中: $AppPath"
$App = Start-Process -FilePath $AppPath -PassThru

Write-Host "3. メインウィンドウの出現を待機中..."
$windowHandle = [IntPtr]::Zero
$deadline = (Get-Date).AddSeconds(20)
while ((Get-Date) -lt $deadline) {
    if ($App.HasExited) { break }
    $App.Refresh()
    if ($App.MainWindowHandle -ne [IntPtr]::Zero) { $windowHandle = $App.MainWindowHandle; break }
    Start-Sleep -Milliseconds 250
}
if ($windowHandle -eq [IntPtr]::Zero) {
    $Failures.Add("メインウィンドウが20秒以内に表示されませんでした（アプリは終了済み: $($App.HasExited)）。")
} else {
    $element = [System.Windows.Automation.AutomationElement]::FromHandle($windowHandle)
    $element.SetFocus()

    # 4. データ読み込み完了まで、期待文字列の出現を待機
    $condition = [System.Windows.Automation.Condition]::TrueCondition
    $foundSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline) {
        $all = $element.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
        foreach ($node in $all) {
            $name = $node.Current.Name
            if (-not [string]::IsNullOrWhiteSpace($name)) { [void]$foundSet.Add($name) }
        }
        $missing = @($Expected | Where-Object { -not $foundSet.Contains($_) })
        if ($missing.Count -eq 0) { break }
        Start-Sleep -Milliseconds 500
    }

    # 5. ウィンドウ全体をスクリーンショットへ保存
    $rect = $element.Current.BoundingRectangle
    if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
        $left = [int][Math]::Floor($rect.Left)
        $top = [int][Math]::Floor($rect.Top)
        $width = [int][Math]::Ceiling($rect.Width)
        $height = [int][Math]::Ceiling($rect.Height)
        $bitmap = New-Object System.Drawing.Bitmap($width, $height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($left, $top, 0, 0, $bitmap.Size)
        $shotPath = Join-Path $ArtifactsDir "dashboard.png"
        $bitmap.Save($shotPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()
        Write-Host "   スクリーンショット保存: $shotPath"
    } else {
        $Failures.Add("ウィンドウの矩形を取得できませんでした。")
    }

    # 6. 収集したテキストと期待文字列を照合
    Write-Host "   収集したテキスト要素数: $($foundSet.Count)"
    foreach ($expected in $Expected) {
        if ($foundSet.Contains($expected)) { Write-Host "   OK: $expected" }
        else { $Failures.Add("画面に「$expected」が見つかりませんでした。"); Write-Host "   NG: $expected" }
    }
}

Write-Host "6. 後片付け..."
if (-not $KeepOpen) {
    Get-Process -Name "MemoryAnalyzer.App", "MemoryAnalyzer.Agent" -ErrorAction SilentlyContinue | Stop-Process -Force
}

if ($Failures.Count -gt 0) {
    Write-Host ""
    Write-Host "検証失敗:" -ForegroundColor Red
    $Failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
Write-Host ""
Write-Host "GUIスモークテスト成功: 起動・描画・主要テキストの表示を確認しました。"