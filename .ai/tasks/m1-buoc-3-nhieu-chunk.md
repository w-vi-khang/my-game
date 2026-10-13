# Task: M1 bước 3 — nhiều chunk song song, nối biên, đo thời gian

## Mục tiêu
Dựng mesh cả 32 chunk của world M1 song song bằng Jobs + Burst, mặt liền ở biên chunk, đo thời gian dựng ([world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1) bước 3). Kèm: mép map lấy mẫu bằng cột biên gần nhất → không sinh vách đứng ([0009](../../design/decisions/0009-dai-duong-va-suong-mu-o-ria-map.md)).

## Ngoài phạm vi
Đảo + vành biển, mặt nước, sương mù (bước 5b); màu vật liệu (bước 5); đào/đặt (bước 4); đo trên điện thoại (bước 6).

## Tiêu chí chấp nhận
- [x] Không hở ở biên: ghép mesh cả map theo vị trí đỉnh, mọi cạnh tam giác dùng chung đúng 2 tam giác, trừ cạnh ở mép map — kiểm bằng: test EditMode.
- [x] Đỉnh trùng nhau ở biên hai chunk có cùng pháp tuyến — kiểm bằng: test EditMode.
- [x] Không còn vách đứng ở mép map (không có mặt hướng ra ngoài ở x/z = 0, 64) — kiểm bằng: test EditMode.
- [x] Dựng song song cho kết quả giống dựng từng chunk — kiểm bằng: test EditMode.
- [x] Đo thời gian dựng 32 chunk (song song) và trung bình mỗi chunk trên Mac — ghi số [run].
- [x] Scene TerrainView hiện cả map + ảnh render.
- [x] Ghi giờ thực tế (ước lượng 4–7 ngày-người).

## Quyết định đã chốt
- Kế hoạch prototype — user duyệt 2026-10-13; 0009 (đại dương + sương mù) — user 2026-10-13.

## Kết quả (2026-10-13)
- `python3 tools/unity_test.py` → 34/34 đạt.
- Mac (Editor, Burst): 32 chunk song song ~29 ms; 1 chunk ~4,3 ms. Cả map 61.800 tam giác, chunk nặng nhất 3.800.
- 4/93.058 cạnh không đa tạp, nằm gọn trong chunk (tính chất Surface Nets) — test cho phép, không do nối biên.
- Ảnh cả map: `client/Logs/terrain_view.png`.
- Thời gian: 15:30–15:33.
