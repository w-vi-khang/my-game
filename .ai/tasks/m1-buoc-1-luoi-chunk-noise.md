# Task: M1 bước 1 — lưới gameplay, chunk 16³, địa hình noise theo seed

## Mục tiêu
Nền dữ liệu world của prototype M1 ([world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1) bước 1, [0007](../../design/decisions/0007-m1-toi-gian.md)): map 64 × 64 × 32 ô (32 chunk 16³), mỗi ô 2 byte, sinh địa hình bằng noise với một seed cố định, tất định giữa các máy.

## Ngoài phạm vi
- Dựng mesh, hiển thị địa hình (bước 2–3), đào/đặt (bước 4), hang, lưu world, nhiều vùng, cây, vật thể.
- Bỏ các package khác của template ngoài AI Navigation (Visual Scripting, Timeline, Multiplayer Center...) — để quyết sau.

## Tiêu chí chấp nhận (kiểm chứng được)
- [x] Assembly `Game.Core` bật No Engine References; `Game.World` (Burst, Collections, Mathematics); `Game.Client`; test `Game.Core.Tests`, `Game.World.Tests` (EditMode) — kiểm bằng: `python3 tools/unity_test.py` biên dịch + chạy được.
- [x] Ô 2 byte đúng bố cục world-tech (vật liệu 8 bit, cờ cốt truyện, cờ đã sửa, mực nước 3 bit, dự phòng 3 bit); chỉ số ô ↔ chunk đúng ở biên — kiểm bằng: test EditMode.
- [x] Cùng seed → cùng world (mã băm giống), seed khác → mã băm khác — kiểm bằng: test EditMode.
- [x] Địa hình đúng luật: 2 lớp đáy là đá không đào được (đánh dấu), cỏ trên cùng, đất 3 ô, đá bên dưới, cát ở ô thấp gần mực nước, hồ nước tĩnh ở chỗ trũng, mặt đất trong khoảng y 8–20 — kiểm bằng: test EditMode.
- [x] Sinh địa hình chạy bằng job Burst `FloatMode.Deterministic`, biên dịch đồng bộ — kiểm bằng: test xác nhận Burst đang bật + đọc code.
- [x] Thông số địa hình đọc từ JSON `design/data/world_m1.json`; bản trong client giống hệt nguồn — kiểm bằng: test so nội dung.
- [x] Mã băm world trên điện thoại Android ARM64 trùng mã băm trên Mac — kiểm bằng: scene HashCheck build APK, cài qua `adb`, so số hiện trên màn hình với test.
- [x] Dọn template: xoá `Assets/TutorialInfo`, `Assets/Readme.asset`, package `com.unity.ai.navigation` — kiểm bằng: `client_test` vẫn exit 0.
- [x] Ghi giờ thực tế so với ước lượng 3–5 ngày-người (AGENTS.local).

## Ràng buộc
- Unity 6000.4.8f1; Burst 1.8.29, Collections 6.4.0, Mathematics 1.3.3 (bản đang được kéo gián tiếp — chuyển thành phụ thuộc trực tiếp, không thêm thư viện mới).
- `Game.Core` không dùng UnityEngine → đọc JSON ở `Game.Client` bằng `JsonUtility` vào class `[Serializable]` khai báo trong Core.
- Mô phỏng/luật dùng số nguyên; số thực chỉ trong job Burst sinh địa hình.
- Unity phải tắt khi chạy `client_test`.

## Quyết định đã chốt (ai chốt, khi nào)
- Nền tảng code M1, kế hoạch prototype — user duyệt 2026-10-13.
- Có máy Android để thử; địa hình theo đề xuất của AI (đồi thoải, y 8–20, lớp cỏ/đất/đá, cát, một hồ) — user 2026-10-13.

## Kết quả (2026-10-13)
- `python3 tools/unity_test.py` → 23/23 đạt; script trả exit 1 khi có test lỗi (đã thử với test cố ý lỗi, đã xoá).
- Mã băm chuẩn `86b0747d4b2be65b` (generatorVersion 1) — Android 14 ARM64 (24095PCADG) log `HASHCHECK hash=86b0747d4b2be65b burst=True ms=3`.
- Thời gian: 14:59–15:12.

## Câu hỏi còn mở
- Mac của user là ARM64 → bước này chỉ so ARM64 macOS với ARM64 Android; máy x64 (PC Windows) chưa kiểm — cần một máy x64 sau này.
- Lòng hồ thấp hơn y 8 (tới y 7) — test cho phép dưới minSurface chỉ khi có nước phía trên; tiêu chí "mặt đất y 8–20" hiểu là ngoài lòng hồ.
