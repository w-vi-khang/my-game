# Task: M1 world bước 7 — world 3 map liền nhau, ranh giới cong, minimap phóng to được, nạp dần chunk

> Trạng thái: user quyết 2026-10-19 ([0022](../../design/decisions/0022-world-nhieu-map-va-minimap-m1.md)). Làm sau sim 6b (xong), **trước sim 6a**. Chia brief theo bước 7a → 7e của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1); mỗi brief viết sau khi brief trước được duyệt.

## Mục tiêu
User muốn tập trung vào world: chia world thành các map có ranh giới, có minimap phóng to được để thấy toàn cảnh địa hình, ranh giới và tên từng map. Ước lượng 9–15 ngày-người [ý tưởng]. Xong khi:
- cùng seed → cùng world + cùng bảng map; mã băm trùng PC và điện thoại;
- world có 3 map, mỗi map khoảng 128 × 128 cột đất; ranh giới là đường cong (không thẳng, không hình chữ nhật); đại dương bao quanh cả world;
- minimap hiện toàn world từ trên cao: màu theo tầng độ cao, ranh giới, tên map (tên tạm trong JSON), vị trí người chơi, dấu mốc (công trình đặc trưng của từng map + 1 checkpoint mỗi map — [0023](../../design/decisions/0023-dau-moc-cong-trinh-checkpoint-va-dan-duong.md)); phóng to và kéo được bằng chuột và cảm ứng;
- đào/đặt → minimap đổi đúng cột đó;
- chỉ chunk quanh camera có mesh + collider; ≥ 30 FPS trên điện thoại khi di chuyển liên tục;
- mô phỏng (sim bước 1–6b) vẫn chạy đúng trên world mới.

## Quyết định (user, 2026-10-19 — 0022)
1. Làm ở M1, trước sim 6a.
2. World liền một khối, 3 map giáp nhau bằng đường cong ngẫu nhiên theo seed; đại dương + sương mù bao quanh cả world (sửa 0009).
3. Mỗi map khoảng 128 × 128 ô.
4. Tên tạm "Map 1/2/3" trong JSON; tên thật user đặt sau.
5. Minimap kiểu lai: nền từ bộ sinh (không dựng voxel), sửa từng cột khi đào/đặt, thêm dấu mốc; phóng to/kéo.
6. Nạp dần chunk quanh người chơi ngay ở M1.

## Planner đề xuất — chốt khi viết brief từng bước
- World 384 × 384 × 48, vành biển ≥ 24 ô; chia map bằng Voronoi có méo noise; bảng map 1 byte/cột.
- Dữ liệu world giữ cả trong RAM (~14 MB), chỉ mesh + collider nạp dần.
- Mỗi 4 ô độ cao một tầng màu; 1 điểm ảnh = 1 cột.
- Mô phỏng giới hạn trong vùng Map 1 (bộ nhớ tìm đường). **User chốt 2026-10-21 (7e):**
  - **vùng mô phỏng** = hộp 64 × 64 cột (đủ chiều cao), quanh trang trại demo; kích thước `regionSize` trong `sim_m1.json`; tìm đường cấp mảng theo vùng (~3,5 MB thay vì ~127 MB cả world);
  - trang trại demo tự tìm chỗ phẳng 9 × 7 cột trong Map 1, gần checkpoint Map 1, tránh dấu mốc;
  - scene SimView chuyển sang world 3 map (nạp dần), test mô phỏng cũ trên đảo v2 giữ nguyên.
  - Tách 2 brief: 0019 lõi (EditMode), 0020 scene SimView (PlayMode + user xem).

## Cách kiểm
- 7a (EditMode): tất định + mã băm; diện tích mỗi map ±25%; mỗi đoạn ranh giới lệch khỏi đường thẳng ≥ 8 ô; vành biển; mã băm trên điện thoại.
- 7b (PlayMode + điện thoại): không lỗ trước camera khi di chuyển; FPS, bộ nhớ, ms dựng mỗi khung.
- 7c (EditMode + user xem): ảnh nền tất định, đúng màu tầng; ranh giới khớp bảng map; user xem phóng to/kéo, tên, ranh giới.
- 7d (EditMode + PlayMode + user xem): đào/đặt → đúng một điểm ảnh đổi; vị trí công trình + checkpoint tất định, đúng map, trên đất khô; mã băm world không đổi; user xem dấu trên bản đồ và trong world.
- 7e (EditMode): test mô phỏng cũ đạt; `SimDemo` chạy trong Map 1.

## Ngoài phạm vi
- Dịch chuyển tới checkpoint + đường chỉ dẫn đi bộ (M2 — 0023); luật "không đào được" của khối giữ chỗ (khối cốt truyện, M2).
- Sương che vùng chưa khám phá ([ideas](../../design/ideas.md)); lưu world, hang, khối cốt truyện, điều kiện mở map (M2).
- Tên thật của map ([naming](../../design/01-story/naming.md) — user đặt).
- Tường sương mù theo khoảng cách tới rìa (shader, M2 — 0009).
