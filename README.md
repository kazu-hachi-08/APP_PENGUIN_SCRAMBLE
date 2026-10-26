# ペンギンスクランブル

（ゲームの説明はこれから書く）

## ▶ ブラウザで遊ぶ

**https://kazu-hachi-08.github.io/penguin-scramble-web/**（初回公開後に有効）

* PCのブラウザ（Chrome / Edge 推奨）で開く。横画面のゲームなので、スマホは横向きで

## ▶ Windows版をダウンロード

**https://github.com/kazu-hachi-08/APP_PENGUIN_SCRAMBLE/releases/latest**

* zipを展開して exe を起動する
* 初回に「WindowsによってPCが保護されました」と出たら「詳細情報 → 実行」で起動できる
* 2回目からはダウンロードし直さなくてよい。新しい版が出るとタイトル画面に「更新する」が出て、押すと自動で入れ替わって開き直す（`PenguinScramble_Start.bat` から起動すると、起動する前に更新する）
* 公開手順: `Tools/Release/release-windows.ps1` の先頭コメントを参照

---

## ドキュメント

| ドキュメント | 内容 |
| ---- | ---- |
| [Docs/DEVELOPMENT_SETUP.md](Docs/DEVELOPMENT_SETUP.md) | 開発環境のセットアップ・リリース手順 |
| [CLAUDE.md](CLAUDE.md) | AI 開発ガイドライン |

## 技術スタック

| 項目 | 内容 |
| ---- | ---- |
| ゲームエンジン | Unity 6000.6.2f1（URP 2D） |
| 言語 | C# |
| 対象プラットフォーム | Android / Windows / ブラウザ（WebGL）。横画面固定 |
| ソース管理 | GitHub |
| 開発人数 | 2人 |

## ディレクトリ構成

```text
Assets/_Project/
├── Common/                       # 共通基盤（Scene / Input / UI / Audio / AppUpdate）。ペンモンから移植
└── Games/100_PenguinScramble/    # ゲーム本体（これから作る）
Assets/WebGLTemplates/PenguinScramble/  # ブラウザ版の index.html
Docs/                             # 仕様書・開発手順
Tools/Release/                    # Windows版・ブラウザ版のリリーススクリプト
.claude/skills/                   # Claude Code 用のプロジェクトスキル
```

---

## 開発ルール

1. 小さく作る
2. 早く遊べる状態にする
3. 毎週末 Playable Build を残す
4. 必要以上に仕様を増やさない

### Git運用

```text
main
├── feature/#xx   # Issue番号でブランチを切る
└── fix/xxx
```

featureブランチで実装 → 動作確認 → Pull Request → レビュー → main へ Merge

### Unity開発時の注意

* アセットを追加したら `.meta` も必ず一緒にコミットする
