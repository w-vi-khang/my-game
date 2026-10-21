# Task: M1 điều khiển — tách công cụ đào / đặt, bản mờ khi đặt

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2 — Executor làm theo brief; Planner Opus viết brief từ contract này)
> Trạng thái: user quyết 2026-10-21 (sau khi xem SimView brief 0026). Làm **trước** sim bước 7. Ước lượng 0,5–1 ngày-người [ý tưởng, ước lượng của AI], nằm trong hạng mục "Điều khiển (PC trước, cảm ứng cơ bản) 3–5" của M1 ([roadmap](../../design/05-roadmap.md)). Một brief.

## Mục tiêu
Đào và đặt thành **hai công cụ riêng**, mỗi công cụ nhắm một kiểu ([controls-ui](../../design/02-gameplay/controls-ui.md), mục "Nhắm theo công cụ"):
- **Đào:** tô viền trắng khối đang có dưới con trỏ, như hiện nay.
- **Đặt:** hiện **bản mờ** của khối sắp đặt ở ô trống sát mặt khối đang trỏ (`placeCell` sẵn có), không tô viền khối đã có.
- **Tab** chuyển Đào ⇄ Đặt. **Chuột trái** dùng công cụ đang cầm. Chuột phải không làm gì (dành cho "Huỷ" sau này).

## Ngoài phạm vi
- Cảm ứng: `TerrainEditInput` hiện chỉ dùng chuột, nên chưa thêm nút đào/đặt cho cảm ứng.
- Tâm nhắm giữa màn hình: vẫn nhắm theo vị trí chuột như hiện nay.
- Thanh công cụ đầy đủ (lệnh làm việc, thẻ bài, phép). Phím 1–4 / lăn chuột vẫn chọn loại vật liệu.
- Chọn vùng 2 điểm, đổi luật đặt/đào trong `WorldEditor`, khối cốt truyện.

## Đã chốt với user (2026-10-21)
1. **Tab** chuyển Đào ⇄ Đặt. Lý do chọn Tab: bộ gõ Telex dùng chữ s/f/r/x/j để bỏ dấu, dễ chặn phím chữ. **Chuột trái** = hành động chính. Chuột phải bỏ trống. HUD ghi công cụ đang cầm.
2. Bản mờ:
   - Có màu của vật liệu đang chọn, trong suốt khoảng 50%.
   - **Chỉ hiện ở ô đặt được theo luật:** ô rỗng hoặc ô nước, kề khối đặc, trong map, không bị chiếm.
   - **Kho hết loại đó** → bản mờ **đỏ**, kèm dòng lý do.
   - Ô không đặt được vì lý do khác → **không có bản mờ**.
3. Khi cầm công cụ đặt, không có khung trắng trên khối đã có. Khi cầm công cụ đào, không có bản mờ.

## Planner chốt khi viết brief [ý tưởng, chưa hỏi user — số tạm, user chỉnh khi xem]
- Thêm hàm kiểm không sửa dữ liệu trong `Game.Core`: `WorldEditor.CheckPlace(x, y, z, material)`. Hàm trả cùng `EditResult` như `TryPlace` nhưng không trừ kho, không ghi ô. `TryPlace` gọi lại hàm này, hành vi không đổi.
  - "Hết đồ" = `CheckPlace` trả `NoMaterial`.
  - Hiện bản mờ khi kết quả là `Ok` hoặc `NoMaterial`; các kết quả khác thì ẩn.
- Bản mờ là một khối lập phương 1 × 1 × 1, **không có collider**, để không chặn tia nhắm và không bị `IsTerrainCollider` nhầm. Màu lấy từ `BlockMesher.TopColor`, alpha khoảng 0,5. Đỏ khi hết đồ.
  - Shader/material trong suốt: tra docs URP 17.4 / Unity 6000.4 trước khi viết brief, hoặc dùng lại `Game/SelectionOverlay`.
- Công cụ mặc định khi mở scene: Đào.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] `CheckPlace` trả đúng từng `EditResult` (OutOfBounds, NotPlaceable, NotEmpty, NotAttached, Occupied, NoMaterial, Ok) và không đổi world/kho — kiểm bằng: test EditMode mới trong `WorldEditorTests`.
- [ ] Mọi test `WorldEditorTests` / `SeaFloodTests` cũ đạt, không sửa assert — kiểm bằng: `run_tests` EditMode.
- [ ] Có test PlayMode cho các điểm sau — kiểm bằng: `run_tests` PlayMode.
  - Tab đổi công cụ.
  - Công cụ đào: khung bật, bản mờ tắt.
  - Công cụ đặt: bản mờ ở `placeCell`, khung tắt.
  - Bản mờ ẩn khi ô `NotAttached` / `Occupied`, đỏ khi `NoMaterial`.
  - Bản mờ không có collider.
- [ ] Test PlayMode cũ (`TerrainEditPlayTests`, `EditOcclusionPlayTests`, `SimViewPlayTests`…) đạt. Test nào giả lập chuột phải để đặt chỉ được đổi sang "Tab + chuột trái"; ghi rõ trong Kết quả của brief.
- [ ] HUD dòng 1 ghi đúng phím mới: Tab, chuột trái, 1–4 / lăn chuột.
- [ ] User xem trong `SimView.unity` và `TerrainView.unity`:
  - Tab đổi công cụ.
  - Bản mờ đúng ô, đúng màu, đỏ khi hết đồ.
  - Đặt/đào bằng chuột trái đúng ô.
  - Không có bản mờ ở ô sai luật.

## Ràng buộc
- `Game.Core` không tham chiếu UnityEngine. Luật đặt chỉ nằm trong `WorldEditor`; client không tự kiểm lại luật.
- Không đổi luật đặt/đào, không đổi `Pick`. Giữ chặn chuột khi bản đồ hoặc vòng thời điểm mở (brief 0016, 0024), giữ "đang bay không đào/đặt".
- Dọn sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.

## File liên quan
- `client/Assets/Game/Client/TerrainEditInput.cs`: `Update`, `Pick`, `OnGUI`, `CreateOutline`.
- `client/Assets/Game/Core/WorldEditor.cs`: `TryPlace` ở `:86-107`.
- Scene dùng `TerrainEditInput`: `TerrainView.unity`, `SimView.unity`, `Bench.unity`.
- Test: `Tests/Core/WorldEditorTests.cs`, `Tests/Client/TerrainEditPlayTests.cs`, `Tests/Client/EditOcclusionPlayTests.cs`.

## Câu hỏi còn mở
- Không có.
