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

## 3. 初回のセットアップ

このリポジトリには `ProjectSettings/` `Packages/` も含めて一式入っているので、Unity Hub で開けばそのまま使える。

1. Unity Hub の「Add」でこのリポジトリを開く
2. 確認する設定（Player Settings）
   - Company Name: `kazuhachi08` / Product Name: `乱戦！ペンモンスクランブル`（**一度配信したら変えない**。保存先が変わる）
   - Web > Resolution and Presentation > WebGL Template: `PenmonScramble`
   - Default Orientation: Auto Rotation（許可は左右の横向きだけ。起動時に `ScreenOrientationApplier` が横へ固定する）
3. 共通基盤が使うパッケージ: Input System（`VirtualJoystick` 等）、uGUI（Legacy Text）

## 4. ディレクトリ構成

```text
Assets/
├── _Project/
│   ├── Common/                       # 共通基盤（PenmonScramble.Common.* 名前空間）
│   │   ├── Scripts/                  # AppUpdate / Audio / Core / Input / Online / Profile / Scene / UI
│   │   ├── Editor/                   # 共通ダイアログのシーン生成（UIDialogBuilder）
│   │   ├── Plugins/WebGL/            # ブラウザ用の jslib（WebScreen: 表示枠の縦横比 / NameInput: 名前入力 / WebBrowser: ブラウザを開く）
│   │   └── Resources/Fonts/          # M PLUS Rounded 1c（OFL）
│   └── Games/
│       └── 100_PenmonScramble/       # ゲーム本体
│           ├── Art/                  # 絵（Icon / Portraits / Stage / Title / UI）
│           ├── Editor/               # エディタ拡張（3D プレビュー・ビルド・ベンチマーク）
│           ├── Scenes/               # ゲームのシーン（PenmonScrambleScene / Preview3D）
│           ├── Scripts/              # Core / Game
│           ├── Settings/             # URP の設定アセット
│           ├── Shaders/              # Toon / BlobShadow / Fx
│           └── Tests/                # EditMode テスト
└── WebGLTemplates/PenmonScramble/    # ブラウザ版の index.html
Tools/
├── Art/                              # 絵の生成スクリプト（ComfyUI で地面の絵を作る gen.py。元絵は raw/）
└── Release/                          # Windows版・ブラウザ版のリリーススクリプト
    └── Windows/                      # 配布 zip に入れる PenmonScramble_Start.bat・update.ps1
```

## 5. リリース

### Windows版（GitHub Releases + 自動更新）

1. Unity で `Tools > PenmonScramble > Release > Windows`（リポジトリの隣の `..\APP_PENGUIN_SCRAMBLE_EXE` へビルドする）
2. `Tools/Release/release-windows-test.bat` をダブルクリック（テスト版。`test-日時` タグで公開し、古いテスト版は消す）
   - 正式版は `.\Tools\Release\release-windows.ps1 -Version v0.1`
3. zip には `version.txt`・`update.ps1`・`PenmonScramble_Start.bat` が入る。配った版は次から自動で更新できる
   - ゲーム内: タイトル画面に「新しい版が あります → 更新する」が出る（`UpdateNotice`。SceneBuilder がタイトルに置く）
   - 起動前: `PenmonScramble_Start.bat` から起動すると更新してから起動する

### ブラウザ版（GitHub Pages）

1. 初回だけ `Tools/Release/release-web.bat` を実行（公開用リポジトリ `penmon-scramble-web` の作成と clone を `..\penmon-scramble-web` に行う）
2. Unity で `Tools > PenmonScramble > Release > Web`（`..\penmon-scramble-web` へビルドする。1 の clone が無いときはビルドせずに止まる）
3. もう一度 `release-web.bat` を実行 → https://kazu-hachi-08.github.io/penmon-scramble-web/

前提: `gh` CLI をインストールし `gh auth login` 済み。

## 6. Git・ファイル管理ルール

- **`.meta` ファイル**: アセットを追加・変更したときに生成される `.meta` は**必ずセットでコミット**する。
