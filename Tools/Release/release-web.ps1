# Webビルドを GitHub Pages（penguin-scramble-web リポジトリ）に公開する。
#
# 使い方（リポジトリのルートで実行）:
#   .\Tools\Release\release-web.ps1
#   .\Tools\Release\release-web.ps1 -Message "v0.2 を公開"
#
# 前提:
#   - UnityでWeb向けに -PagesDir（既定は <repo>\..\penguin-scramble-web）へ直接ビルド済みであること
#   - gh CLI がインストール済みで `gh auth login` 済みであること
#
# ビルド先と公開用リポジトリの clone を同じフォルダにして、コピーの手間とフォルダの二重管理をなくしている。
# 初回（フォルダがまだ無いとき）は公開用リポジトリの作成・clone だけ行うので、そのあとビルドしてもう一度実行する。
# ゲーム本体のリポジトリに数十MBのビルドを積まないよう、公開物は別リポジトリに置く。

param(
    [string]$PagesDir,
    [string]$Repo = "kazu-hachi-08/penguin-scramble-web",
    [string]$Message = "WebGL版を更新 " + (Get-Date -Format "yyyy-MM-dd HH:mm")
)

$ErrorActionPreference = "Stop"

# 存在確認だけしたい gh 呼び出し用。PowerShell 5.1 は native の stderr をリダイレクトすると
# エラー扱いにして Stop で止まってしまうので、この中だけ Continue にする
function Test-Quietly([scriptblock]$Command) {
    $ErrorActionPreference = "Continue"
    & $Command *> $null
    return ($LASTEXITCODE -eq 0)
}

# スクリプトの置き場所から辿ることで、どのフォルダで実行しても同じ結果にする
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$parentDir = Split-Path $repoRoot
if ([string]::IsNullOrEmpty($PagesDir)) { $PagesDir = Join-Path $parentDir ($Repo.Split("/")[1]) }
$pagesUrl = "https://" + $Repo.Split("/")[0] + ".github.io/" + $Repo.Split("/")[1] + "/"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "gh CLI が見つかりません。インストールして gh auth login してください。"
}

# 初回だけ公開用リポジトリを作って clone する（ビルドより先に clone しておかないと、中身のあるフォルダには clone できない）
if (-not (Test-Path $PagesDir)) {
    if (-not (Test-Quietly { gh repo view $Repo })) {
        Write-Host "公開用リポジトリを作成: $Repo"
        gh repo create $Repo --public --description "ペンギンスクランブル ブラウザ版"
        if ($LASTEXITCODE -ne 0) { Write-Error "リポジトリの作成に失敗しました。" }
    }
    git clone "https://github.com/$Repo.git" $PagesDir
    if ($LASTEXITCODE -ne 0) { Write-Error "clone に失敗しました。" }
    Write-Host "準備できました。Unity で $PagesDir に Web ビルドしてから、もう一度実行してください。"
    return
}
if (-not (Test-Path (Join-Path $PagesDir ".git"))) {
    Write-Error "$PagesDir が git リポジトリではありません。フォルダを消してからもう一度実行してください（clone し直します）。"
}
if (-not (Test-Path (Join-Path $PagesDir "index.html"))) {
    Write-Error "$PagesDir に index.html がありません。先にUnityで Web ビルドを出力してください。"
}

# GitHub Pages の Jekyll は _ で始まるファイルを無視するため、そのまま配信させる
New-Item -ItemType File -Path (Join-Path $PagesDir ".nojekyll") -Force | Out-Null

git -C $PagesDir add -A
git -C $PagesDir commit -m $Message
if ($LASTEXITCODE -ne 0) { Write-Error "コミットできませんでした（前回と同じビルドの可能性があります）。" }
git -C $PagesDir push -u origin HEAD:main
if ($LASTEXITCODE -ne 0) { Write-Error "push に失敗しました。" }

# Pages が未設定なら main のルートで公開する
if (-not (Test-Quietly { gh api "repos/$Repo/pages" })) {
    Write-Host "GitHub Pages を有効化"
    gh api -X POST "repos/$Repo/pages" -f "source[branch]=main" -f "source[path]=/" | Out-Null
}

Write-Host "公開完了（反映まで1〜2分かかる）: $pagesUrl"
