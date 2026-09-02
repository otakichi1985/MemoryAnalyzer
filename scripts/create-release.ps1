param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notes = ""
)

# 新しい版をビルドしてGitHub Releaseへ公開する手順。
# 使い方: powershell -File scripts\create-release.ps1 -Version 1.2.0
$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent
$Csproj = Join-Path $Root "src\MemoryAnalyzer.App\MemoryAnalyzer.App.csproj"
$DistDir = Join-Path $Root "dist\MemoryAnalyzer"
$Repo = "otakichi1985/MemoryAnalyzer"

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version は x.y.z 形式で指定してください。"
}

# 1. バージョンをcsprojへ反映
$Content = Get-Content -LiteralPath $Csproj -Raw
$Content = $Content -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
Set-Content -LiteralPath $Csproj -Value $Content -NoNewline

# 2. Releaseビルドを公開
& dotnet publish (Join-Path $Root "src\MemoryAnalyzer.App\MemoryAnalyzer.App.csproj") `
    -c Release --no-restore -o $DistDir
if ($LASTEXITCODE -ne 0) { throw "publish に失敗しました。" }

# 3. zipを作成
$ZipName = "MemoryAnalyzer-$Version.zip"
$ZipPath = Join-Path $Root "dist\$ZipName"
if (Test-Path -LiteralPath $ZipPath) { Remove-Item -LiteralPath $ZipPath -Force }
Compress-Archive -Path (Join-Path $DistDir "*") -DestinationPath $ZipPath

# 4. 改ざん・破損を検出するためのチェックサムを作成
$Hash = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$ChecksumPath = "$ZipPath.sha256"
Set-Content -LiteralPath $ChecksumPath -Value $Hash -NoNewline

Write-Host "zip: $ZipPath"
Write-Host "sha256: $Hash"

# 5. バージョン変更をコミットし、タグを付けてGitHubへ公開
git -C $Root add $Csproj
git -C $Root commit -m "v$Version"
git -C $Root push origin main
git -C $Root tag "v$Version"
git -C $Root push origin "v$Version"

if ([string]::IsNullOrEmpty($Notes)) {
    $Notes = "Memory Analyzer $Version"
}
& gh release create "v$Version" $ZipPath $ChecksumPath `
    --repo $Repo `
    --title "Memory Analyzer v$Version" `
    --notes $Notes

Write-Host "リリース完了: https://github.com/$Repo/releases/tag/v$Version"
