# ステージの絵の作り方（§9）

ペンギン20種類・大ボス3体・地面3枚を ComfyUI で作り、`Assets/_Project/Games/100_PenmonScramble/Art/` へ置く。

1. Comfy Desktop を起動する（モデル `DreamShaperXL_Turbo_v2_1`。8 steps / cfg 2.0 / dpmpp_sde karras）
2. `python gen.py all`（1枚1分ほど。元絵は `raw/` に出る。git には入れない）
   - 1枚だけ作り直す: `python gen.py p07` / seed を変える: `python gen.py p07:1234`（決めた seed は `gen.py` の `SEEDS` へ）
   - プロンプトは `gen.py` の `PENGUINS` / `BOSSES` / `GROUNDS`
3. 背景の透過（初回だけ `python -m venv .venv` → `.venv\Scripts\pip install "rembg[cpu]" scipy`）
   - `.venv\Scripts\python cutout.py all`（1枚だけなら `cutout.py p07`）
   - 左を向いた絵は `cutout.py` の `FLIP` に足す（ゲームの絵は右向き）
4. Unity で `Tools/PenmonScramble/Rebuild`

メモ: Intel Arc（内蔵）はメモリが足りないとき失敗する。`gen.py` はモデルを降ろして1回やり直す。それでもだめなら Comfy Desktop を再起動する。
