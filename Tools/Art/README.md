# 地面の絵の作り方（§9）

ワールドの地面3枚（`Art/Stage/World1〜3.png`）を ComfyUI で作る。ペンギン・大ボスは3Dのモデルで描くので絵は要らない（一覧・ガチャの顔も Rebuild のときに3Dのモデルを撮って作る）。タイトルの背景（`Art/Title/TitleBackground.png`）は1枚絵をそのまま置いている。

1. Comfy Desktop を起動する（モデル `DreamShaperXL_Turbo_v2_1`。8 steps / cfg 2.0 / dpmpp_sde karras）
2. `python gen.py all`（1枚1分ほど。元絵は `raw/` に出て、同じ絵が `Assets/_Project/Games/100_PenmonScramble/Art/Stage/` に置かれる。`raw/` は git に入れない）
   - 1枚だけ作り直す: `python gen.py w2` / seed を変える: `python gen.py w2:1234`（決めた seed は `gen.py` の `SEEDS` へ）
   - プロンプトは `gen.py` の `GROUNDS`
3. Unity で `Tools/PenmonScramble/Rebuild`

メモ: Intel Arc（内蔵）はメモリが足りないとき失敗する。`gen.py` はモデルを降ろして1回やり直す。それでもだめなら Comfy Desktop を再起動する。
