"""
Compares HEAD version of each .cs file against working tree, ignoring
all comments and whitespace. Prints any file whose CODE differs.
"""
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

SCRIPTS_DIR = os.path.join(os.path.dirname(__file__), "Assets", "Scripts")

def strip_comments_and_ws(text: str) -> str:
    # Remove /* ... */ block comments (greedy minimal)
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.DOTALL)
    # Remove // line comments
    lines = []
    for line in text.split("\n"):
        # find // not inside a string. Simple heuristic: split on // but not inside quotes.
        # We'll use a simple state machine.
        out = []
        i = 0
        in_str = False
        in_char = False
        escaped = False
        while i < len(line):
            c = line[i]
            if escaped:
                out.append(c)
                escaped = False
                i += 1
                continue
            if c == "\\" and (in_str or in_char):
                out.append(c)
                escaped = True
                i += 1
                continue
            if c == '"' and not in_char:
                in_str = not in_str
                out.append(c)
                i += 1
                continue
            if c == "'" and not in_str:
                in_char = not in_char
                out.append(c)
                i += 1
                continue
            if not in_str and not in_char and c == "/" and i + 1 < len(line) and line[i+1] == "/":
                break
            out.append(c)
            i += 1
        stripped = "".join(out).strip()
        if stripped:
            lines.append(stripped)
    return "\n".join(lines)

def get_head_version(path: str) -> str:
    rel = os.path.relpath(path, os.path.dirname(__file__)).replace("\\", "/")
    try:
        out = subprocess.check_output(
            ["git", "show", f"HEAD:{rel}"],
            cwd=os.path.dirname(__file__),
            stderr=subprocess.PIPE,
        )
        return out.decode("utf-8", errors="replace")
    except subprocess.CalledProcessError:
        return None

def main():
    differences = []
    for root, dirs, files in os.walk(SCRIPTS_DIR):
        for f in files:
            if not f.endswith(".cs"):
                continue
            path = os.path.join(root, f)
            head = get_head_version(path)
            if head is None:
                print(f"NEW (no HEAD): {path}")
                continue
            with open(path, "r", encoding="utf-8", errors="replace") as fp:
                cur = fp.read()
            head_code = strip_comments_and_ws(head)
            cur_code  = strip_comments_and_ws(cur)
            if head_code != cur_code:
                differences.append((path, head_code, cur_code))

    if not differences:
        print("OK: every file's CODE matches HEAD (comments/whitespace only changed).")
        return

    print(f"\nFound {len(differences)} file(s) with CODE differences:\n")
    for path, head_code, cur_code in differences:
        rel = os.path.relpath(path, os.path.dirname(__file__))
        print(f"=== {rel} ===")
        # Show line-level diff
        head_lines = head_code.split("\n")
        cur_lines  = cur_code.split("\n")
        import difflib
        diff = difflib.unified_diff(head_lines, cur_lines, lineterm="",
                                     fromfile="HEAD", tofile="current", n=2)
        for line in diff:
            print(line)
        print()

if __name__ == "__main__":
    main()
