# 地面の絵の作り方（§9）

ワールドの地面5枚（`Art/Stage/World1〜5.png`）と、闘技場の床5枚（`Art/Stage/Arena1〜5.png`。楕円を囲む四角に貼るので、まん中を中心にした模様）、ボスラッシュのコロシアムの床1枚（`Art/Stage/Colosseum.png`。明るい石の舞台。ゲームの中でステージごとにワールドの色を掛ける）を ComfyUI で作る。ペンギン・大ボスは3Dのモデルで描くので絵は要らない（一覧・ガチャの顔も Rebuild のときに3Dのモデルを撮って作る）。タイトルの背景も3Dのペンギンが動くジオラマ、アプリのアイコンも3Dのモデルを撮って作るので、絵は要らない。

1. Comfy Desktop を起動する（モデル `DreamShaperXL_Turbo_v2_1`。8 steps / cfg 2.0 / dpmpp_sde karras）
2. `python gen.py all`（1枚1分ほど。元絵は `raw/` に出て、同じ絵が `Assets/_Project/Games/100_PenmonScramble/Art/Stage/` に置かれる。`raw/` は git に入れない）
   - 1枚だけ作り直す: `python gen.py w2`（闘技場は `a2`） / seed を変える: `python gen.py w2:1234`（決めた seed は `gen.py` の `SEEDS` へ）
   - プロンプトは `gen.py` の `GROUNDS`（地面）・`ARENAS`（闘技場）・`COLOSSEUM`（コロシアム。`python gen.py c1`）
3. Unity で `Tools/PenmonScramble/Rebuild`

絵が無いワールドは、前のワールドの絵を色替えして代わりに敷く（`StageTheme` の `FallbackArtWorld`）。コロシアムの絵が無いときは、ストーリーの闘技場の床で代わりにする。

メモ: Intel Arc（内蔵）はメモリが足りないとき失敗する。`gen.py` はモデルを降ろして1回やり直す。それでもだめなら Comfy Desktop を再起動する。
