# Windows版の自動更新。GitHub Releases の最新版と、手元の version.txt（公開した版の名前）を比べ、
# 違えば zip を落としてこのフォルダに上書きし、ゲームを起動する。
# release-windows.ps1 が exe と同じフォルダに入れて配る。呼ばれ方は2つ:
#   PenguinScramble_Start.bat から: 起動する前に更新を確かめる
#   ゲームの「更新する」から: -WaitPid <ゲームのプロセス番号> 付き。動いている exe は上書きできないので、閉じるのを待つ
# ゲームの記録はレジストリにあるので、フォルダを上書きしても消えない

param([int]$WaitPid = 0)

$ErrorActionPreference = "Stop"
# 5.1 の Invoke-WebRequest は進み具合の表示を出すと極端に遅くなる
$ProgressPreference = "SilentlyContinue"
# 古い Windows の既定のままだと GitHub（TLS 1.2 のみ）につながらない
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$dir = $PSScriptRoot
$latestApi = "https://api.github.com/repos/kazu-hachi-08/APP_PENGUIN_SCRAMBLE/releases/latest"
$versionFile = Join-Path $dir "version.txt"

function Install-Release($release) {
    $asset = $release.assets | Where-Object name -like "*.zip" | Select-Object -First 1
    if (-not $asset) { return }
    Write-Host "新しい版 $($release.tag_name) をダウンロードしています..."
    $work = Join-Path $env:TEMP "PenguinScrambleUpdate"
    if (Test-Path $work) { Remove-Item $work -Recurse -Force }
    New-Item -ItemType Directory -Path $work | Out-Null
    $zip = Join-Path $work $asset.name
    Invoke-WebRequest $asset.browser_download_url -OutFile $zip
    # 別の場所で展開し終えてから写す。ダウンロードや展開で失敗しても、今のフォルダは前の版のまま遊べるように
    $files = Join-Path $work "files"
    Expand-Archive $zip -DestinationPath $files
    Copy-Item (Join-Path $files "*") $dir -Recurse -Force
    Remove-Item $work -Recurse -Force
    Write-Host "更新しました。"
}

function Start-Game {
    $exe = Get-ChildItem $dir -Filter *.exe | Where-Object Name -notlike "UnityCrashHandler*" | Select-Object -First 1
    if ($exe) { Start-Process $exe.FullName -WorkingDirectory $dir }
}

if ($WaitPid) { Wait-Process -Id $WaitPid -ErrorAction SilentlyContinue }

try {
    Write-Host "更新を確かめています..."
    $release = Invoke-RestMethod $latestApi -Headers @{ "User-Agent" = "PenguinScramble" }
    $local = if (Test-Path $versionFile) { (Get-Content $versionFile -Raw).Trim() } else { "" }
    if ($release.tag_name -ne $local) { Install-Release $release }
} catch {
    # オフラインや GitHub の回数制限でも遊べるように、今の版のまま起動する
    Write-Host "更新できませんでした（今の版で起動します）: $_"
    Start-Sleep -Seconds 3
}
Start-Game
