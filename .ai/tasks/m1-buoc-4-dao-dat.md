# Task: M1 bước 4 — đào/đặt nguyên ô, dựng lại chunk bẩn, collider, kho vật liệu

> Trạng thái: user xác nhận 2026-10-13 15:45 (điều khiển như đề xuất, kho bắt đầu 0) — xong phần AI kiểm được, chờ user thử bằng chuột

## Mục tiêu
Người chơi đào/đặt nguyên ô trên world M1; chunk bị ảnh hưởng dựng lại mesh và bake lại collider; kho vật liệu đếm theo loại ([world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1) bước 4, luật ở [mechanics](../../design/02-gameplay/mechanics.md#voxel-world--luật-m1)).

## Ngoài phạm vi
- Nhân vật người chơi, Cinemachine, HUD thật/thanh công cụ thật — chưa có trong hai kế hoạch prototype; bước này chỉ dùng camera debug + chuột.
- Màu vật liệu (bước 5), đảo/biển/sương mù (5b), đo mobile (6), lưu/nạp chunk đã sửa (M2), tìm đường + môi trường sống cập nhật theo sửa ô (simulation bước 2, 8 — bước này chỉ phát sự kiện "ô đã đổi" để sau dùng).
- Vật thể chiếm ô (sinh vật, cây, kho) chưa tồn tại → chỉ có chỗ móc kiểm tra "ô bị chiếm", chưa có vật thể thật.

## Tiêu chí chấp nhận (kiểm chứng được)
- [x] **Luật đào** (Game.Core): ô `Cell.IsDiggable` → thành rỗng, bật cờ đã sửa, kho +1 đúng loại; đá đáy / nước / rỗng / khối cốt truyện / ngoài map → từ chối, world và kho không đổi — kiểm bằng: test EditMode.
- [x] **Luật đặt** (Game.Core): chỉ đặt vào ô **rỗng** trong map, kho loại đó ≥ 1 → tiêu 1, bật cờ đã sửa; hết vật liệu / ô không rỗng (kể cả nước) / ô bị chiếm (móc kiểm tra) / ngoài map / vật liệu không đặt được (nước, đá đáy) → từ chối — kiểm bằng: test EditMode.
- [x] **Chunk bẩn:** sửa một ô → danh sách chunk cần dựng lại gồm chunk chứa ô + mọi chunk bên cạnh (kể cả chéo) mà ô nằm trong vùng đọc của chunk đó. Vùng đọc thật của mesher là **2 ô** quanh chunk (mẫu −3..34 ở ×2 → ô −2..17), không phải 1 như world-tech dòng 74 — kiểm bằng: test EditMode.
- [x] **Dựng lại tăng dần = dựng lại toàn bộ:** sau ~200 lần đào/đặt ngẫu nhiên (seed cố định), mesh các chunk dựng lại theo danh sách bẩn giống hệt `BuildAll` trên world đã sửa — kiểm bằng: test EditMode.
- [x] **≤ 2 khung hình:** sửa ô qua API trong Play → mesh của mọi chunk bẩn đã cập nhật chậm nhất 2 khung hình sau; collider cập nhật (raycast trúng bề mặt mới) — kiểm bằng: test PlayMode (`python3 tools/unity_test.py PlayMode`).
- [x] Đo trên Mac (Editor, Burst): thời gian dựng lại + ghi mesh + bake collider khi sửa ô giữa chunk (1 chunk) và ở góc chunk (tối đa 8 chunk) — ghi số [run].
- [ ] Scene TerrainView: chuột trái đào, chuột phải đặt, phím 1–4 chọn đất/cỏ/đá/cát, camera debug bay quanh; nhãn góc màn hình hiện kho — user thử trong Editor (AI không bấm chuột trong Unity được).
- [x] `python3 tools/unity_test.py` (EditMode) + PlayMode đều đạt; `python3 tools/check_design.py` exit 0.
- [x] Cập nhật world-tech (dòng 74 → vùng đọc 2 ô; hàng bước 4 có kết quả), roadmap "Theo dõi M1" ghi giờ thực tế (ước lượng 3–5).

## Ràng buộc
- Luật đào/đặt + kho trong `Game.Core` (C# thuần, No Engine References). Dựng mesh/bake trong `Game.World`/`Game.Client`, theo pattern Jobs + Burst đã có.
- Không thêm package mới. API `Physics.BakeMesh`, `MeshCollider.cookingOptions`, `Mesh.AllocateWritableMeshData` tra docs 6000.4 trước khi dùng.
- Không đổi bộ sinh → mã băm chuẩn `86b0747d4b2be65b` giữ nguyên.

## Cách làm (đề xuất)
1. Core: `MaterialInventory` (bộ đếm theo `BlockMaterial`, bắt đầu 0, không giới hạn) + `WorldEditor.TryDig/TryPlace` trả kết quả + danh sách chunk bẩn; sự kiện ô đã đổi.
2. World: dựng lại một tập chunk song song (tách từ `BuildAll`), dùng chung mảng ô Native giữ suốt phiên thay vì copy mỗi lần.
3. Client: `TerrainChunkView` giữ GameObject theo chunk (tạo khi chunk từ rỗng thành có mặt), Update gom chunk bẩn → schedule job → complete trong LateUpdate cùng khung hình → ghi mesh gom một lần `AllocateWritableMeshData(n)` → bake collider bằng job `Physics.BakeMesh` → gán `MeshCollider`.
4. Chọn ô bằng raycast vào collider: đào = ô ở `điểm trúng − pháp tuyến × 0,5`, đặt = `+ pháp tuyến × 0,5` (mesh mịn nên ô chọn có thể lệch ở góc — chấp nhận ở M1, có khung hiển thị ô đang chọn).
5. PlayMode test asmdef mới `Game.Client.Tests`; thêm `client_test_play` vào `.ai/project.json` verify.

## Quyết định đã chốt (ai chốt, khi nào)
- Kế hoạch prototype world (bước 4, tiêu chí ≤ 2 khung hình, ước lượng 3–5) — user duyệt 2026-10-13.
- Luật M1 đào/đặt, kho vật liệu không giới hạn — user duyệt 2026-10-13 (mechanics "Luật M1").

## Câu hỏi còn mở
- (đã trả lời, user 2026-10-13) Điều khiển như đề xuất; kho bắt đầu 0.

## Kết quả (2026-10-13)
- `python3 tools/unity_test.py EditMode` → 55/55 đạt (21 mới: `WorldEditorTests`, `IncrementalRebuildTests`); `python3 tools/unity_test.py PlayMode` → 3/3 đạt (`TerrainEditPlayTests`).
- Mesh + collider cập nhật trong **chính khung hình sửa ô** (chờ 0 khung hình): sửa trong Update → dựng lại trong LateUpdate (schedule + complete cùng khung hình).
- Mac (Editor, Burst), mỗi lần sửa: 1 chunk (TB 20 lần) dựng mesh 4,54 ms · ghi 0,07 ms · bake 0,92 ms; góc 8 chunk 8,26 · 0,28 · 1,32 ms. Dựng cả map lúc đầu ~28 ms + ghi 1,1 ms + bake ~4 ms.
- Khác "Cách làm" đề xuất: vẫn copy mảng ô sang Native mỗi lần dựng lại (256 KB, đã nằm trong 4,5 ms đo được) thay vì giữ mảng Native suốt phiên — đơn giản hơn, chưa cần.
- cookingOptions: UseFastMidphase | EnableMeshCleaning | WeldColocatedVertices (bỏ CookForFasterSimulation để bake nhanh hơn — chưa đo so sánh).
- Scene TerrainView đã có DebugFlyCamera + TerrainEditInput; ảnh chụp vẫn chạy (`client/Logs/terrain_view.png`).
- Thời gian: 15:45–15:53.

## Kết quả sau sửa lỗi + MCP for Unity (2026-10-13, phiên sau)
- User thử lần 1: `MissingReferenceException` (TerrainEditInput.cs:51, `Build()` xoá khung chọn) → sửa (chunk nằm trong con `Chunks`); không đặt được, 1–4 không thấy → sửa UX (dòng hướng dẫn, loại đang chọn vàng đậm, lăn chuột, lý do khi không được, chữ phóng theo màn hình).
- [run, qua MCP `run_tests`] EditMode 55/55; PlayMode **5/5** (thêm `BuildKeepsOtherChildren`, `EditInputSurvivesBuild`). Góc 8 chunk 8,04 · 0,29 · 1,44 ms, chờ 0 khung hình; 1 chunk (TB 20) 4,50 · 0,07 · 0,93 ms.
- [run, Play TerrainView qua MCP, chuột/phím **ảo** tiêm vào Input System — đi đúng đường `TerrainEditInput.Update`]: ô dưới chuột (22,13,22) là **Grass** (xác nhận lớp trên là cỏ → lỗi "không đặt được" lần 1 là do chọn "đất" khi kho đất 0). Chuột trái → `Đào (22, 13, 22): +1 cỏ`; phím 2 → chọn cỏ; chuột phải → `Đặt cỏ (22, 13, 22)`, kho cỏ về 0, dựng lại 1 chunk ngay khung hình sau; chuột phải lần nữa → "kho hết loại này…". Console: 0 lỗi/cảnh báo của game. Ảnh: `client/Temp/McpShots/02_after_place.png` (không commit).
- Cần đổi tạm (rồi trả lại) `InputSystem.settings` → `AllDeviceInputAlwaysGoesToGameView` + `IgnoreFocus` vì Editor không focus thì Input System bỏ sự kiện chuột ảo. Settings là bản mặc định trong bộ nhớ (không có asset) → không đổi file project.
- **Còn lỗi UX:** khung trắng chọn ô gần như không thấy — nét 0,04 đơn vị ≈ **1 px** ở khoảng cách mặc định 33 đơn vị, và khung bao ô đặc nên nửa dưới chìm trong mặt mesh mịn [run].
- MCP `run_tests` PlayMode tạm bật DisableDomainReload và ghi vào `ProjectSettings/EditorSettings.asset` (`m_EnterPlayModeOptions: 1`); bộ nhớ đã trả về `None` nhưng file thì không → đã `git checkout` file. Lưu ý mỗi lần chạy PlayMode qua MCP.
- Tiêu chí "user thử trong Editor" vẫn mở: AI chỉ kiểm bằng input ảo; chờ user thử chuột thật (camera bay, cảm giác chọn ô).
- **Sửa khung chọn** (user đồng ý 2026-10-13): shader `Game/SelectionOverlay` (unlit, `ZTest Always`, trong `Client/Resources/Shaders/` để build có shader — URP Unlit 17.4 không có thuộc tính ZTest [repo `Library/PackageCache/com.unity.render-pipelines.universal@*/Shaders/Unlit.shader`]) + nét giữ `outlinePixels = 3` px theo khoảng cách camera. [run] Ảnh `client/Temp/McpShots/08_input_off.png`: khung hiện đủ 12 cạnh, vẽ đè lên mặt đất, nét 0,131 đơn vị ở 33 đơn vị (≈ 3 px). Lưu ý cách kiểm: chuột ảo bị đọc là (0,0) lúc chụp → `Update` tắt khung; phải tắt `TerrainEditInput` tạm thời mới chụp được — không phải lỗi game (chuột thật trên Game view không gặp).
- [run] Sau sửa: EditMode 55/55, PlayMode 5/5, `check_design.py` exit 0; `EditorSettings.asset` bị MCP ghi lại lần nữa → đã `git checkout`.
- Thời gian thêm: sửa lỗi + MCP ~15:53–16:10 (theo giờ cập nhật handoff), phiên kiểm qua MCP + khung chọn ~16:34–16:50 → tổng bước 4 ≈ 0,75 giờ.
