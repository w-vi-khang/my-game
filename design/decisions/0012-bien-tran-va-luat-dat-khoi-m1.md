# 0012 — Biển tràn theo mực nước + luật đặt khối mới (đưa vào M1)

> Trạng thái: Chốt
> Ngày: 2026-10-13
> Bổ sung: [0007](0007-m1-toi-gian.md) (M1 tối giản — "nước chảy" vẫn không thuộc M1; phần dưới là biển loang tức thời, không mô phỏng chảy). Sửa luật "không đào/đặt nước ở M1" trong [mechanics](../02-gameplay/mechanics.md#voxel-world--luật-m1).

## Bối cảnh
- Sau khi có nền đại dương (bước 5b, [0009](0009-dai-duong-va-suong-mu-o-ria-map.md)), user mô tả cách nước biển hoạt động và luật đặt khối (2026-10-13): nước biển tràn vào theo mực nước biển khi người chơi đào; đặt khối được trong không khí lẫn trong nước nhưng phải gắn vào một khối đặc.
- Luật M1 cũ (user duyệt 2026-10-13): nước tĩnh có sẵn, không đào/đặt nước; chỉ đặt vào ô rỗng. Nước chảy (mực 0–7) là M2 ([world-tech](../04-tech/world-tech.md#nước-chảy)).
- Mặt biển nền hiện phủ cả map ở y = waterLevel + 1 − 0,1 → vùng khô thấp hơn mực nước vẫn trông như có nước.

## Các lựa chọn
- **A. M1: luật đặt khối + biển loang + mặt nước theo ô** — ~3–5 ngày-người thêm vào M1 (đang vượt quỹ).
- **B. M1 chỉ luật đặt khối** (~0,5 ngày); biển loang để M2 cùng nước chảy.
- **C. Để hết sang M2.**

## Quyết định
**Chốt (user, 2026-10-13): A.** Luật (user xác nhận cách hiểu của AI, 2026-10-13; bỏ cơ chế "bơm cạn vùng kín" riêng):
- **Mực nước biển S** = `waterLevel` của world. Mỗi ô hoặc đầy nước hoặc không (M1 không có mực lưng chừng). Ngoài biên map, mọi ô y ≤ S là biển (nguồn vô hạn).
- **Đào** một ô y ≤ S kề mặt (6 hướng) với ô nước hoặc với biển ngoài map → nước **loang ngay** qua mọi ô rỗng y ≤ S thông với nó (6 hướng; khe chéo không thông). Ô y > S không bao giờ ngập.
- **Đặt khối** vào ô rỗng **hoặc ô nước**, với điều kiện ô đó **kề mặt ít nhất một khối đặc**. Đặt vào ô nước → nước ô đó mất, nước xung quanh giữ nguyên.
- **Nước không tự rút**: phá tường, đặt khối không làm khô vùng nào; nước chỉ xuất hiện khi loang.
- Hệ quả tự nhiên (không cần cơ chế riêng): vùng bị tường bao kín tới đáy, lấp hết ô nước rồi đào lại → khô; mở thông ra biển mà thấp hơn S → ngập lại. Tường không chạm đáy → nước luồn dưới chân tường.
- **Hiển thị:** mặt biển nền chỉ nằm ngoài map; trong map vẽ mặt nước theo từng ô nước có không khí phía trên.

## Hệ quả
- Ước lượng (AI): luật đặt khối 0,5 · biển loang (tìm vùng thông, giới hạn trong map, test) 1–2 · mặt nước theo ô + mặt biển ngoài map 1,5–2,5 → **3–5 ngày-người**. M1: 47–79 → **50–84**; tổng M1–M4 ~12,0–22,5 → **~12,0–22,7 năm** ở 10 giờ/tuần ([roadmap](../05-roadmap.md)).
- Cập nhật: [mechanics](../02-gameplay/mechanics.md#voxel-world--luật-m1) (luật M1), [world-tech](../04-tech/world-tech.md) (mục Nước + kế hoạch bước 5c), [glossary](../glossary.md) ("Mực nước biển"), [roadmap](../05-roadmap.md), [vision](../00-vision.md)/[summary](../summary.md)/[risks](../04-tech/risks.md) (con số tổng), [ideas](../ideas.md) (khối kính).
- Code: `Game.Core` — luật đặt (kề khối đặc, cho phép ô nước), biển loang khi đào (thuần C#, có test EditMode); `Game.World` — mesh mặt nước; `Game.Client` — mặt biển nền khoét phần map.
- Vật liệu **kính** (user nhắc khi hỏi ví dụ tường chữ U) chưa có ở M1 → ideas.
