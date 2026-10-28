"""ComfyUI（Comfy Desktop）の API で、ステージの絵（§9）の元絵を作る。
Comfy Desktop を起動しておく（モデル: DreamShaperXL_Turbo_v2_1）。出力は Tools/Art/raw/（git には入れない）。
使い方: python gen.py <name> [<name> ...]   （name: p01..p20, b101..b103, w1..w3, all）
seed を変えて作り直すとき: python gen.py p01:1234 。気に入ったら SEEDS に書いておく
背景の透過と Art/ への配置は cutout.py（README.md）
"""
import json, sys, time, urllib.request, os

SERVER = "http://127.0.0.1:8188"
CKPT = "DreamShaperXL_Turbo_v2_1.safetensors"
OUT = os.path.join(os.path.dirname(__file__), "raw")
# 既定の seed（種類ごとに 5000+No など）で気に入らず、作り直して決めたもの
SEEDS = {"p20": 6021, "w2": 9102}

STYLE = ("cute chubby round toy penguin character, colorful vinyl toy figure style, glossy 3d render, "
         "soft studio lighting, vibrant saturated colors, full body, single character, "
         "(three-quarter side view, body and head turned to the right:1.3), walking to the right, centered, "
         "(isolated on solid pure white background:1.4), simple white background")
CREATURE_NEG = ("text, letters, logo, watermark, signature, blood, scary, realistic, photo, dark, gloomy, horror, "
                "blurry, lowres, deformed, extra limbs, multiple characters, group, crowd, cropped, cut off, "
                "front view, facing viewer, symmetrical, colored background, gradient background, blue background, shadow on background, scenery, ground, floor, frame, border")

PENGUINS = {
    1: "adelie penguin, black head with white eye rings, tough delinquent attitude, frowning brave eyes, icy light blue accents",
    2: "chinstrap penguin, thin black chin strap line like a beard, wise old grandpa with tiny round glasses, icy light blue accents",
    3: "gentoo penguin, white patch over the eyes, bright orange beak, sporty runner with a light blue headband, energetic",
    4: "king penguin, golden orange ear patches, wearing a tiny golden crown and a red royal cape, proud",
    5: "big chubby emperor penguin, yellow neck patch, sturdy boss with a little ice blue scarf, confident, large",
    6: "humboldt penguin, black chest band, sleepy relaxed half-closed eyes, water drops, ocean blue accents",
    7: "african cape penguin, black chest stripe and pink patch above eyes, chatty open beak smile, blue water splash accents",
    8: "magellanic penguin, two black chest bands, adventurer with a small blue backpack and explorer hat",
    9: "galapagos penguin, small, sweating drops, holding a little blue fan, hot sweaty, ocean blue accents",
    10: "royal penguin, white face, rich little prince with a golden bow tie and sparkling jewels, blue water accents",
    11: "rockhopper penguin, spiky yellow crest feathers like a punk mohawk, angry red eyes, green wind accents",
    12: "fiordland penguin, yellow eyebrow crest, shy hiding behind a green leaf, timid eyes, green wind accents",
    13: "snares penguin, yellow crest, playing a small green drum around its belly, rhythmic happy",
    14: "macaroni penguin, flowing orange yellow crest, spinning dancer pose on one foot, green swirl wind effect",
    15: "yellow-eyed penguin, pale yellow band around the head, mysterious old hermit sage with a long white beard and a wooden staff, green wind aura",
    16: "tiny little blue penguin, very small and round, static electricity sparks, fluffy staticky feathers, yellow lightning accents",
    17: "white-flippered penguin, inventor wearing goggles on the head, holding a little wrench, yellow lightning accents",
    18: "erect-crested penguin, upright yellow crest like an antenna, glowing tip, yellow electric sparks",
    19: "fantasy electric penguin with glowing yellow electric eel stripe patterns on the body, glowing, lightning sparks",
    20: "fantasy thunder god penguin child, (a fluffy dark grey storm cloud floating right above its head:1.3), yellow lightning bolts striking from the cloud, ring of small taiko drums on its back, golden glowing eyes, powerful",
}

BOSSES = {
    101: "big fierce brown great skua seabird boss monster, spread wings, sharp hooked beak, angry eyes, toy figure style",
    102: "big leopard seal boss monster, spotted grey body, wide toothy grin, menacing but cute, toy figure style",
    103: "big orca killer whale boss monster, black and white, jumping pose, toothy grin, menacing but cute, toy figure style",
}
BOSS_STYLE = ("colorful vinyl toy figure style, glossy 3d render, soft studio lighting, vibrant colors, full body, "
              "single character, (three-quarter side view, turned to the right:1.3), centered, (isolated on solid pure white background:1.4), simple white background")

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
    kind, num = name[0], int(name[1:])
    if kind == "p":
        return workflow(f"{PENGUINS[num]}, {STYLE}", CREATURE_NEG, 896, 896, seed or 5000 + num, f"penguin{num:02}")
    if kind == "b":
        return workflow(f"{BOSSES[num]}, {BOSS_STYLE}", CREATURE_NEG, 896, 896, seed or 7000 + num, f"boss{num}")
    if kind == "w":
        return workflow(f"{GROUNDS[num]}, {GROUND_STYLE}", GROUND_NEG, 1344, 768, seed or 9000 + num, f"world{num}")
    raise ValueError(name)


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
    with open(os.path.join(OUT, f"{name}.png"), "wb") as f:
        f.write(urllib.request.urlopen(SERVER + q).read())
    print(f"{name}: ok {time.time() - start:.0f}s", flush=True)
    return True


if __name__ == "__main__":
    names = []
    for a in sys.argv[1:]:
        if a == "all":
            names += [f"p{i:02}" for i in range(1, 21)] + ["b101", "b102", "b103", "w1", "w2", "w3"]
        else:
            names.append(a)
    for a in names:
        n, _, s = a.partition(":")
        if not run(n, int(s) if s else None):
            # XPU のメモリ不足は、モデルを全部降ろしてからやり直すと通る
            post("/free", {"unload_models": True, "free_memory": True})
            time.sleep(5)
            run(n, int(s) if s else None)  # XPU の一時的な失敗は1回だけやり直す
