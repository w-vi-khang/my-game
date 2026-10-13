# 0013 — Nước biển lan theo nhịp (thay "loang ngay" của 0012)

> Trạng thái: Chốt
> Ngày: 2026-10-13
> Thay thế: chữ "loang **ngay**" trong [0012](0012-bien-tran-va-luat-dat-khoi-m1.md) (các luật khác của 0012 giữ nguyên).

## Bối cảnh
- User thử bước 5c trong Editor (2026-10-13): nước tràn vào "khá nhanh, gần như ngay lập tức, không như nước".
- 0012 chốt biển loang tức thời; nước chảy đầy đủ (mực 0–7, bảo toàn lượng nước) là M2, 8–15 ngày-người ([world-tech](../04-tech/world-tech.md#nước-chảy)).

## Các lựa chọn
- **A. Lan theo nhịp:** vẫn luật 0012 (mỗi ô đầy hoặc rỗng, trạng thái cuối giống hệt), nhưng nước rơi xuống ngay trong cùng nhịp và lan ngang/dâng lên 1 ô mỗi nhịp (nhịp cố định) — ~0,5–1 ngày-người.
- **B. Nước chảy đầy đủ ngay M1:** +8–15 ngày-người vào M1 đang vượt quỹ.
- **C. Giữ tức thời**, chảy dần làm ở M2.

## Quyết định
**Chốt (user, 2026-10-13): A.**
- Đào ô ≤ mực nước biển S kề nước (hoặc biển ngoài map) → ô đó vào **mặt trước dòng nước**. Mỗi nhịp: mọi ô ở mặt trước còn rỗng thành nước; từ mỗi ô vừa ngập, nước **rơi thẳng xuống** qua các ô rỗng ≤ S ngay trong nhịp đó; các ô rỗng ≤ S kề ngang hoặc phía trên các ô vừa ngập vào mặt trước của nhịp sau.
- Nhịp cố định (mặc định 8 nhịp/giây, chỉnh ở client) → tất định, không phụ thuộc khung hình. Trạng thái cuối giống luật 0012.
- Đang lan mà người chơi đặt khối vào ô ở mặt trước → ô đó không ngập, nước dừng ở đó (đặt khối chặn được dòng nước). Đào thêm chỗ khác kề nước → thêm vào mặt trước.

## Hệ quả
- Ước lượng (AI): +0,5–1 → làm tròn **+1 ngày-người**. M1: 50–84 → **51–85**; tổng tới M4 không đổi khi làm tròn (~12,0–22,7 năm).
- Cập nhật: [mechanics](../02-gameplay/mechanics.md#voxel-world--luật-m1) (luật biển tràn), [world-tech](../04-tech/world-tech.md) (mục Nước, bước 5c), [roadmap](../05-roadmap.md).
- Code: `WorldEditor` giữ mặt trước dòng nước, `StepWater()` chạy một nhịp, `SettleWater()` chạy tới khi yên (test, tua nhanh sau này); client gọi `StepWater()` theo nhịp cố định.
