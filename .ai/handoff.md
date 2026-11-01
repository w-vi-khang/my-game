# Handoff — 2026-11-01 (Planner, task 5j)

## Đang làm
Task [m1-world-5j-sua-loi-choi-thu](tasks/m1-world-5j-sua-loi-choi-thu.md). User chơi thử ngày 2026-11-01: **phần nước đạt**. Lỗi 13 (cột thác bo góc kề khối đặc) đã xong qua brief 0092, review đạt ở commit `263509e`. Còn lỗi 14–16, ghi trong task ở commit `b1101d3`.

## Việc tiếp
1. **Lỗi 16** (dễ, viết brief ngay):
   - Đổi tên GameObject "Người chơi" → "Player" trong `client/Assets/Game/Client/Scenes/{SimView,TerrainView,WalkBench}.unity`. Trong YAML scene, tên được ghi dạng `m_Name: "Người chơi"`.
   - Kiểm code/test có tìm object theo tên này không: grep `"Player"` và `Find(`.
   - Mở rộng `HardcodedTextScanTests` để quét `m_Name` trong `.unity`/`.prefab`. Lưu ý: tên đã được escape `\uXXXX`.
   - User hỏi vì sao vẫn còn tên tiếng Việt dù đã có luật code tiếng Anh. Trả lời: task `m1-code-tieng-anh` chỉ quét file `.cs`, scene nằm ngoài phạm vi.
2. **Lỗi 14 + 15** (mép cỏ ở bậc thang thấp: phần đen + mảng sáng vá đầu dải; mesh quá nhọn):
   - Ảnh user lưu ngoài repo: `~/Documents/User/khang/Game/art-test/shots/user5j_grass_lip_dark_patch.png`, `user5j_terraces_sharp.webp` (bậc thang cỏ phía nam trại SimView).
   - Trước khi viết brief, Planner dựng lại bậc thang trong world test 16³, chụp gần bằng camera tạm (`execute_code`: RenderTexture → PNG vào scratchpad). Mẫu chụp đã dùng cho cột thác trong phiên này.
   - Giả thuyết lỗi 14: đáy trong `f0`/`f1` của dải cỏ không thon theo mặt cắt (`BlockMesher.cs:888`), và `AddLipConcave` (`:784`) vẫn vá đầu dải đã thon.
   - Lỗi 15: xác định chỗ nhọn là đầu thon 0091 (`LipTaperLength` 0,5 [số tạm]) hay `AddLipCorner`.
3. Lỗi 14–16 đạt + user xem → đóng task 5j, cập nhật world-tech dòng 5j + roadmap.
4. **5k — nước kiểu ảnh tham khảo** (user 2026-11-01: "tôi muốn nước giống như ảnh 3"):
   - Ảnh lưu ngoài repo: `art-test/shots/ref_water_style_5k.webp`. Bãi cát nhìn từ trên; nước đổi màu theo độ sâu (xanh ngọc nhạt sát bờ → xanh đậm ngoài xa); nhiều vệt bọt trắng song song bờ; đáy thấy rõ qua nước; lấp lánh mặt trời.
   - Khớp mục "Hiển thị dòng chảy" [ý tưởng] ở world-tech (5k, ước lượng 4–8).
   - Cần task contract mới, chốt với user: phạm vi (màu theo độ sâu, bọt bờ, caustic, vệt chảy), thứ tự, FPS điện thoại. Không lưu ảnh tham khảo vào repo.

## Ghi chú
- Phiên Executor `exec-0088` = `local_b7728311-1023-4b48-a428-fa77c2f00dbe` (Opus · medium). Dọn bằng `clear_session` trước mỗi brief mới.
- Số tạm: `basinFillRadius` 16, `LipTaperLength` 0,5. User chưa nhận xét hai số này.
- Ảnh tham khảo nước (5k) là game của người khác. Chưa có tên/nguồn, không ghi tên game khi chưa có nguồn.
