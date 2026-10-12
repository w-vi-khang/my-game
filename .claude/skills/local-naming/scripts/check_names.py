#!/usr/bin/env python3
"""Kiểm danh sách tên riêng trong design/01-story/naming.md (chỉ thư viện chuẩn).

  python3 check_names.py [--naming design/01-story/naming.md] [--glossary design/glossary.md]
                         [--similar 0.85] [--max-len 16] [--candidates "Tên A,Tên B"]

Đọc bảng dưới mục '## Danh sách tên' (cột 1 = tên, cột 2 = nhóm, cột 4 = trạng thái; bỏ qua dòng trạng thái 'bỏ').
Lỗi (exit 1): tên trùng nhau (không phân biệt hoa thường, dấu); tên trùng một thuật ngữ trong glossary.
Cảnh báo: tên gần giống nhau (tỉ lệ giống ≥ --similar), tên dài hơn --max-len ký tự.
--candidates: kiểm thêm các tên đề xuất (chưa ghi vào bảng) so với bảng và glossary.
"""
import argparse, difflib, re, sys, unicodedata


def norm(s):
    s = unicodedata.normalize("NFD", s.strip().lower()).replace("đ", "d")
    return "".join(c for c in s if unicodedata.category(c) != "Mn")


def table_rows(text, heading):
    m = re.search(rf"^##\s+{re.escape(heading)}\s*$(.*?)(?=^##\s|\Z)", text, re.M | re.S)
    if not m:
        return []
    rows = []
    for line in m.group(1).splitlines():
        line = line.strip()
        if not line.startswith("|") or re.match(r"^\|[\s:|-]+\|$", line):
            continue
        cells = [c.strip() for c in line.strip("|").split("|")]
        rows.append(cells)
    return rows[1:]  # bỏ dòng tiêu đề


def glossary_terms(text):
    terms = set()
    for line in text.splitlines():
        if line.startswith("|") and not re.match(r"^\|[\s:|-]+\|$", line):
            first = line.strip("|").split("|")[0].strip()
            for t in first.split("/"):
                if t.strip() and t.strip() != "Thuật ngữ":
                    terms.add(norm(t))
    return terms


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--naming", default="design/01-story/naming.md")
    ap.add_argument("--glossary", default="design/glossary.md")
    ap.add_argument("--similar", type=float, default=0.85)
    ap.add_argument("--max-len", type=int, default=16)
    ap.add_argument("--candidates", default="")
    a = ap.parse_args()

    rows = table_rows(open(a.naming, encoding="utf-8").read(), "Danh sách tên")
    names = [(r[0], r[1] if len(r) > 1 else "") for r in rows
             if r and r[0] and norm(r[3] if len(r) > 3 else "") != "bo"]
    gloss = glossary_terms(open(a.glossary, encoding="utf-8").read())
    cands = [(c.strip(), "đề xuất") for c in a.candidates.split(",") if c.strip()]

    errs, warns, seen = [], [], {}
    for name, group in names + cands:
        n = norm(name)
        if n in seen:
            errs.append(f"trùng tên: '{name}' ({group}) và '{seen[n][0]}' ({seen[n][1]})")
        else:
            seen[n] = (name, group)
        if n in gloss:
            errs.append(f"'{name}' trùng thuật ngữ trong glossary")
        if len(name) > a.max_len:
            warns.append(f"'{name}' dài {len(name)} ký tự (> {a.max_len})")
    uniq = list(seen.values())
    for i in range(len(uniq)):
        for j in range(i + 1, len(uniq)):
            r = difflib.SequenceMatcher(None, norm(uniq[i][0]), norm(uniq[j][0])).ratio()
            if r >= a.similar:
                warns.append(f"gần giống ({r:.0%}): '{uniq[i][0]}' ~ '{uniq[j][0]}'")

    print(f"{len(names)} tên trong bảng, {len(cands)} tên đề xuất, {len(gloss)} thuật ngữ glossary")
    for w in warns:
        print("CẢNH BÁO:", w)
    for e in errs:
        print("LỖI:", e)
    print("OK" if not errs else f"{len(errs)} lỗi")
    return 1 if errs else 0


if __name__ == "__main__":
    sys.exit(main())
