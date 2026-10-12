---
name: local-game-balance
description: Cân bằng số liệu game bằng script — chỉ số sinh vật (base, phân bố loài/ngẫu nhiên, hệ số quy đổi, trần, điểm mỗi cấp độ/bậc), tỉ lệ vượt trội và chặn quá may, bảo hiểm gói rút thưởng, dòng tài nguyên kinh tế (faucet/sink), nhịp tiến trình; ghi bảng dữ liệu trong design/data/. Dùng khi user muốn đặt/chỉnh con số, tính tỉ lệ, kiểm tra cân bằng, mô phỏng kinh tế hoặc tiến trình.
---

# Cân bằng số liệu (chỉ số, kinh tế, tiến trình)

Quy tắc thiết kế là nguồn chuẩn — skill này chỉ đặt con số cho quy tắc đã có, không đổi quy tắc:
- Chỉ số: `design/02-gameplay/mechanics.md` mục "Chỉ số sinh vật", "Nhận sinh vật" (vượt trội, chặn quá may).
- Kinh tế: `design/02-gameplay/economy.md`; tiền nạp: `design/decisions/0004-tien-nap.md`.
- Tiến trình: `design/02-gameplay/progression.md`.

## Đầu vào cần có (thiếu thì hỏi tuần tự, mỗi lượt 1–2 câu, kèm đề xuất)
1. Cân bằng phần nào: bảng chỉ số, tỉ lệ vượt trội/chặn quá may, dòng tài nguyên, hay nhịp tiến trình?
2. Mục tiêu đo được (vd "tối đa 1% người chơi từng bị chặn quá may sau 300 lần sở hữu", "1 lần rút thưởng sau ~2 ngày chơi không nạp").
3. Phạm vi loài/tài nguyên cần tính (một loài, cả map, toàn game).

## Quy trình
1. Đọc các file nguồn chuẩn ở trên + `design/glossary.md`. Ghi lại quy tắc sẽ kiểm (vd phần ngẫu nhiên < phân bố loài; tổng điểm các loài lệch không quá `max_total_spread`; hệ số quy đổi giống nhau mọi loài; vượt trội bỏ trong PvP; tiền nạp không vượt mức tối đa).
2. Dữ liệu nằm ở `design/data/` (JSON, không cần dòng trạng thái). Chưa có → copy từ `templates/` của skill này (`stats.example.json`, `economy.example.json`) rồi sửa; giữ `_comment` ghi rõ số nào là minh hoạ.
3. Chạy công cụ, đọc output thật; lặp chỉnh số cho tới khi đạt mục tiêu ở đầu vào 2.
4. Ghi kết quả vào tài liệu: số đã chọn + lý do + lệnh đã chạy và output chính (vào mục tương ứng trong mechanics/economy/progression). Số do AI đề xuất ghi `[ý tưởng]` cho tới khi user đồng ý.
5. Tác động chéo: số liệu chỉ số ↔ kinh tế ↔ tiến trình ↔ roadmap (vd thay tỉ lệ rút thưởng đổi nhịp tiến trình). Cập nhật phần Nháp; phần Chốt → hỏi user.
6. Chạy `python3 .claude/skills/local-game-balance/scripts/balance.py check design/data/stats.json` (nếu có) và `python3 tools/check_design.py`.

## Công cụ
`python3 .claude/skills/local-game-balance/scripts/balance.py <lệnh>` (chỉ thư viện chuẩn):
- `check <stats.json>` — kiểm quy tắc bảng chỉ số; exit 1 nếu sai.
- `roll <stats.json> <loài> [--n 1000 --level L --tier T]` — sinh thử cá thể, xem khoảng giá trị và % chạm trần.
- `luck --rate p --window N [--target 0.01] [--lifetime] [--owns 300] [--reduce 0.25]` — chọn ngưỡng K cho chặn quá may. Dùng `--lifetime` khi mục tiêu tính trên cả quá trình chơi: K theo từng cửa sổ thường làm nhiều người chơi bị chặn hơn mục tiêu.
- `economy <economy.json> [--days 30]` — dòng tài nguyên mỗi ngày, số ngày tới mục tiêu.
- `pity --rate p --hard N [--cost C]` — bảo hiểm gói: số lần rút trung bình, tỉ lệ thực tế, % người chơi chạm mốc, chi phí trung bình/tệ nhất.
- Mẫu dữ liệu: `.claude/skills/local-game-balance/templates/`.

## Cấm
- Không đổi quy tắc thiết kế để "vừa" con số — báo mâu thuẫn và hỏi user.
- Không trình bày số minh hoạ/ước đoán như số đã cân bằng; không bịa số liệu thị trường (tỉ lệ gacha của game khác...) — cần `[docs]` kèm link.
- Không đặt mức để tiền nạp mua được thứ người không nạp không đạt được (0004).

## Tiêu chí hoàn thành
- [ ] Mục tiêu đo được đã ghi và đã đạt theo output thật của `balance.py`.
- [ ] `balance.py check` exit 0 (khi có bảng chỉ số).
- [ ] Số liệu + lý do + lệnh đã chạy được ghi vào tài liệu design/; số chưa duyệt có `[ý tưởng]`.
- [ ] Tác động chéo đã liệt kê và xử lý.
- [ ] `python3 tools/check_design.py` exit 0.
