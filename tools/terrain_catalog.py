#!/usr/bin/env python3
"""Kiểm danh mục địa hình của my-game (skill local-terrain-gen). Chỉ thư viện chuẩn.

  python3 tools/terrain_catalog.py check   [design/data/terrain_catalog.json]   kiểm cấu trúc + tham chiếu chéo (exit 1 nếu sai)
  python3 tools/terrain_catalog.py summary [catalog]                            bảng dạng địa hình × khối, việc phải làm thêm
  python3 tools/terrain_catalog.py section <catalog> <id dạng địa hình|id chuyển tiếp>   in mặt cắt ASCII kèm chú giải

Danh mục gồm:
- blocks: vật liệu khối (exists=false → vật liệu mới, phải thêm vào Cell.cs/màu/texture).
- shapes: hình khối (cube, dốc chéo, góc dốc, khối nước mặt...). exists=false → việc code mới (mesher, collider, tìm đường).
- landforms: dạng địa hình — lớp khối theo độ sâu, luật xếp, tham số (value + tmp), mặt cắt ASCII.
- transitions: vùng giao nhau giữa hai dạng địa hình — độ rộng, luật trộn, khối/hình dùng.
Mẫu: tools/terrain_catalog.example.json (copy sang design/data/terrain_catalog.json rồi sửa).
"""
import argparse, json, sys

DEFAULT = "design/data/terrain_catalog.json"
STATUSES = {"ý tưởng", "chốt"}


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def ids(items):
    return [x.get("id") for x in items]


def check_params(owner, params, errs, warns):
    for name, p in (params or {}).items():
        if not isinstance(p, dict) or "value" not in p:
            errs.append(f"{owner}: tham số '{name}' phải là {{value, tmp}}")
            continue
        if "tmp" not in p:
            errs.append(f"{owner}: tham số '{name}' thiếu cờ tmp (số tạm/chưa chốt hay đã chốt)")


def check_section(owner, sec, legend, errs):
    if not sec:
        return
    widths = {len(r) for r in sec}
    if len(widths) != 1:
        errs.append(f"{owner}: các dòng mặt cắt dài không bằng nhau {sorted(widths)}")
    for ch in sorted({c for r in sec for c in r}):
        if ch not in legend:
            errs.append(f"{owner}: ký tự '{ch}' trong mặt cắt chưa có trong legend")


def cmd_check(a):
    d = load(a.catalog)
    errs, warns = [], []
    blocks, shapes = d.get("blocks", []), d.get("shapes", [])
    lands, trans = d.get("landforms", []), d.get("transitions", [])
    legend = d.get("legend", {})
    for coll, items in (("blocks", blocks), ("shapes", shapes), ("landforms", lands), ("transitions", trans)):
        seen = set()
        for i in ids(items):
            if not i:
                errs.append(f"{coll}: có mục thiếu id")
            elif i in seen:
                errs.append(f"{coll}: id trùng '{i}'")
            seen.add(i)
    bset, sset, lset = set(ids(blocks)), set(ids(shapes)), set(ids(lands))
    for ch, ref in legend.items():
        if len(ch) != 1:
            errs.append(f"legend: khoá '{ch}' phải là 1 ký tự")
        if ref not in bset | sset | {"air"}:
            errs.append(f"legend: '{ch}' → '{ref}' không phải block/shape/air")
    for s in shapes:
        o = f"shape '{s['id']}'"
        for n in s.get("needs", []):
            if n not in sset:
                errs.append(f"{o}: cần hình '{n}' nhưng danh mục chưa có")
        if s.get("kind") == "water_top":
            fr = s.get("water_fraction")
            if not (isinstance(fr, list) and len(fr) == 2 and 0 < fr[0] <= fr[1] < 1):
                errs.append(f"{o}: water_fraction phải là [min, max] trong (0, 1), min ≤ max")
        if not s.get("exists", False):
            for k in ("walkable", "collider", "dig_result"):
                if k not in s:
                    errs.append(f"{o}: hình mới phải ghi '{k}' (ảnh hưởng tìm đường/va chạm/đào)")
        check_params(o, s.get("params"), errs, warns)
    for l in lands:
        o = f"landform '{l['id']}'"
        if l.get("status") not in STATUSES:
            errs.append(f"{o}: status phải là một trong {sorted(STATUSES)}")
        if not l.get("pillars"):
            errs.append(f"{o}: thiếu pillars (mọi cơ chế gắn ≥ 1 pillar)")
        if not l.get("layers"):
            errs.append(f"{o}: thiếu layers (khối theo độ sâu)")
        for ly in l.get("layers", []):
            if ly.get("block") not in bset:
                errs.append(f"{o}: layer dùng khối '{ly.get('block')}' chưa có trong blocks")
        for sh in l.get("shapes", []):
            if sh not in sset:
                errs.append(f"{o}: dùng hình '{sh}' chưa có trong shapes")
        if not l.get("rules"):
            warns.append(f"{o}: chưa có luật xếp khối (rules)")
        check_params(o, l.get("params"), errs, warns)
        check_section(o, l.get("section"), legend, errs)
    pairs = set()
    for t in trans:
        o = f"transition '{t['id']}'"
        a_, b_ = t.get("a"), t.get("b")
        for x in (a_, b_):
            if x not in lset:
                errs.append(f"{o}: dạng địa hình '{x}' chưa có trong landforms")
        if a_ == b_:
            errs.append(f"{o}: a và b trùng nhau")
        key = frozenset((a_, b_))
        if key in pairs:
            errs.append(f"{o}: cặp {a_}–{b_} đã có chuyển tiếp khác")
        pairs.add(key)
        for x in t.get("blocks", []):
            if x not in bset:
                errs.append(f"{o}: dùng khối '{x}' chưa có trong blocks")
        for x in t.get("shapes", []):
            if x not in sset:
                errs.append(f"{o}: dùng hình '{x}' chưa có trong shapes")
        if not t.get("rule"):
            errs.append(f"{o}: thiếu luật trộn (rule)")
        check_params(o, t.get("params"), errs, warns)
        check_section(o, t.get("section"), legend, errs)
    for w in warns:
        print("CẢNH BÁO:", w)
    for e in errs:
        print("LỖI:", e)
    if errs:
        print(f"✗ {len(errs)} lỗi, {len(warns)} cảnh báo")
        return 1
    print(f"✓ danh mục hợp lệ: {len(blocks)} khối, {len(shapes)} hình, {len(lands)} dạng địa hình, "
          f"{len(trans)} chuyển tiếp, {len(warns)} cảnh báo")
    return 0


def cmd_summary(a):
    d = load(a.catalog)
    for l in d.get("landforms", []):
        bl = ", ".join(ly["block"] for ly in l.get("layers", []))
        sh = ", ".join(l.get("shapes", [])) or "—"
        print(f"- {l['id']} ({l.get('name', '')}) [{l.get('status')}] khối: {bl} | hình: {sh}")
    for t in d.get("transitions", []):
        print(f"- chuyển tiếp {t['a']} ↔ {t['b']}: {t.get('rule', '')}")
    new_b = [b["id"] for b in d.get("blocks", []) if not b.get("exists", False)]
    new_s = [s["id"] for s in d.get("shapes", []) if not s.get("exists", False)]
    tmp = sum(1 for coll in ("shapes", "landforms", "transitions") for x in d.get(coll, [])
              for p in (x.get("params") or {}).values() if isinstance(p, dict) and p.get("tmp"))
    print(f"Vật liệu mới phải làm: {', '.join(new_b) or 'không'}")
    print(f"Hình khối mới phải làm: {', '.join(new_s) or 'không'}")
    print(f"Tham số [số tạm]: {tmp}")
    return 0


def cmd_section(a):
    d = load(a.catalog)
    legend = d.get("legend", {})
    for item in d.get("landforms", []) + d.get("transitions", []):
        if item["id"] == a.id:
            sec = item.get("section")
            if not sec:
                print(f"'{a.id}' chưa có mặt cắt")
                return 1
            print(f"Mặt cắt '{a.id}' (trục ngang = ô, trên = cao):")
            for r in sec:
                print("  " + r)
            used = sorted({c for r in sec for c in r})
            print("Chú giải: " + ", ".join(f"'{c}'={legend.get(c, '?')}" for c in used))
            return 0
    print(f"Không có dạng địa hình/chuyển tiếp '{a.id}'")
    return 1


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    for name in ("check", "summary"):
        s = sub.add_parser(name)
        s.add_argument("catalog", nargs="?", default=DEFAULT)
    s = sub.add_parser("section")
    s.add_argument("catalog")
    s.add_argument("id")
    a = p.parse_args()
    return {"check": cmd_check, "summary": cmd_summary, "section": cmd_section}[a.cmd](a)


if __name__ == "__main__":
    sys.exit(main())
