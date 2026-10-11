#!/usr/bin/env python3
"""Kiểm tra cấu trúc tài liệu thiết kế trong design/ (lệnh verify của giai đoạn tiền sản xuất).
  python3 tools/check_design.py            -> exit 0 nếu không có lỗi
  python3 tools/check_design.py --summary  -> in thêm bảng tiến độ

Lỗi (exit 1):
- File thiếu dòng '> Trạng thái: ...' hoặc trạng thái không hợp lệ.
- Link nội bộ (markdown) trỏ tới file không tồn tại.
- design/README.md không liệt kê file, hoặc trạng thái trong bảng lệch với file.
- File 'Chốt' vẫn còn '_(chưa có)_' hoặc '[ý tưởng]'.
- Khối mermaid rỗng hoặc không bắt đầu bằng loại sơ đồ hợp lệ.
- Quyết định trong decisions/ đặt tên sai (NNNN-ten.md) hoặc chưa có trong decisions/README.md.
Cảnh báo (không làm fail): số chỗ còn trống và số ý tưởng chưa chốt.
"""
import os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DESIGN = os.path.join(ROOT, "design")
STATUSES = {"Nháp", "Đang xem xét", "Chốt"}
DECISION_STATUSES = {"Đề xuất", "Chốt"}
MERMAID_TYPES = ("flowchart", "graph", "sequenceDiagram", "classDiagram", "stateDiagram", "stateDiagram-v2",
                 "erDiagram", "gantt", "journey", "mindmap", "timeline", "pie", "quadrantChart", "block-beta")
PLACEHOLDER, IDEA = "_(chưa có)_", "[ý tưởng]"
errors, warnings = [], []


def rel(p):
    return os.path.relpath(p, ROOT)


def status_of(text):
    m = re.search(r"^> Trạng thái:\s*(.+?)\s*$", "\n".join(text.splitlines()[:6]), re.M)
    return m.group(1) if m else None


def strip_code(text):
    """Bỏ khối code, code inline và comment HTML (hướng dẫn) trước khi đếm/kiểm link."""
    text = re.sub(r"```.*?```", "", text, flags=re.S)
    text = re.sub(r"<!--.*?-->", "", text, flags=re.S)
    return re.sub(r"`[^`\n]*`", "", text)


docs = {}
for dirpath, _, files in os.walk(DESIGN):
    for f in sorted(files):
        if f.endswith(".md") and f != "_template.md":
            p = os.path.join(dirpath, f)
            docs[p] = open(p, encoding="utf-8").read()

for p, text in docs.items():
    r = rel(p)
    st = status_of(text)
    in_decisions = os.path.dirname(p) == os.path.join(DESIGN, "decisions") and os.path.basename(p) != "README.md"
    if st is None:
        errors.append(f"{r}: thiếu dòng '> Trạng thái: ...' ở đầu file")
    elif in_decisions:
        if st not in DECISION_STATUSES and not st.startswith("Bị thay thế bởi"):
            errors.append(f"{r}: trạng thái quyết định không hợp lệ: '{st}'")
    elif st not in STATUSES:
        errors.append(f"{r}: trạng thái không hợp lệ '{st}' (dùng: {', '.join(sorted(STATUSES))})")

    body = strip_code(text)
    for target in re.findall(r"\]\(([^)\s]+)\)", body):
        if re.match(r"^[a-z]+:", target) or target.startswith("#"):
            continue
        path = os.path.normpath(os.path.join(os.path.dirname(p), target.split("#")[0]))
        if not os.path.exists(path):
            errors.append(f"{r}: link hỏng -> {target}")

    holes, ideas = body.count(PLACEHOLDER), body.count(IDEA)
    if st == "Chốt" and (holes or ideas):
        errors.append(f"{r}: trạng thái Chốt nhưng còn {holes} chỗ trống, {ideas} [ý tưởng]")
    elif holes or ideas:
        warnings.append((r, st, holes, ideas))

    for block in re.findall(r"```mermaid\n(.*?)```", text, flags=re.S):
        first = next((l.strip() for l in block.splitlines() if l.strip() and not l.strip().startswith("%%")), "")
        if not first.startswith(MERMAID_TYPES):
            errors.append(f"{r}: khối mermaid rỗng hoặc sai loại sơ đồ: '{first[:40]}'")

    if in_decisions and not re.fullmatch(r"\d{4}-[a-z0-9-]+\.md", os.path.basename(p)):
        errors.append(f"{r}: tên quyết định phải dạng NNNN-ten-ngan.md")

# Bảng trạng thái trong design/README.md
index_path = os.path.join(DESIGN, "README.md")
index = docs.get(index_path, "")
listed = {}
for link, st in re.findall(r"^\|\s*\[[^\]]*\]\(([^)]+)\)\s*\|[^|\n]*\|\s*([^|\n]+?)\s*\|", index, re.M):
    listed[os.path.normpath(os.path.join(DESIGN, link))] = st
for p, text in docs.items():
    if p == index_path or os.path.dirname(p) == os.path.join(DESIGN, "decisions"):
        continue
    if p not in listed:
        errors.append(f"design/README.md: chưa liệt kê {rel(p)}")
    elif status_of(text) and listed[p] != status_of(text):
        errors.append(f"design/README.md: trạng thái {rel(p)} ghi '{listed[p]}' nhưng file là '{status_of(text)}'")

# Danh sách quyết định
dec_index = docs.get(os.path.join(DESIGN, "decisions", "README.md"), "")
for p in docs:
    if os.path.dirname(p) == os.path.join(DESIGN, "decisions") and os.path.basename(p) != "README.md":
        if f"({os.path.basename(p)})" not in dec_index:
            errors.append(f"design/decisions/README.md: chưa liệt kê {os.path.basename(p)}")

if "--summary" in sys.argv:
    counts = {}
    for p, t in docs.items():
        counts[status_of(t)] = counts.get(status_of(t), 0) + 1
    print("Tiến độ: " + ", ".join(f"{k}: {v}" for k, v in sorted(counts.items(), key=lambda x: str(x[0]))))
    for r, st, holes, ideas in warnings:
        print(f"  • {r} [{st}] còn {holes} chỗ trống, {ideas} [ý tưởng]")

if errors:
    print("\n".join("✗ " + e for e in errors))
    print(f"--- {len(errors)} lỗi trong {len(docs)} file")
    sys.exit(1)
print(f"✓ design/ hợp lệ: {len(docs)} file, {sum(w[2] for w in warnings)} chỗ trống, "
      f"{sum(w[3] for w in warnings)} ý tưởng chưa chốt")
