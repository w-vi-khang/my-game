# 0035 — Camera quan sát: đặt trên ô (sớm hơn 7l c), xoay khi đang xem; số camera PC sau chơi thử 7l b

> Trạng thái: Chốt
> Ngày: 2026-10-23
> Thay đổi: [0034](0034-dieu-khien-pc-click-di-chuyen.md) mục 2 (số tạm camera 55° / 14 ô → 10° / 9 ô) và mục 4 (camera quan sát: hướng lúc đặt = cả góc ngang + góc cúi của camera chính; đặt trên ô làm ngay ở bản sửa 7l b, chưa cần vật phẩm) · [0029](0029-nhieu-diem-nhin-camera-quan-sat.md) mục 2 (thêm xoay khi đang xem).

## Bối cảnh
User chơi thử 7l b (brief 0055 + 0056) ngày 2026-10-23 và phản hồi:
- Camera mặc định quá cao/xa; xoay chuột giữa quá chậm.
- Muốn xoay được camera quan sát khi đang xem.
- **Lỗi:** đặt camera quan sát rồi bấm xem (V / nút) → "không thấy gì cả". User làm rõ: **hình không đổi**.

Planner điều tra trong phiên Play của user [run, 2026-10-23]:
- Xem / quay về chạy đúng: camera chuyển tới điểm đặt, nạp vùng xa (46 cột thiếu) xong sau 24 khung rồi hiện hình; console không lỗi.
- Công cụ Camera hiện đặt camera quan sát **đúng bằng vị trí + hướng của camera đang nhìn** (`client/Assets/Game/Client/TerrainEditInput.cs:197–202` [repo]). Góc điểm đặt trong phiên của user (12,02°, 239,82°) trùng khít góc camera chính lúc đó. Vì vậy bấm xem ngay sau khi đặt thì hình y hệt → "hình không đổi". Đây là cách đặt tạm của brief 0053, không phải lỗi bộ nạp hay camera.
- 0034 mục 4 đã định cách đặt mới (chuột phải lên ô → camera trên ô đó 2 ô), nhưng xếp ở 7l c cùng vật phẩm trong túi.

## Các lựa chọn
- **Sửa lỗi:** (a) đưa cách đặt trên ô vào bản sửa 7l b (brief 0057), dùng công cụ Camera hiện có — user chọn; (b) để 7l c làm.
- **Góc cúi lúc đặt:** 20° [số tạm] (đề xuất Planner) / 0° ngang hẳn / **theo góc camera chính** — user chọn.
- **Xoay khi xem:** user chọn giữ chuột giữa + di chuột, hướng mới được giữ.

## Quyết định
User, 2026-10-23:
1. **Số camera PC** [số tạm] (sửa số tạm của 0034 mục 2):
   - góc cúi mặc định **10°**, khoảng cách mặc định **9 ô** (`design/data/sim_m1.json` player `cameraPitch`, `cameraDistance`);
   - độ nhạy xoay mặc định **0,3** (trước 0,15; Cài đặt 0032).
   - File cài đặt cũ (`settings.json`) đặt lại độ nhạy, góc cúi, khoảng cách về mặc định mới một lần (nâng phiên bản định dạng).
2. **Đặt camera quan sát trên ô** (làm ngay ở brief 0057, chưa cần vật phẩm):
   - Cầm công cụ Camera, chuột phải lên một khối trong tầm thao tác → camera đặt ở **giữa ô, cao hơn mặt trên khối đó 2 ô** [số tạm].
   - Hướng = **góc ngang + góc cúi của camera chính** lúc đặt.
   - Ngoài tầm → không đặt. Chưa tự đi tới 2 ô (phần đó vẫn ở 7l c).
   - 7l c chỉ còn: camera quan sát thành vật phẩm trong túi + tự đi tới 2 ô.
3. **Xoay camera quan sát khi đang xem:**
   - Giữ chuột giữa + di chuột → xoay ngang + dọc. Độ nhạy và đảo trục dọc theo Cài đặt (như camera chính).
   - Góc cúi kẹp trong [số tạm] −80°…85°.
   - Không thu phóng, không di chuyển; Space / X không có tác dụng khi đang xem.
   - **Hướng mới được giữ** cho lần xem sau, tới khi đặt lại camera (đặt lại → hướng theo camera chính lúc đặt mới).

## Hệ quả
- **Pillar:** P3 "Tự do trong khung truyện" — camera quan sát nhìn được đúng chỗ muốn theo dõi.
- **Thành phần:** client Unity (`ObservationCamera`, `TerrainEditInput`, `FollowCamera`); `PlayerSettings` + `PlayerMotion` ở `Game.Core`; số trong `design/data/sim_m1.json`.
- **Ước lượng (của AI, chưa xác minh):** 0,5 ngày-người, nằm trong quỹ 7l (14–20). Phần đặt trên ô chuyển từ 7l c sang 7l b → 7l c bớt tương ứng; tổng 7l không đổi.
- **Cập nhật:** [controls-ui](../02-gameplay/controls-ui.md), [glossary](../glossary.md) (Công cụ Camera), [world-tech](../04-tech/world-tech.md) 7l b + 7l c, 0034 (ghi chú), `decisions/README.md`. Brief sửa: `.ai/briefs/0057`.
