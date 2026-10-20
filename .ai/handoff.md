# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-20 22:55

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7](tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7c minimap** (`design/04-tech/world-tech.md:177`). Tách 2 brief: **0015 lõi** (Game.Core, đã exec) và **0016 giao diện** (uGUI, phím M, chuột, chụm 2 ngón, Bench APK — chưa viết).

## Đã làm (kèm kết quả kiểm chứng)
- Brief [0015](briefs/0015-world-minimap-loi.md) viết (commit `21efd8e`), executor Sonnet · medium làm xong: code `deedb6d`, kết quả `ad28e6f`, trạng thái `chờ review` [repo]. Theo executor: 29/29 test mới, EditMode 301/301, mã băm world/bảng map không đổi, ảnh nền dựng 15,5 ms trên Mac.

## Còn lại
1. **Review 0015** (`/ai-workflow-kit:review-brief 0015`, Opus · medium): đọc diff `dae0b43..deedb6d`; gửi user ảnh `client/Builds/world_maps_preview.png` để xem bảng màu. Điểm cần nêu với user: world thật chỉ có **dải nước 0–1** (biển sâu tối đa 4 ô: đáy 8, mặt nước 12) → 2 dải đậm (5–6, ≥ 7) không bao giờ hiện; chỉ có tầng đất 3–8. Có thể đề xuất chia lại dải nước (vd 1, 2, 3, ≥ 4) — user quyết, sửa trong brief 0016.
2. Ghi giờ 7c phần 1 (executor 22:46–22:50 = 4 phút) vào roadmap khi duyệt.
3. **Viết brief 0016** sau khi 0015 duyệt. Đã chốt/ tra sẵn (phiên 2026-10-20):
   - uGUI 2.0 (tech-stack chọn), asmdef `Game.Client` phải thêm tham chiếu `UnityEngine.UI` (asmdef ở `Packages/com.unity.ugui/Runtime/UGUI/UnityEngine.UI.asmdef`) [run].
   - Canvas Screen Space Overlay + `CanvasScaler.ScaleMode.ScaleWithScreenSize` (enum có `ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize`; `ScreenMatchMode`: `MatchWidthOrHeight, Expand, Shrink`) [run]; `RawImage.texture`, `RawImage.uvRect` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.RawImage.html)) ← `MinimapViewport.VisibleUv`; `RectMask2D` cắt `MaskableGraphic` (IClippable) [run].
   - Ranh giới + dấu người chơi: lớp con `MaskableGraphic`, `protected override void OnPopulateMesh(VertexHelper vh)` ([Graphic](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Graphic.html), [VertexHelper](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.VertexHelper.html): `Clear`, `AddVert(Vector3, Color32, Vector4)`, `AddTriangle`), `SetVerticesDirty()` khi zoom/kéo.
   - Tên map: `UnityEngine.UI.Text` (còn trong ugui 2.0, menu Legacy — [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Text.html)) + `Outline` (`effectColor`, `effectDistance`) [run]; font `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` — **không có trang docs 6000.4**, chạy được trong Editor (trả font động) [run], tên "LegacyRuntime" có trong `unity default resources` của Editor và AndroidPlayer [run]; brief nên có bước kiểm khác null trên điện thoại.
   - Texture: `Texture2D(384, 384, RGBA32, false)`, `SetPixels32` hàng dưới trước + `Apply` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.SetPixels32.html)); FilterMode.Point, wrap Clamp.
   - Input System 1.19: `Touchscreen.current.touches` (10 phần tử), `TouchControl.position/press/isInProgress/delta` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Controls.TouchControl.html)); `Keyboard.mKey`, `escapeKey` [run]; nút mở/đóng tự kiểm bằng `RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pos, null, out local)` (Overlay → cam null, [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html)) — không cần EventSystem.
   - Khi bản đồ mở: `TerrainEditInput` bỏ qua chuột (đào/đặt, lăn chọn loại); camera bay vẫn chạy phím để thấy dấu người chơi di chuyển. Vị trí người chơi = Camera.main (chưa có nhân vật).
   - Thử chụm 2 ngón trên điện thoại: Bench là bản sao TerrainView (menu "Game/Tạo scene Bench") nên có minimap; sau khi đo xong (camera dừng) user bấm nút bản đồ và chụm thử.
4. Exec 0016 → review (user xem + FPS Bench) → 7d → 7e → sim 6a.

## Quyết định quan trọng & lý do
- Lõi minimap thuần C# trong Game.Core để test EditMode; UI tách brief riêng cho brief vừa một phiên.
- Đo FPS điện thoại: xem quy trình Bench trong commit `ca90b92` (`git show ca90b92:.ai/handoff.md`).

## File liên quan
- `.ai/briefs/0015-world-minimap-loi.md`, `client/Assets/Game/Core/Minimap*.cs`, `client/Assets/Game/Client/TerrainEditInput.cs`, `client/Assets/Game/Client/DebugFlyCamera.cs`, `client/Assets/Game/Client/Editor/BenchBuild.cs`, `design/04-tech/tech-stack.md:26`.

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — world 7c: brief 0015 (lõi minimap) đã exec, chờ review.
Bước tiếp theo: /ai-workflow-kit:review-brief 0015 (gửi user ảnh xem trước bảng màu, nêu chuyện dải nước), rồi viết brief 0016 giao diện minimap.
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (review + viết brief, AGENTS.md §10.2).
```
