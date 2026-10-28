"""gen.py の元絵（raw/）の背景を透過し、余白を切り詰めた 512 四方の PNG にして Art/ へ置く。
rembg が要る: python -m venv .venv && .venv\Scripts\pip install "rembg[cpu]" scipy
使い方: .venv\Scripts\python cutout.py all
  左を向いている絵は FLIP に入れる（ゲームの絵は右向き。§9）
"""
import os
import sys
from collections import deque
import numpy as np
from PIL import Image
from rembg import new_session, remove
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
ART = os.path.join(HERE, "..", "..", "Assets", "_Project", "Games", "100_PenmonScramble", "Art")
SIZE = 512
FLIP = {"p03", "p05", "b102", "b103"}
# 塗りつぶしで背景を探すときの閾値。となりとの差が STEP 以下なら背景が続いているとみなす（グラデーションと影を追う）
STEP = 10
MIN_BRIGHT = 150
MAX_SAT = 40


def background_mask(rgb):
    h, w, _ = rgb.shape
    img = rgb.astype(np.int16)
    bright = img.mean(axis=2)
    sat = img.max(axis=2) - img.min(axis=2)
    ok = (bright >= MIN_BRIGHT) & (sat <= MAX_SAT)
    mask = np.zeros((h, w), bool)
    q = deque()
    for y in range(h):
        for x in (0, w - 1):
            q.append((y, x))
    for x in range(w):
        for y in (0, h - 1):
            q.append((y, x))
    for y, x in q:
        mask[y, x] = ok[y, x]
    q = deque(p for p in q if mask[p])
    while q:
        y, x = q.popleft()
        c = img[y, x]
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and not mask[ny, nx] and ok[ny, nx]:
                if np.abs(img[ny, nx] - c).max() <= STEP:
                    mask[ny, nx] = True
                    q.append((ny, nx))
    return mask


def cut(src, dst, flip):
    im = Image.open(src).convert("RGB")
    a = np.asarray(remove(im, session=_session()))[:, :, 3]
    # rembg は白いお腹を半透明にしがち、塗りつぶしは白いお腹から背景へ漏れがち。両方を足して、
    # 縁に囲まれた穴（お腹）は不透明で埋める
    flood = np.where(background_mask(np.asarray(im)), 0, 255).astype(np.uint8)
    a = np.maximum(a, flood)
    solid = ndimage.binary_closing(a > 100, structure=np.ones((9, 9)))
    a = np.where(ndimage.binary_fill_holes(solid) & (a < 255), 255, a).astype(np.uint8)
    out = im.convert("RGBA")
    out.putalpha(Image.fromarray(a))
    ys, xs = np.nonzero(a > 40)
    out = out.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    if flip:
        out = out.transpose(Image.FLIP_LEFT_RIGHT)
    side = max(out.size)
    pad = int(side * 0.04)
    canvas = Image.new("RGBA", (side + pad * 2, side + pad * 2), (0, 0, 0, 0))
    canvas.paste(out, ((canvas.width - out.width) // 2, (canvas.height - out.height) // 2))
    canvas.resize((SIZE, SIZE), Image.LANCZOS).save(dst)


_SESSION = None


def _session():
    global _SESSION
    if _SESSION is None:
        _SESSION = new_session("isnet-general-use")
    return _SESSION


def targets():
    for i in range(1, 21):
        yield f"p{i:02}", f"Penguins/Penguin{i:02}"
    for b in (101, 102, 103):
        yield f"b{b}", f"Bosses/Boss{b}"


if __name__ == "__main__":
    for name, out in targets():
        if "all" in sys.argv or name in sys.argv:
            cut(f"{RAW}/{name}.png", f"{ART}/{out}.png", name in FLIP)
            print(out, flush=True)
    # 地面は透過しないのでそのまま置く
    for w in (1, 2, 3):
        if "all" in sys.argv or f"w{w}" in sys.argv:
            Image.open(f"{RAW}/w{w}.png").convert("RGB").save(f"{ART}/Stage/World{w}.png")
            print(f"Stage/World{w}", flush=True)
