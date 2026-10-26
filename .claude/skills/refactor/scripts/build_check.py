"""Unity が生成した csproj を dotnet build して、エラーだけを短く出す。

csproj はファイルを1つずつ列挙しているので、意図して消したファイルは CS2001（見つからない）になる。
git で削除済みのファイルの CS2001 は除外する。
変更のないプロジェクトは警告を出さないので、毎回 --no-incremental で全部コンパイルし直す。

--unused-warnings: 未使用コードの観点向けに、未使用を示すコンパイラ警告をファイル別に出す。
CS0649（割り当てられない）は SceneBuilder が値を入れているため誤検出なので出さない。

プロジェクトのルートで実行する。
"""
import argparse
import re
import subprocess
import sys

PROJECTS = ["Assembly-CSharp-Editor.csproj", "PenguinScramble.Logic.Tests.csproj"]
UNUSED_CODES = {"CS0169", "CS0414", "CS8321", "CS0168", "CS0219"}
LINE = re.compile(r"^(?P<file>.+?)\((?P<line>\d+),\d+\): (?P<kind>error|warning) (?P<code>CS\d+): (?P<msg>.*?)(?: \[.*\])?$")


def deleted_files():
    out = subprocess.run(["git", "ls-files", "--deleted"], capture_output=True, text=True, encoding="utf-8").stdout
    return {p.replace("/", "\\").lower() for p in out.split()}


def build(project):
    result = subprocess.run(["dotnet", "build", project, "-nologo", "-v", "q", "-clp:NoSummary", "--no-incremental"],
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    return result.stdout.splitlines()


def relative(path):
    marker = "assets\\"
    i = path.lower().find(marker)
    return path[i:] if i >= 0 else path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--unused-warnings", action="store_true")
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    deleted = deleted_files()
    errors, warnings = set(), set()
    for project in PROJECTS:
        for raw in build(project):
            m = LINE.match(raw.strip())
            if not m:
                continue
            file = relative(m["file"])
            if m["code"] == "CS2001" and any(d in m["msg"].lower() for d in deleted):
                continue
            entry = f"{file}:{m['line']} {m['code']} {m['msg']}"
            if m["kind"] == "error":
                errors.add(entry)
            elif m["code"] in UNUSED_CODES:
                warnings.add(entry)

    if args.unused_warnings:
        print(f"## 未使用を示す警告: {len(warnings)}件")
        for w in sorted(warnings):
            print(w)
        print()

    if errors:
        print(f"## コンパイルエラー: {len(errors)}件")
        for e in sorted(errors):
            print(e)
        sys.exit(1)
    print("OK: コンパイルエラー 0件")


if __name__ == "__main__":
    main()
