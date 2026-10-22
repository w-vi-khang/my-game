# Brief 0043 — Sim 7f: HUD trái không đè chữ trạng thái trại; bỏ dòng "đang đi tới" khi đã xới

> Trạng thái: chờ làm
> Review: nhẹ
> Model thực thi: Sonnet · low
> Cập nhật: 2026-10-22 19:15

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Planner chạy thử scene SimView qua MCP sau brief 0041–0042 (2026-10-22, ảnh chụp Game view 1280 × 656) thấy hai lỗi nhỏ:
1. HUD góc trái của `TerrainEditInput` dài thêm (dòng Mưa, trạng thái, nông sản — brief 0041/0042) nên **đè lên chữ trạng thái của `SimView`** (dòng `F1: 30 FPS · …`, `Kho …`, `Ruộng N · …` vẽ cố định từ y = 100).
2. Đang xới (sau khi tự đi tới nơi), HUD vẫn còn dòng `Tự xới ô …: đang đi tới` bên dưới dòng `Đang xới ô …: n/6 giây`.

Sửa: chữ trạng thái của `SimView` bắt đầu ngay dưới HUD trái; khi tới nơi thì xoá dòng "đang đi tới". Ước lượng ~0,1 ngày-người (thuộc 7f).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `a9ec042`.
- `Client/TerrainEditInput.cs` `OnGUI()` (`:386-395`): `GUI.Label(new Rect(10, 10, 1000, 200), text, new GUIStyle(GUI.skin.label) { richText = true });` — chiều cao cố định 200.
- `Client/SimView.cs` `OnGUI()` (`:649-…`): `GUI.Label(new Rect(10, 100, 1000, 24), $"F1: 30 FPS · …")`, rồi `GUI.Label(new Rect(10, 100 + 24 * line++, …))` cho kho, các ruộng, dòng đo.
- `Client/PlayerFarmActions.cs` `Update()`, nhánh `case AutoWalkState.Arrived:` (đặt `TillTimer.Start(); TillState = SelfTillState.Tilling;`).
- API Unity: `GUIStyle.CalcHeight(GUIContent content, float width)` — chiều cao khi vẽ `content` với độ rộng `width` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUIStyle.CalcHeight.html). Ngoài ra chỉ dùng API đã có trong file.

## Quyết định đã chốt (không được đổi)
1. `TerrainEditInput`: `public static float HudBottom { get; private set; }` (đơn vị GUI đã chia `scale`, như `Rect` trong `OnGUI`). `OnGUI`: tạo style một lần (trường riêng `GUIStyle hudStyle`, lazy trong `OnGUI`), `float h = hudStyle.CalcHeight(new GUIContent(text), 1000f);`, vẽ `new Rect(10, 10, 1000, h)`, `HudBottom = 10f + h`. Nhánh `return` sớm (không có túi / bản đồ mở) → `HudBottom = 0f`.
2. `SimView`: `public static float StatusTop(float hudBottom) => Mathf.Max(100f, hudBottom + 4f);`. `OnGUI` dùng `float y0 = StatusTop(TerrainEditInput.HudBottom);` thay mọi số `100` ở các `Rect` chữ trạng thái (`new Rect(10, y0, …)`, `new Rect(10, y0 + 24 * line++, …)`). Không đổi gì khác (bảng, nút, nhãn trên đầu sinh vật).
3. Thứ tự `OnGUI` giữa hai component không xác định → có thể trễ 1 khung; chấp nhận.
4. `PlayerFarmActions`, `case AutoWalkState.Arrived:` thêm `StatusLine = "";` (dòng `Đang xới ô …` đã nói đủ).

## Phạm vi
- Được sửa: `Client/TerrainEditInput.cs` (chỉ `OnGUI` + thuộc tính/trường mới), `Client/SimView.cs` (chỉ `StatusTop` + `OnGUI` chữ trạng thái), `Client/PlayerFarmActions.cs` (chỉ dòng ở quyết định 4); `Tests/Client/PlayerBagSowPlayTests.cs` (thêm test); mục Kết quả của brief này.
- Không được sửa: `Core/**`, scene, `design/`, test khác, file khác.

## Các bước
1. `TerrainEditInput.OnGUI` + `HudBottom` theo quyết định 1. Summary cho `HudBottom`: "Đáy HUD trái (đơn vị GUI) ở khung vẽ gần nhất; 0 khi không vẽ — SimView đặt chữ trạng thái dưới mốc này (brief 0043)."
2. `SimView.StatusTop` + `OnGUI` theo quyết định 2.
3. `PlayerFarmActions` theo quyết định 4.
4. Test ở "Phần cần test" (thêm vào `PlayerBagSowPlayTests`, dùng `SetUp` có sẵn).

## Tiêu chí xong
- PlayMode toàn bộ qua (cũ + mới, kỳ vọng 148 + 2), EditMode 472/472, 0 lỗi console.
- `git diff --stat` chỉ có file trong Phạm vi.

## Test sau khi xong
### Phần cần test
- **StatusTop:** `SimView.StatusTop(0f) == 100f`; `StatusTop(250f) == 254f`.
- **Tới nơi xoá dòng đi tới:** như `PlayerFarmPlayTests.SelfTillFull` (Xới, `input.UseTool` lên ô có biển, chờ ≤ 10 giây tới `TillState == Tilling`) → `Farming.StatusLine == ""`, `Farming.HudLines()` chứa `"Đang xới ô"` và không chứa `"đang đi tới"`.
- Cũ: `PlayerFarmPlayTests` (`SelfTillFull` chỉ kiểm `"Xới xong ô"` lúc xong — phải vẫn qua), `TerrainEditPlayTests`, `DropPlayTests`, `SimFarmUiPlayTests`, `SimViewPlayTests` — qua không đổi kỳ vọng.

### Lệnh executor chạy
1. `date` (bắt đầu).
2. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch.
3. `run_tests` PlayMode lọc `PlayerBagSowPlayTests` → qua; `run_tests` PlayMode toàn bộ; `run_tests` EditMode toàn bộ → ghi dòng tổng.
4. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py PlayMode` rồi `python3 tools/unity_test.py EditMode`.
5. `date` (kết thúc).

### User tự test
- (gộp vào lượt user xem 0041/0042) Scene SimView, cầm Xới: dòng `F1: 30 FPS …`, `Kho …`, `Ruộng …` nằm dưới HUD trái, không đè chữ.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc không tìm được đoạn cần sửa.
- Một test cũ đổi kết quả.
- Cần API Unity không có trong brief; phải sửa file ngoài Phạm vi.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit:
Chạy bằng:
Bắt đầu: · Kết thúc: (theo `date`)

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
