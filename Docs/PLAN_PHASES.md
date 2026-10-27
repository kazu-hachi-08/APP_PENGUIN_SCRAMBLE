# 実装計画: フェーズ分け

> 仕様は `PENMON_SCRAMBLE_SPEC.md`。各フェーズの終わりに「遊べる状態」にする。
> オンラインは後から足すと作り直しになりやすいので、土台ができたらすぐ（フェーズ2で）入れる。

## 全体

| フェーズ | 遊べるようになること | 目安 |
| --- | --- | --- |
| 0 | Unity プロジェクトが3つの環境でビルドできる。ComfyUI で絵が1枚作れるか分かる | 週末1回 |
| 1 | ソロで1エリア。仮の丸いペンギンを動かして、敵を倒せる | 週末1〜2回 |
| 2 | 同じ1エリアを、オンラインの4人で遊べる | 週末2回 |
| 3 | 20種類・技の7つの形・3体交代・必殺技 | 週末2回 |
| 4 | 1ステージを通しで遊べる（エリア進行・ボス・結果・ダウンと復活） | 週末1〜2回 |
| 5 | 合体技と吹っ飛び連鎖 | 週末1回 |
| 6 | ガチャ・コイン・ペンギン一覧・編成・セーブ | 週末2回 |
| 7 | 3ワールド×3ステージ、大ボス3体、絵と音の差し替え | 週末2〜3回 |
| 8 | 調整とリリース（Android / Windows / ブラウザ） | 週末1〜2回 |

## フェーズ0: 準備

- [x] Unity 6000.6.2f1 でプロジェクトを作る（`Packages/`・`ProjectSettings/` がまだ無い）。ペンモンと同じパッケージを入れる: Input System / Netcode for GameObjects / Multiplayer Services / URP（2D）
- [x] Player Settings: Company Name・Product Name・Android のパッケージ名を決める（**後から変えるとセーブの保存先が変わるので、ここで決めて固定する**）→ `kazuhachi08` / `乱戦！ペンモンスクランブル` / `com.kazuhachi08.penmonscramble`
- [x] ペンモンの `Common/Scripts/Online/`（`OnlineSession` など）を移植し、名前空間を `PenmonScramble.Common.Online` にする（使っている `Profile/` も一緒に移植。Unity Cloud のプロジェクトは未リンク → エディタの Project Settings > Services で新しく作る）
- [x] asmdef を作る: `PenmonScramble.Core`（純C#・`noEngineReferences: true`）/ `PenmonScramble.Game`（Unity 側）/ `Tests/Editor`。Game から Common を参照するため、Common にも `PenmonScramble.Common`（＋`.Common.Editor`）を作った
- [x] 空のシーンを SceneBuilder で作り、Android・Windows・WebGL でビルドできることを確かめる（`Tools/PenmonScramble/Build/*` で `Builds/` へ出力。実機での起動は未確認）
- [ ] Comfy Desktop でペンギンを1枚生成してみる（Intel Arc で動くか、1枚に何分かかるか）。動かなければドット絵に切り替える

**完了の確認**: 3つの環境で空のシーンが起動する。絵の作り方（ComfyUI / ドット絵）が決まっている。

## フェーズ1: ソロで1エリア

純C#（Core）:

- [x] `StageWorld`: 毎フレームの更新（固定の時間刻み 1/30秒）。ペンギン・敵・技の弾を持つ
- [x] 円の当たり判定と押し合い、壁（エリアの四角）
- [x] 移動・向き・HP・ダメージ・吹っ飛び（速さと減速）
- [x] 技の形を2つだけ: 近接・弾
- [x] 敵: 近づいて近接で殴るだけの AI
- [x] イベント（`Hit` / `Defeated` / `SkillUsed`）を外へ出す
- [x] テスト: 当たり判定・ダメージ・吹っ飛び（`StageWorldTests`）

Unity 側（Game / View）:

- [x] 入力 → `PlayerInput`（移動の向き・ボタン）に変換する（タッチ・キーボード・コントローラー）。共通の `InputManager` にコントローラーと Action1〜4 を足した
- [x] 仮の見た目（色つきの丸＋向きの三角）で描く。奥を後ろに描く
- [x] ダメージ数字・被弾で白く光る
- [ ] 実機（スマホ・PC・コントローラー）で遊んで確かめる

仮の数値（ペンギン・技・敵の数）は `Core/Stage/TrialStage.cs`。ペンギン・技はフェーズ3で定義表（`SpeciesTable`）に置き換えた。敵の数・出方はフェーズ4でステージ定義（`StageTable`）に置き換えた。

**完了の確認**: スマホ・PC・コントローラーで動かして、敵を全部倒せる。

## フェーズ2: オンライン協力（1エリア）

- [x] タイトルに「ひとりで遊ぶ」「部屋を作る」「コードで参加」（最大4人）。ホストが開始（共通の `ModeSelectPanel.ShowMultiplayer`。見た目は `SceneBuilder.Title.cs`）
- [x] ゲスト → ホスト: `PlayerInput` を送る（変わったときだけ。ホストは `InputLatch` で短いタップを取りこぼさない）
- [x] ホスト → 全員: 位置・HP・イベントを毎秒15回送る（`StageSnapshot` を `StageCodec` でバイト列にして、NGO の CustomMessagingManager で。`Game/Online/StageLink.cs`）
- [x] ゲストは、届いた位置の間を補間して描く（ホスト・ソロも同じスナップショットから描く。`StageView.Apply`）
- [x] 人数で敵の HP を変える（`Balance.EnemyHpScale`）
- [x] 切断: ゲストが抜けたら消す（`OnlineSession.ContinueWhenMemberLeaves` / `OnMemberLeft`）/ ホストが抜けたらタイトルへ
- [x] ソロも同じ流れで動く（通信しないホスト = `StageHost` の link が null）
- [x] テスト: スナップショット・入力の変換、イベントの取り出し、人数と敵の HP（`StageNetTests` など）
- [ ] Unity Cloud のプロジェクトをリンクする（Project Settings > Services。未リンクだと部屋を作れない）
- [ ] 実機（Windows＋スマホ＋ブラウザ）で協力を試す

**完了の確認**: 2台以上（Windows＋スマホ＋ブラウザ）で同じエリアを遊べる。ゲストの遅れが遊べる程度か判断する（ダメなら、自分の移動を手元で先に動かす処理を足す）。

## フェーズ3: ペンギンと技

- [x] 種類の定義表（20種類。§5.3）と技の定義表（40個）をコードで持つ（`Core/Penguins/SpeciesTable.cs`。必殺技は `SpecialMoves.cs`）
- [x] 技の形の残り5つ: 突進・拡散・周囲・設置・落下（`StageWorld.Areas.cs` / `StageWorld.Projectiles.cs`。地面の範囲は `AreaEffect`）
- [x] 3体の編成と交代（交代できない時間・倒れたら次の子）（`Unit` が `PartyMember` を3つ持つ。`StageWorld.Party.cs`）。編成画面ができるまでは `TrialStage.RotatingParty` で挑戦ごとに3体が替わる
- [x] 必殺技ゲージと、属性ごとの必殺技4種類（カットイン・画面の揺れ・ヒットストップ）（ヒットストップは Core で全体を止める。`SpecialCutIn` / `CameraShake`）
- [x] 敵の見た目を種類ごとにする（暗い色）（`PenguinPalette`）
- [x] テスト: 技の形ごとの当たり方・交代・ゲージ（`SkillShapeTests` / `PartyTests` / `SpeciesTableTests`）
- [x] 入力: 必殺技（L / 北 / 右上のボタン）と交代（Space / LB / 右下の小ボタン）。通信の版を 2 に上げた（`StageCodec.FormatVersion`）
- [ ] 実機で20種類を触って、技の数値（威力・待ち時間・範囲）を調整する

**完了の確認**: 20種類のどれでも遊べて、技がそれぞれ違って見える。

## フェーズ4: 1ステージ通し

- [x] ステージ定義（エリアの数・各エリアの敵の出方）をコードで持つ（`Core/Stage/StageDef.cs` / `StageTable.cs`。1ステージ目だけ）
- [x] エリアの出入口が閉じる → 敵が何回かに分けて出る → 全部倒すと開く → カメラが次のエリアへ（`StageWorld.Progress.cs`。出入口は動ける範囲を広げ・縮めるだけ。カメラは `StageCamera`、地面と出入口は `StageView.Ground.cs`）
- [x] ステージのボス（強い野生ペンギン。大きく表示）（`SpeciesTable.BossStats`。HUD の上の真ん中に HP）
- [x] ダウンと復活（協力）・全員ダウンで失敗（`StageWorld.Revive.cs`）
- [x] 結果画面（クリア / 失敗・倒した数）
- [x] テスト: エリアの進み方・復活・失敗の判定（`StageFlowTests` / `ReviveTests`）。通信の版を 3 に上げた
- [ ] 実機で1ステージを通して、長さ（3〜5分）と敵の数・ボスの強さを調整する

**完了の確認**: 1ステージを最初から最後まで、ソロと協力で遊べる。

## フェーズ5: 合体技と連鎖

- [x] 吹っ飛んだ敵が別の敵に当たると、ダメージを与えて飛ばす（連鎖）（`StageWorld.Chain.cs`。数値は `Balance.Chain*`）
- [x] 合体技: 別の人の技が1秒以内に同じ敵へ → 属性の組み合わせで7種類（`StageWorld.Combo.cs`・`ComboMoves.cs`。雷雲は動く範囲 `AreaEffectKind.Cloud`）
- [x] 「合体！○○」の文字とエフェクト（文字は HUD の真ん中の知らせ `StageHud.Progress.cs`、輪と揺れは `StageView.Effects.cs`）。通信の版を 4 に上げた
- [x] テスト: 合体が起きる条件・起きない条件（同じ人の技・1秒を過ぎた・2秒の待ち・合体技のダメージ）と連鎖（`ComboTests` / `ChainTests`）
- [ ] 実機の協力で、合体技の起きやすさ・強さと連鎖の気持ちよさを調整する

**完了の確認**: 協力で合体技が自然に起きて、盛り上がる。

## フェーズ6: ガチャと記録

- [ ] セーブデータ（バージョン入り・読めない記録は退避）とテスト
- [ ] 初回の無料ガチャ3回
- [ ] ガチャ画面（★の演出）・コイン
- [ ] ペンギン一覧（並べ替え・売る）
- [ ] 編成画面（3体を選ぶ）
- [ ] ステージの解放とクリア済み（協力ではホストのクリアを全員の記録に書く）

**完了の確認**: 遊ぶ → コイン → ガチャ → 強い個体で次へ、が回る。アプリを再起動しても記録が残る。

## フェーズ7: 中身をそろえる

- [ ] 9ステージの定義
- [ ] 大ボス3体（トウゾクカモメ・ヒョウアザラシ・シャチ）と、攻撃の前の赤い範囲
- [ ] 背景3種類
- [ ] 絵の差し替え（ペンギン20種類・ボス3体）
- [ ] BGM・効果音（ペンモンの `GameMusic.Synth` を移植して、楽譜から作る）

**完了の確認**: ワールド3の大ボスまで通しでクリアできる。

## フェーズ8: 調整とリリース

- [ ] 数値の調整（パワーの範囲・コイン・敵の強さ）
- [ ] `Tools/Release/` のスクリプトでテスト版を配って遊んでもらう
- [ ] Android / Windows / ブラウザで公開
