#!/usr/bin/env python3
"""Công cụ cân bằng số liệu của my-game (chỉ dùng thư viện chuẩn).

  python3 balance.py check   <stats.json>                     kiểm tra bảng chỉ số theo quy tắc thiết kế
  python3 balance.py roll    <stats.json> <loài> [--n 1000] [--level L] [--tier T] [--seed S]
                                                              sinh thử cá thể -> khoảng giá trị từng chỉ số
  python3 balance.py luck    --rate p --window N [--target 0.01] [--lifetime] [--players 20000] [--owns 300] [--reduce 0.25]
                                                              chọn ngưỡng K cho "chặn quá may" + mô phỏng
  python3 balance.py economy <economy.json> [--days 30]       dòng tài nguyên (faucet/sink) theo ngày
  python3 balance.py pity    --rate p --hard N [--cost C]      bảo hiểm gói: lần rút trung bình, % chạm mốc, chi phí

Quy tắc lấy từ design/02-gameplay/mechanics.md (mục Chỉ số sinh vật):
- giá trị thật = min(trần, tổng điểm × hệ số quy đổi); hệ số quy đổi giống nhau ở mọi loài.
- phân bố loài + phân bố ngẫu nhiên = tổng điểm cơ bản; phần ngẫu nhiên ít hơn phân bố loài.
- tổng điểm khác nhau giữa các loài nhưng không quá chênh (max_total_spread).
- điểm cấp độ/bậc: hai quỹ riêng (combat, work). Vượt trội bị bỏ trong PvP.
Exit 1 nếu `check` phát hiện lỗi.
"""
import argparse, json, math, random, sys
from collections import deque


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


def binom_tail(n, p, k):
    """P(X >= k), X ~ Binomial(n, p)."""
    return sum(math.comb(n, i) * p ** i * (1 - p) ** (n - i) for i in range(k, n + 1))


def simulate_luck(p, n, k, a):
    """Mô phỏng chặn quá may: trong n lần sở hữu gần nhất có >= k vượt trội -> tỉ lệ × reduce."""
    rng = random.Random(a.seed)
    hit_players, total_out, throttled_steps = 0, 0, 0
    for _ in range(a.players):
        win, cnt, hit = deque(), 0, False
        for _ in range(a.owns):
            throttled = cnt >= k
            throttled_steps += throttled
            hit = hit or throttled
            got = rng.random() < (p * a.reduce if throttled else p)
            win.append(got)
            cnt += got
            if len(win) > n:
                cnt -= win.popleft()
            total_out += got
        hit_players += hit
    owns = a.players * a.owns
    return hit_players / a.players, throttled_steps / owns, total_out / owns


def cmd_luck(a):
    p, n = a.rate, a.window
    k = next(k for k in range(1, n + 2) if k > n or binom_tail(n, p, k) < a.target)
    print(f"Tỉ lệ vượt trội {p:.2%}, cửa sổ {n} lần sở hữu gần nhất. Kỳ vọng trong cửa sổ: {n * p:.2f}.")
    print(f"Ngưỡng K nhỏ nhất có P(≥K trong một cửa sổ) < {a.target:.1%}: K = {k} "
          f"(P = {binom_tail(n, p, k) if k <= n else 0:.4%})")
    if a.lifetime:  # mục tiêu tính trên cả quá trình chơi: % người chơi từng bị chặn < target
        while k <= n:
            hit, _, _ = simulate_luck(p, n, k, a)
            if hit < a.target:
                break
            k += 1
        print(f"Chế độ --lifetime: K = {k} để < {a.target:.1%} người chơi từng bị chặn sau {a.owns} lần sở hữu")
    hit, thr, eff = simulate_luck(p, n, k, a)
    print(f"Mô phỏng {a.players} người chơi × {a.owns} lần sở hữu, K = {k}, hạ tỉ lệ còn ×{a.reduce} khi chạm ngưỡng:")
    print(f"- Người chơi từng bị chặn: {hit:.2%}")
    print(f"- Lần sở hữu đang bị chặn: {thr:.3%}")
    print(f"- Tỉ lệ vượt trội thực tế: {eff:.3%} (gốc {p:.3%})")
    return 0


def cmd_pity(a):
    """Bảo hiểm cứng: rút N lần liên tiếp chưa ra phần thưởng hiếm -> lần thứ N chắc chắn ra."""
    p, n = a.rate, a.hard
    q = 1 - p
    expected = (1 - q ** n) / p if p > 0 else n  # E[số lần rút tới khi ra] = sum_{k=0}^{N-1} q^k
    reach = q ** (n - 1)  # xác suất phải tới lần thứ N mới ra (nhờ bảo hiểm)
    print(f"Tỉ lệ gốc {p:.2%}, mốc bảo hiểm {n} lần.")
    print(f"- Số lần rút trung bình tới khi ra: {expected:.1f} (không bảo hiểm: {1 / p:.1f})")
    print(f"- Tỉ lệ thực tế (tính cả bảo hiểm): {1 / expected:.2%}")
    print(f"- Xác suất người chơi phải chạm mốc bảo hiểm: {reach:.2%}")
    for pct in (0.5, 0.9):
        k = next((k for k in range(1, n + 1) if 1 - q ** k >= pct), n)
        print(f"- {pct:.0%} người chơi ra trong ≤ {k} lần")
    if a.cost:
        print(f"- Chi phí trung bình: {expected * a.cost:.0f}; tệ nhất (chạm mốc): {n * a.cost:.0f}")
    return 0


def cmd_economy(a):
    d = load(a.data)
    print(f"{'tài nguyên':<22}{'vào/ngày':>12}{'ra/ngày':>12}{'ròng/ngày':>12}{f'sau {a.days} ngày':>16}")
    for r, cfg in d["resources"].items():
        fin, fout = sum(cfg.get("faucets", {}).values()), sum(cfg.get("sinks", {}).values())
        net = fin - fout
        print(f"{r:<22}{fin:>12.1f}{fout:>12.1f}{net:>12.1f}{cfg.get('start', 0) + net * a.days:>16.1f}")
        for name, cost in cfg.get("targets", {}).items():
            days = "không bao giờ" if net <= 0 else f"{max(cost - cfg.get('start', 0), 0) / net:.1f} ngày"
            print(f"    → đủ cho '{name}' ({cost}): {days}")
    return 0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("check"); c.add_argument("data")
    r = sub.add_parser("roll"); r.add_argument("data"); r.add_argument("species")
    r.add_argument("--n", type=int, default=1000); r.add_argument("--level", type=int, default=1)
    r.add_argument("--tier", type=int, default=1); r.add_argument("--seed", type=int, default=1)
    l = sub.add_parser("luck"); l.add_argument("--rate", type=float, required=True)
    l.add_argument("--window", type=int, required=True); l.add_argument("--target", type=float, default=0.01)
    l.add_argument("--players", type=int, default=20000); l.add_argument("--owns", type=int, default=300)
    l.add_argument("--reduce", type=float, default=0.25); l.add_argument("--seed", type=int, default=1)
    l.add_argument("--lifetime", action="store_true", help="target = %% người chơi từng bị chặn trong --owns lần sở hữu")
    pi = sub.add_parser("pity"); pi.add_argument("--rate", type=float, required=True)
    pi.add_argument("--hard", type=int, required=True); pi.add_argument("--cost", type=float, default=0)
    e = sub.add_parser("economy"); e.add_argument("data"); e.add_argument("--days", type=int, default=30)
    a = ap.parse_args()
    return {"check": cmd_check, "roll": cmd_roll, "luck": cmd_luck, "economy": cmd_economy, "pity": cmd_pity}[a.cmd](a)


if __name__ == "__main__":
    sys.exit(main())
