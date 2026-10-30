"""haiku に渡す担当（チャンク）を作る。

code: 対象フォルダの .cs をフォルダ単位でまとめ、1担当が max-lines 以下になるよう分ける。
      フォルダが大きすぎるときは子フォルダに降り、直下のファイルは行数で詰めて分ける。
docs: Docs/ 直下の .md を「## 」見出しで区切り、max-lines 以下になるよう詰める。

出力は JSON（標準出力）。プロジェクトのルートで実行する。
"""
import argparse
import json
import sys
from pathlib import Path

CODE_ROOTS = [
    "Assets/_Project/Common/Scripts",
    "Assets/_Project/Common/Editor",
    "Assets/_Project/Games/100_PenmonScramble/Scripts",
    "Assets/_Project/Games/100_PenmonScramble/Editor",
]


def count_lines(path: Path) -> int:
    with path.open(encoding="utf-8", errors="replace") as f:
        return sum(1 for _ in f)


def total_lines(folder: Path) -> int:
    return sum(count_lines(p) for p in folder.rglob("*.cs"))


def pack(files, max_lines):
    """(path, lines) のリストを順に詰めて、max_lines 以下の束にする。"""
    groups, current, size = [], [], 0
    for path, lines in files:
        if current and size + lines > max_lines:
            groups.append((current, size))
            current, size = [], 0
        current.append(path)
        size += lines
    if current:
        groups.append((current, size))
    return groups


def split_folder(folder: Path, max_lines: int, out: list):
    size = total_lines(folder)
    if size == 0:
        return
    if size <= max_lines:
        files = sorted(p.as_posix() for p in folder.rglob("*.cs"))
        out.append({"name": folder.as_posix(), "lines": size, "files": files})
        return
    for sub in sorted(p for p in folder.iterdir() if p.is_dir()):
        split_folder(sub, max_lines, out)
    direct = [(p.as_posix(), count_lines(p)) for p in sorted(folder.glob("*.cs"))]
    groups = pack(direct, max_lines)
    for i, (files, lines) in enumerate(groups, 1):
        suffix = f" ({i}/{len(groups)})" if len(groups) > 1 else ""
        out.append({"name": folder.as_posix() + "/*.cs" + suffix, "lines": lines, "files": files})


def merge_small(chunks, max_lines):
    """隣り合う小さな担当を max_lines まで1つにまとめる。haiku の起動数を減らすため。
    並びはフォルダ順なので、まとまるのは近いフォルダ同士になる。"""
    merged = []
    for c in chunks:
        last = merged[-1] if merged else None
        if last and last["lines"] + c["lines"] <= max_lines:
            last["name"] += " + " + c["name"]
            last["lines"] += c["lines"]
            last["files"] += c["files"]
        else:
            merged.append(dict(c, files=list(c["files"])))
    return merged


def plan_code(max_lines):
    out = []
    for root in CODE_ROOTS:
        split_folder(Path(root), max_lines, out)
    return merge_small(out, max_lines)


def doc_sections(path: Path):
    """「## 」見出しごとに (開始行, 終了行) を返す（1始まり・終了行を含む）。"""
    lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    starts = [0] + [i for i, l in enumerate(lines) if l.startswith("## ") and i > 0]
    bounds = starts + [len(lines)]
    return [(bounds[i] + 1, bounds[i + 1]) for i in range(len(starts))]


def plan_docs(max_lines):
    out = []
    for path in sorted(Path("Docs").glob("*.md")):
        sections = [((s, e), e - s + 1) for s, e in doc_sections(path)]
        for ranges, lines in pack(sections, max_lines):
            start, end = ranges[0][0], ranges[-1][1]
            out.append({"name": f"{path.as_posix()}:{start}-{end}", "lines": lines,
                        "file": path.as_posix(), "start": start, "end": end})
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", choices=["code", "docs"], required=True)
    parser.add_argument("--max-lines", type=int)
    args = parser.parse_args()
    max_lines = args.max_lines or (3000 if args.mode == "code" else 300)
    plan = plan_code(max_lines) if args.mode == "code" else plan_docs(max_lines)
    sys.stdout.reconfigure(encoding="utf-8")
    print(json.dumps({"mode": args.mode, "max_lines": max_lines, "chunks": plan},
                     ensure_ascii=False, indent=1))


if __name__ == "__main__":
    main()
