"""ComfyUI（Comfy Desktop）の API で、ワールドの地面の絵（§9）を作る。
Comfy Desktop を起動しておく（モデル: DreamShaperXL_Turbo_v2_1）。元絵は Tools/Art/raw/（git には入れない）に出し、
同じ絵を Art/Stage/World<n>.png へ置く。
使い方: python gen.py <name> [<name> ...]   （name: w1..w3, all）
seed を変えて作り直すとき: python gen.py w1:1234 。気に入ったら SEEDS に書いておく
"""
import json, sys, time, urllib.request, os, shutil

SERVER = "http://127.0.0.1:8188"
CKPT = "DreamShaperXL_Turbo_v2_1.safetensors"
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "raw")
STAGE_ART = os.path.join(HERE, "..", "..", "Assets", "_Project", "Games", "100_PenmonScramble", "Art", "Stage")
# 既定の seed（9000+ワールド）で気に入らず、作り直して決めたもの
SEEDS = {"w2": 9102}

GROUND_STYLE = ("top-down view, flat ground texture for a 2d game, seen from directly above, no characters, "
                "soft colors, low contrast, simple, evenly lit, stylized cartoon, toy diorama style")
GROUNDS = {
    1: "sunny sandy beach with small scattered ice floes and pebbles, shallow sea foam along the top edge, warm sand color",
    2: "flat smooth frozen lake ice surface, pale light blue ice surface with subtle thin cracks and scattered small snow patches, uniform floor texture filling the whole image, pale cyan",
    3: "dark night ocean surface with floating ice sheets and cracks in the ice, deep navy blue, faint moonlight reflections",
}
GROUND_NEG = ("text, letters, logo, watermark, characters, animals, penguins, people, horizon, sky, perspective, "
              "blurry, lowres, high contrast, busy, cluttered, frame, border, hole, pit, crater, walls, edges")


def workflow(pos, neg, w, h, seed, prefix):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": CKPT}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"text": pos, "clip": ["1", 1]}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"text": neg, "clip": ["1", 1]}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": w, "height": h, "batch_size": 1}},
        "5": {"class_type": "KSampler", "inputs": {"model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0],
                                                    "latent_image": ["4", 0], "seed": seed, "steps": 8, "cfg": 2.0,
                                                    "sampler_name": "dpmpp_sde", "scheduler": "karras", "denoise": 1.0}},
        "6": {"class_type": "VAEDecodeTiled", "inputs": {"samples": ["5", 0], "vae": ["1", 2], "tile_size": 512, "overlap": 64, "temporal_size": 64, "temporal_overlap": 8}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": prefix}},
    }


def job(name, seed=None):
    seed = seed or SEEDS.get(name)
    num = int(name[1:])
    if name[0] != "w" or num not in GROUNDS:
        raise ValueError(name)
    return workflow(f"{GROUNDS[num]}, {GROUND_STYLE}", GROUND_NEG, 1344, 768, seed or 9000 + num, f"world{num}")


def post(path, data):
    req = urllib.request.Request(SERVER + path, json.dumps(data).encode(), {"Content-Type": "application/json"})
    body = urllib.request.urlopen(req).read()
    return json.loads(body) if body else None


def get(path):
    return json.load(urllib.request.urlopen(SERVER + path))


def run(name, seed=None):
    start = time.time()
    pid = post("/prompt", {"prompt": job(name, seed)})["prompt_id"]
    while True:
        time.sleep(3)
        hist = get(f"/history/{pid}")
        if pid in hist:
            break
    entry = hist[pid]
    status = entry.get("status", {})
    if status.get("status_str") != "success":
        print(f"{name}: FAILED {json.dumps(status.get('messages', []))[-600:]}", flush=True)
        return False
    img = entry["outputs"]["7"]["images"][0]
    q = f"/view?filename={img['filename']}&subfolder={img['subfolder']}&type={img['type']}"
    os.makedirs(OUT, exist_ok=True)
    raw = os.path.join(OUT, f"{name}.png")
    with open(raw, "wb") as f:
        f.write(urllib.request.urlopen(SERVER + q).read())
    shutil.copyfile(raw, os.path.join(STAGE_ART, f"World{name[1:]}.png"))
    print(f"{name}: ok {time.time() - start:.0f}s", flush=True)
    return True


if __name__ == "__main__":
    names = []
    for a in sys.argv[1:]:
        if a == "all":
            names += [f"w{i}" for i in GROUNDS]
        else:
            names.append(a)
    for a in names:
        n, _, s = a.partition(":")
        if not run(n, int(s) if s else None):
            # XPU のメモリ不足は、モデルを全部降ろしてからやり直すと通る
            post("/free", {"unload_models": True, "free_memory": True})
            time.sleep(5)
            run(n, int(s) if s else None)  # XPU の一時的な失敗は1回だけやり直す
