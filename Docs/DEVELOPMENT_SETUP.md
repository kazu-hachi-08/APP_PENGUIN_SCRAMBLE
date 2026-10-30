# 開発環境・プロジェクトセットアップ手順書

2人開発で Unity 環境をそろえるための手順をまとめる。

---

## 1. Unityバージョン

- **バージョン**: `Unity 6000.6.2f1`（ペンモンと同じ。`ProjectSettings/ProjectVersion.txt` と同じものを使う）
- レンダーパイプライン: URP（2D）

## 2. 必要なUnityモジュール

- **Android Build Support**（Android SDK & NDK Tools / OpenJDK）
- **Web Build Support**（ブラウザ版）
- **Windows Build Support (IL2CPP)**（Windows版を IL2CPP で出す場合。Mono なら不要）

## 3. 初回のセットアップ（Unity プロジェクトの作成）

このリポジトリには、ペンモンから移植した共通基盤・リリーススクリプトなどが先に入っている（`ProjectSettings/` `Packages/` はまだ無い）。

1. Unity Hub で **Universal 2D** テンプレートの新規プロジェクトを、別の空フォルダ（例: `C:\MyApp\_tmp_scramble`）に作る
2. 作ったフォルダの `Assets/` `Packages/` `ProjectSettings/` をこのリポジトリへ移す（`Assets/` は中身をマージ）
3. Unity Hub の「Add」でこのリポジトリを開く
4. Player Settings
   - Company Name: `kazuhachi08` / Product Name: `乱戦！ペンモンスクランブル`（**一度配信したら変えない**。保存先が変わる）
   - Web > Resolution and Presentation > WebGL Template: `PenmonScramble`
   - Default Orientation: Landscape
5. 共通基盤が使うパッケージ: Input System（`VirtualJoystick` 等）、uGUI（Legacy Text）

## 4. ディレクトリ構成

```text
Assets/
├── _Project/
│   ├── Common/                       # 共通基盤（PenmonScramble.Common.* 名前空間）
│   │   ├── Scripts/                  # AppUpdate / Audio / Core / Input / Scene / UI
│   │   ├── Editor/                   # 共通ダイアログのシーン生成（UIDialogBuilder）
│   │   ├── Plugins/WebGL/            # ブラウザ版の表示枠の縦横比（WebScreen.jslib）
│   │   └── Resources/Fonts/          # M PLUS Rounded 1c（OFL）
│   └── Games/
│       └── 100_PenmonScramble/       # ゲーム本体
└── WebGLTemplates/PenmonScramble/    # ブラウザ版の index.html
Tools/Release/                        # Windows版・ブラウザ版のリリーススクリプト
```

## 5. リリース

### Windows版（GitHub Releases + 自動更新）

1. Unity で Windows 向けに `..\APP_PENGUIN_SCRAMBLE_EXE`（リポジトリの隣）へビルド
2. `Tools/Release/release-windows-test.bat` をダブルクリック（テスト版。`test-日時` タグで公開し、古いテスト版は消す）
   - 正式版は `.\Tools\Release\release-windows.ps1 -Version v0.1 -BuildDir ..\APP_PENGUIN_SCRAMBLE_EXE`
3. zip には `version.txt`・`update.ps1`・`PenmonScramble_Start.bat` が入る。配った版は次から自動で更新できる
   - ゲーム内: タイトル画面に `UpdateNotice`（Common/Scripts/AppUpdate）を置き、`CommonDialog` を割り当てる
   - 起動前: `PenmonScramble_Start.bat` から起動すると更新してから起動する

### ブラウザ版（GitHub Pages）

1. 初回だけ `Tools/Release/release-web.bat` を実行（公開用リポジトリ `penmon-scramble-web` の作成と clone を `..\penmon-scramble-web` に行う）
2. Unity で Web 向けに `..\penmon-scramble-web` へビルド
3. もう一度 `release-web.bat` を実行 → https://kazu-hachi-08.github.io/penmon-scramble-web/

前提: `gh` CLI をインストールし `gh auth login` 済み。

## 6. Git・ファイル管理ルール

- **`.meta` ファイル**: アセットを追加・変更したときに生成される `.meta` は**必ずセットでコミット**する。
- 移植したファイルの `.meta` はペンモンと GUID を共有しないよう消してある。初回に Unity で開くと作り直されるので、それをコミットする。
