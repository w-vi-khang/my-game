#!/usr/bin/env python3
"""Quy tắc cân bằng RIÊNG của my-game (bảng chỉ số sinh vật). Chỉ thư viện chuẩn.
Phần dùng chung (pity, drop, streak, economy, curve) nằm ở skill `game-balance` của ai-workflow-kit;
file này là `balance.check_command` trong .ai/project.json.

  python3 tools/balance_rules.py check [design/data/stats.json]    kiểm bảng chỉ số theo quy tắc thiết kế (exit 1 nếu sai)
  python3 tools/balance_rules.py roll  <stats.json> <loài> [--n 1000] [--level L] [--tier T] [--seed S]

Quy tắc lấy từ design/02-gameplay/mechanics.md (mục Chỉ số sinh vật):
- giá trị thật = min(trần, tổng điểm × hệ số quy đổi); hệ số quy đổi giống nhau ở mọi loài.
- phân bố loài + phân bố ngẫu nhiên = tổng điểm cơ bản; phần ngẫu nhiên ít hơn phân bố loài.
- tổng điểm khác nhau giữa các loài nhưng không quá chênh (max_total_spread).
- điểm cấp độ/bậc: hai quỹ riêng (combat, work). Vượt trội bị bỏ trong PvP.
Mẫu dữ liệu: tools/balance_rules.stats.example.json (copy sang design/data/stats.json rồi sửa).
(Tách từ skill local-game-balance khi chuyển sang skill game-balance của kit, 2026-10-12.)
"""
import argparse, json, os, random, sys


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def species_total(sp_table):
    return sum(sp_table["species_points"].values()) + sp_table["random_points"]


def cmd_check(a):
    d = load(a.data)
    errs, warns = [], []
    spread = d.get("max_total_spread", 0.15)
    for tname, table in d["tables"].items():
        stats = table["stats"]
        for s, cfg in stats.items():
            if cfg.get("coef", 0) <= 0:
                errs.append(f"[{tname}] chỉ số '{s}': thiếu hoặc sai hệ số quy đổi")
            if "cap" not in cfg:
                errs.append(f"[{tname}] chỉ số '{s}': thiếu trần")
        totals = {}
        for name, sp in d["species"].items():
            if tname not in sp:
                errs.append(f"[{tname}] loài '{name}': thiếu bảng")
                continue
            t = sp[tname]
            for s in t["species_points"]:
                if s not in stats:
                    errs.append(f"[{tname}] loài '{name}': chỉ số lạ '{s}'")
            sp_pts = sum(t["species_points"].values())
            if t["random_points"] >= sp_pts:
                errs.append(f"[{tname}] loài '{name}': phần ngẫu nhiên ({t['random_points']}) phải ít hơn phân bố loài ({sp_pts})")
            totals[name] = species_total(t)
            # trần đạt ngay từ điểm cơ bản -> điểm cộng vào chỉ số đó vô nghĩa
            for s, p in t["species_points"].items():
                if s in stats and p * stats[s]["coef"] >= stats[s]["cap"]:
                    warns.append(f"[{tname}] loài '{name}': '{s}' chạm trần ngay từ phân bố loài")
        if totals:
            mean = sum(totals.values()) / len(totals)
            for name, tot in totals.items():
                dev = abs(tot - mean) / mean
                if dev > spread:
                    errs.append(f"[{tname}] loài '{name}': tổng {tot} lệch {dev:.0%} so với trung bình {mean:.0f} (cho phép {spread:.0%})")
            print(f"[{tname}] tổng điểm cơ bản: " + ", ".join(f"{k}={v}" for k, v in totals.items()) + f" (trung bình {mean:.0f})")
    for w in warns:
        print("CẢNH BÁO:", w)
    for e in errs:
        print("LỖI:", e)
    print("OK" if not errs else f"{len(errs)} lỗi")
    return 1 if errs else 0


def roll_points(t, stats, rng):
    pts = dict.fromkeys(stats, 0)
    for s, p in t["species_points"].items():
        pts[s] += p
    keys = list(t["species_points"])  # ngẫu nhiên chỉ rơi vào chỉ số loài có
    for _ in range(t["random_points"]):
        pts[rng.choice(keys)] += 1
    return pts


def cmd_roll(a):
    d = load(a.data)
    sp = d["species"].get(a.species)
    if sp is None:
        print(f"Không có loài '{a.species}'. Có: {', '.join(d['species'])}")
        return 1
    rng = random.Random(a.seed)
    for tname, table in d["tables"].items():
        stats, t = table["stats"], sp[tname]
        extra = d.get("points_per_level", {}).get(tname, 0) * max(a.level - 1, 0) \
            + d.get("points_per_tier", {}).get(tname, 0) * max(a.tier - 1, 0)
        lo, hi, capped = {}, {}, dict.fromkeys(stats, 0)
        for _ in range(a.n):
            pts = roll_points(t, stats, rng)
            for s, cfg in stats.items():
                v = pts[s] * cfg["coef"]
                if v >= cfg["cap"]:
                    capped[s] += 1
                v = min(cfg["cap"], v)
                lo[s], hi[s] = min(lo.get(s, v), v), max(hi.get(s, v), v)
        print(f"\n[{tname}] loài {a.species}, {a.n} cá thể; điểm cấp độ/bậc để phân bổ: {extra} (chưa cộng)")
        print(f"{'chỉ số':<16}{'thấp nhất':>12}{'cao nhất':>12}{'trần':>10}{'% chạm trần':>13}")
        for s, cfg in stats.items():
            print(f"{s:<16}{lo[s]:>12.2f}{hi[s]:>12.2f}{cfg['cap']:>10}{capped[s] / a.n:>13.1%}")
        for s, v in t.get("fixed", {}).items():
            print(f"{s:<16}{'cố định theo loài: ' + str(v):>34}")
    print(f"\nVượt trội (không tính trong PvP): +{sp.get('outstanding_extra', '—')} điểm")
    return 0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("check"); c.add_argument("data", nargs="?", default="design/data/stats.json")
    r = sub.add_parser("roll"); r.add_argument("data"); r.add_argument("species")
    r.add_argument("--n", type=int, default=1000); r.add_argument("--level", type=int, default=1)
    r.add_argument("--tier", type=int, default=1); r.add_argument("--seed", type=int, default=1)
    a = ap.parse_args()
    if a.cmd == "check" and not os.path.exists(a.data):
        print(f"Chưa có {a.data} — chưa có bảng chỉ số để kiểm (tạo từ tools/balance_rules.stats.example.json). "
              "Không phải lỗi quy tắc.")
        return 0
    return {"check": cmd_check, "roll": cmd_roll}[a.cmd](a)


if __name__ == "__main__":
    sys.exit(main())
