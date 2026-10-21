# Task: M1 world bước 7g — ô minimap nhỏ luôn hiện ở góc, bấm để phóng to

> Trạng thái: user quyết 2026-10-21 (kèm ảnh tham khảo: minimap tròn ở góc, chữ N ở đỉnh vòng, mũi tên người chơi ở giữa). Ước lượng 1–1,5 ngày-người [ý tưởng, ước lượng của AI]. Thuộc hạng mục minimap M1 của [0022](../../design/decisions/0022-world-nhieu-map-va-minimap-m1.md) (0022 mục 5 không nói "nút", nên không trái phần đã Chốt). Một brief.

## Mục tiêu
Thay nút "Bản đồ (M)" bằng một **ô minimap tròn luôn hiện** ở góc trên-phải. Ô này hiện vùng quanh người chơi, cố định Bắc lên trên, có chữ "N" ở đỉnh vòng. Bấm hoặc chạm vào ô thì mở bản đồ lớn hiện có (phóng to/kéo như cũ). Phím M vẫn mở/đóng, Esc vẫn đóng.

Xong khi: test PlayMode đạt (ô nhỏ dựng được, theo vị trí người chơi, bấm/chạm mở bản đồ lớn, chặn đào xuyên qua ô, cập nhật điểm ảnh khi đào/đặt thì ô nhỏ cũng thấy); test cũ của bản đồ và vòng thời điểm đạt; FPS điện thoại ≥ 30; user xem trên Mac và điện thoại.

## Đã chốt với user (2026-10-21)
1. Vị trí: **góc trên-phải, thay chỗ nút "Bản đồ (M)"**.
2. Hướng: **cố định Bắc lên trên**. Mũi tên người chơi xoay theo camera. Chữ "N" cố định ở đỉnh vòng.
3. Nội dung ô nhỏ: **ảnh nền + vị trí người chơi, ranh giới map, dấu mốc (biểu tượng, không tên), tên map** đang đứng.
4. Nút chọn thời điểm (brief 0024) **giữ riêng**, chỉ dời xuống dưới ô minimap cho khỏi chồng; vị trí cuối user chỉnh sau.
5. Biểu tượng mặt trời gắn mép minimap trong ảnh tham khảo là **thời tiết**: làm sau, đã ghi vào [ideas](../../design/ideas.md).

## Planner chốt khi viết brief [ý tưởng, chưa hỏi user — đều là số tạm, user chỉnh khi xem]
- Cỡ ô: đường kính khoảng 200 (đơn vị tham chiếu 1280 × 720). Vùng hiện khoảng 64 × 64 cột quanh người chơi.
- Tên map hiện bên trong ô, sát mép dưới, có viền chữ.
- Khi bản đồ lớn mở: ô nhỏ ẩn, trong bảng có nút "Đóng (M)" ở góc trên-phải.
- Mặt nạ tròn: tra docs uGUI 2.0 (`Mask` dùng Graphic tự vẽ làm stencil, hoặc cách khác) trước khi viết brief.
- Dời `TimeDial`: nút, dòng buổi và vòng 4 mốc xuống dưới ô minimap.

## Ngoài phạm vi
Thời tiết + biểu tượng thời tiết, ô nhỏ xoay theo camera, sương che vùng chưa khám phá, dịch chuyển/đường chỉ dẫn (M2, 0023), đổi luật phóng to/kéo của bản đồ lớn.

## File liên quan
- `client/Assets/Game/Client/MinimapView.cs` (nút ở `:396-400`, `Refresh` ở `:260-306`, `Build` ở `:308-410`), `MinimapViewport` (Game.Core), `MinimapLinesGraphic.cs`, `MinimapLandmarkGraphic.cs`, `MinimapMarkerGraphic.cs`
- `client/Assets/Game/Client/TimeDial.cs` (vị trí nút, vòng, dòng buổi), `TimeDialGraphic.cs` (hình `Disk` dùng làm nền tròn)
- `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs`, `TimeDialPlayTests.cs`
- [world-tech 7g](../../design/04-tech/world-tech.md), [controls-ui](../../design/02-gameplay/controls-ui.md)
