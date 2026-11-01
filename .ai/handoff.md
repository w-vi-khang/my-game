# Handoff — 2026-11-01 (Planner, task 5j, cập nhật 21:30)

## Đang làm
Task [m1-world-5j-sua-loi-choi-thu](tasks/m1-world-5j-sua-loi-choi-thu.md). User chơi thử ngày 2026-11-01: **phần nước đạt**. Lỗi 13 (cột thác bo góc kề khối đặc) đã xong qua brief 0092, review đạt ở commit `263509e`. Còn lỗi 14–16, ghi trong task ở commit `b1101d3`.

## Việc tiếp
1. ~~Lỗi 16~~ **xong**: brief 0093 đã duyệt (`945b006`). Play Mode sót lại đã dừng.
2. **Lỗi 14 + 15**: đã dựng lại, chụp và user đã chọn hướng sửa (ghi trong task, mục 14–15).
   - **Việc tiếp: viết brief.**
     - (a) Đầu thon dạng 1/4 elip, độ dài 0,3 [số tạm]: `TaperAt` trong `AddLipEdge`, `BlockMesher.cs:841`, hằng `LipTaperLength` `:57`.
     - (b) Đỉnh mép cỏ (`AddLipEdge` + `AddLipConcave`) nhận AO bằng mức AO mặt trên khối ở hai đầu cạnh, nội suy dọc cạnh. Xem `AoLevel` `:660`, `Shade(color, AoBrightness[ao])` `:651`.
   - Brief phải trích chữ ký `Shade`/`AoBrightness` (chưa đọc). Phải chọn cách tính AO đỉnh mặt trên ở đầu cạnh từ ô lân cận y+1.
   - Test 0091 `LipTaper*` cần đổi kỳ vọng f(0,25).
   - Mẫu dựng world thử + chụp (codedom, C# 6, không dùng tuple): thế giới 16³, cột `hh`, `BlockMesher.Build`, `ChunkMeshWriter.Write`, material `Game/Terrain` + `TerrainChunkView.waterMaterial`, camera `CopyFrom(Camera.main)`, RenderTexture → PNG.
3. Lỗi 14–16 đạt + user xem → đóng task 5j, cập nhật world-tech dòng 5j + roadmap.
4. **5k — nước kiểu ảnh tham khảo** (user 2026-11-01: "tôi muốn nước giống như ảnh 3"):
   - Ảnh lưu ngoài repo: `art-test/shots/ref_water_style_5k.webp`. Bãi cát nhìn từ trên; nước đổi màu theo độ sâu (xanh ngọc nhạt sát bờ → xanh đậm ngoài xa); nhiều vệt bọt trắng song song bờ; đáy thấy rõ qua nước; lấp lánh mặt trời.
   - Khớp mục "Hiển thị dòng chảy" [ý tưởng] ở world-tech (5k, ước lượng 4–8).
   - Cần task contract mới, chốt với user: phạm vi (màu theo độ sâu, bọt bờ, caustic, vệt chảy), thứ tự, FPS điện thoại. Không lưu ảnh tham khảo vào repo.

## Ghi chú
- Phiên Executor `exec-0088` = `local_b7728311-1023-4b48-a428-fa77c2f00dbe` (Opus · medium). Dọn bằng `clear_session` trước mỗi brief mới.
- Số tạm: `basinFillRadius` 16, `LipTaperLength` 0,5. User chưa nhận xét hai số này.
- Ảnh tham khảo nước (5k) là game của người khác. Chưa có tên/nguồn, không ghi tên game khi chưa có nguồn.
