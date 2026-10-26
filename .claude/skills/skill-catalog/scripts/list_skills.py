#!/usr/bin/env python3
"""`.claude/skills/` 直下1階層のSKILL.mdからfrontmatter(name/description)だけを読み取ってJSONで出力する。

本文を読み込まないのは、一覧表示だけのためにSKILL.md全体をトークンに載せる必要がないため。
"""
import json
import re
import sys
from pathlib import Path

# WindowsのコンソールはデフォルトでUTF-8以外のコードページになりがちで、
# 日本語のJSONをそのままprintすると文字化けするため明示的にUTF-8へ切り替える。
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8")
    except AttributeError:
        pass

FRONTMATTER_RE = re.compile(r"\A---\r?\n(.*?)\r?\n---", re.DOTALL)


def parse_frontmatter(text: str) -> dict:
    m = FRONTMATTER_RE.match(text)
    if not m:
        raise ValueError("frontmatter (---で囲まれたヘッダー) が見つかりません")
    fields = {}
    for line in m.group(1).splitlines():
        line = line.strip()
        if not line or line.startswith("#") or ":" not in line:
            continue
        key, _, value = line.partition(":")
        fields[key.strip()] = value.strip()
    return fields


def collect_skills(skills_dir: Path) -> tuple[list[dict], list[str]]:
    """有効なスキルのリストと、スキップしたエントリの理由リストを返す。"""
    results = []
    skipped = []
    for entry in sorted(skills_dir.iterdir()):
        if not entry.is_dir():
            continue
        skill_md = entry / "SKILL.md"
        if not skill_md.is_file():
            skipped.append(f"{entry.name}: SKILL.mdが存在しない")
            continue
        try:
            text = skill_md.read_text(encoding="utf-8")
            fields = parse_frontmatter(text)
            name = fields.get("name")
            description = fields.get("description")
            if not name:
                raise ValueError("frontmatterにnameがない")
            if not description:
                raise ValueError("frontmatterにdescriptionがない")
        except Exception as e:
            skipped.append(f"{entry.name}: {e}")
            continue
        try:
            path_str = str(skill_md.relative_to(Path.cwd()).as_posix())
        except ValueError:
            path_str = str(skill_md.as_posix())
        results.append({"name": name, "description": description, "path": path_str})

    results.sort(key=lambda s: s["name"].lower())
    return results, skipped


def main():
    skills_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(".claude/skills")

    if not skills_dir.is_dir():
        print(f"エラー: スキルディレクトリが見つかりません: {skills_dir}", file=sys.stderr)
        sys.exit(1)

    skills, skipped = collect_skills(skills_dir)

    for reason in skipped:
        print(f"skip: {reason}", file=sys.stderr)

    print(json.dumps(skills, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
