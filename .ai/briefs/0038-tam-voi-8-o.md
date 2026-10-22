# Brief 0038 — Tầm với mọi thao tác 8 ô (0031)

> Trạng thái: chờ làm
> Review: nhẹ + user xem
> Model thực thi: Sonnet · low
> Cập nhật: 2026-10-22 16:20

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Theo [0031](../../design/decisions/0031-tam-voi-moi-thao-tac-8-o.md) (Chốt, user 2026-10-22): mọi thao tác của nhân vật (đào, đặt, xới, nhặt bằng F/click) trong **8 ô**. Hiện tại: tầm công cụ 10 ô, tầm việc tay 4 ô (brief 0036, 0037). Việc cơ học: đổi 2 hằng số, chữ HUD/comment, test biên.

Ước lượng ≤ 0,1 ngày-người. Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `0b3a415`.

- `Core/PlayerRules.cs`: summary lớp `:6` ("tầm công cụ 10 ô / việc tay 4 ô (0030)"); `:11` summary + `public const float ToolReach = 10f;` `:12`; `:13` summary + `public const float HandReach = 4f;` `:14`. Cách đo (`Within`, Euclid từ tâm thân = chân + 1 tới tâm ô, `≤ reach` sai số `1e-4f`) giữ nguyên.
- `Client/TerrainEditInput.cs`: chữ/comment về tầm ở `:16`, `:17`, `:105`, `:155`, `:182`, `:222`, `:234`, `:241`, `:293`, `:294` (mọi chỗ có `4 ô`, `10 ô`, `(0030)` trong file này đều nói về tầm với).
- Test dựa vào tầm cũ:
  - `Tests/Core/PlayerRulesTests.cs:8-36` (4 test biên 10 / 10,1 và 4 / 4,1);
  - `Tests/Core/DropFieldTests.cs:137-154` `CollectInReachTakesOnlyWithin4`;
  - `Tests/Core/DropPickTests.cs:38-39` `OutOfHandReachIgnored` (chân (5.5, 1, 10.5) cách chồng ≈ 5,02 → với 8 ô thành trong tầm);
  - `Tests/Client/PlayerCharacterPlayTests.cs:253-277` `ReachLimitsTools` (ô +10 ngoài, +9 trong, đặt +11 ngoài);
  - `Tests/Client/DropPlayTests.cs:110-145` `CollectNearbyTakesOnlyWithin4` và `ClickPicksOnlyInReachAndFarDoesNotBlockDig` (chồng xa ở +6 → với 8 ô thành trong tầm), và `DigDropsVisibleThenCollectAndPlace` (ngay sau).
- Không dùng API Unity mới.

## Quyết định đã chốt (không được đổi)
1. `ToolReach = 8f`, `HandReach = 8f`. **Giữ hai hằng và hai hàm** `InToolReach` / `InHandReach` (0031: để sau tách lại nếu cần). Không đổi cách đo.
2. Chữ người chơi đọc: mọi "4 ô" và "10 ô" về tầm trong `TerrainEditInput.cs` → "8 ô"; "(0030)" → "(0031)". Không đổi chữ khác.
3. Tính biên (tâm thân = chân + 1): ô (A.x + k, G) với chân (A.x + 0.5, G + 1, A.z + 0.5) cách `√(k² + 2,25)` → k = 7 trong (≈ 7,16), k = 8 ngoài (≈ 8,14); ô (A.x + 9, G + 1) cách ≈ 9,01 → ngoài.
4. Test PlayMode có `CollectNearby` đếm chính xác: ngay sau `Teleport`, gọi `input.CollectNearby();` một lần rồi `yield return Run(Vector2.zero, false, 0.3f);` để dọn túi hạt đầu game nếu nó nằm trong 8 ô của A (tầm 8 ô rộng hơn trước), rồi mới ghi số túi ban đầu và thêm chồng.

## Phạm vi
- Được sửa: `Core/PlayerRules.cs`, `Client/TerrainEditInput.cs` (chỉ chữ/comment ở QĐ 2), `Tests/Core/PlayerRulesTests.cs`, `Tests/Core/DropFieldTests.cs`, `Tests/Core/DropPickTests.cs`, `Tests/Client/PlayerCharacterPlayTests.cs`, `Tests/Client/DropPlayTests.cs` — chỉ các chỗ ở "Các bước"; mục Kết quả của brief này.
- Không được sửa: file khác (gồm `Core/StarterBag.cs`, `Core/DropPick.cs`, `Core/DropField.cs`, scene, `design/`).

## Các bước
1. **`Core/PlayerRules.cs`:**
   - `:12` → `public const float ToolReach = 8f;`; summary `:11` → `/// <summary>Tầm công cụ: đào, đặt, xới, click ô ruộng (0031: 8 ô).</summary>`
   - `:14` → `public const float HandReach = 8f;`; summary `:13` → `/// <summary>Tầm việc tay: nhặt (sau này gieo, thu hoạch, phép Mưa, mở kho — 0031: 8 ô).</summary>`
   - `:6`: "tầm công cụ 10 ô / việc tay 4 ô (0030)" → "tầm với mọi thao tác 8 ô (0031)".
   - Xong khi: biên dịch được.
2. **`Client/TerrainEditInput.cs`:** thay trong cả file: `4 ô` → `8 ô`, `10 ô` → `8 ô`, `(0030)` → `(0031)`.
   - Xong khi: `grep -n "4 ô\|10 ô\|0030" client/Assets/Game/Client/TerrainEditInput.cs` không còn kết quả.
3. **`Tests/Core/PlayerRulesTests.cs:8-36`** thay 4 test bằng (cùng chân như cũ):
   - `InToolReach_BoundaryExactly8`: chân (0.5, −0.5, 0.5): ô (8,0,0) true; (9,0,0) false; (0,8,0) true; (0,0,−8) true.
   - `InToolReach_8Point1Outside`: chân (0.4, −0.5, 0.5), ô (8,0,0) → false.
   - `InHandReach_BoundaryExactly8`, `InHandReach_8Point1Outside`: như hai test trên, dùng `InHandReach`.
4. **`Tests/Core/DropFieldTests.cs:137-154`:** đổi tên thành `CollectInReachTakesOnlyWithin8`; `Drops.Add(4, 1, 0, …Dirt, 2)` → `(8, 1, 0, …)`, `Drops.Add(5, 1, 0, …Stone, 1)` → `(9, 1, 0, …)`; dòng `Drops.Add(4, 1, 0, B(BlockMaterial.Dirt), 1);` (`:152`) → `(8, 1, 0, …)`. Các assert giữ nguyên (chân (0.5, 0.5, 0.5): (8,1,0) cách đúng 8 → trong; (9,1,0) cách 9 → ngoài; chân (0.4, …) cách 8,1 → ngoài).
5. **`Tests/Core/DropPickTests.cs:39`:** chân `(5.5f, 1f, 10.5f)` → `(5.5f, 1f, 14.5f)` (cách ≈ 9,01 → ngoài).
6. **`Tests/Client/PlayerCharacterPlayTests.cs` `ReachLimitsTools`:** `far` = `C(A.x + 8, G, A.z)`; `near` = `C(A.x + 7, G, A.z)`; `pc` = `C(A.x + 9, G + 1, A.z)` (QĐ 3).
7. **`Tests/Client/DropPlayTests.cs`:**
   - `CollectNearbyTakesOnlyWithin4` → đổi tên `CollectNearbyTakesOnlyWithin8`.
   - Trong test đó và `ClickPicksOnlyInReachAndFarDoesNotBlockDig`: chồng `far` ở `A.x + 6` → `A.x + 9`; `rayFar` x `A.x + 6.5f` → `A.x + 9.5f`; assert `dig == C(A.x + 6, G, A.z)` → `C(A.x + 9, G, A.z)`.
   - Trong 3 test `CollectNearbyTakesOnlyWithin8`, `ClickPicksOnlyInReachAndFarDoesNotBlockDig`, `DigDropsVisibleThenCollectAndPlace`: chèn bước dọn QĐ 4 ngay sau dòng `yield return Teleport(...)`.
8. Test khác fail vì tầm 8 ô → dừng, `cần Opus` (không tự sửa).

## Tiêu chí xong
- `grep -n "ToolReach = \|HandReach = " client/Assets/Game/Core/PlayerRules.cs` → hai dòng `8f`.
- `grep -n "4 ô\|10 ô\|0030" client/Assets/Game/Client/TerrainEditInput.cs` → rỗng.
- EditMode + PlayMode toàn bộ qua.
- Sau PlayMode, `git status` không có `client/ProjectSettings/EditorSettings.asset`.

## Test sau khi xong

### Phần cần test
- **Biên tầm 8 ô (Core)** · `PlayerRulesTests.InToolReach_*`, `InHandReach_*` · 8 trong, 8,1 ngoài, cả trục y và z.
- **Thu trong 8 ô (Core)** · `DropFieldTests.CollectInReachTakesOnlyWithin8` · nhặt chồng cách 8, bỏ chồng cách 9.
- **Nhặt một chồng, ngoài tầm** (phần cũ) · `DropFieldTests.TryCollectSingleStack` · chồng (9,1,0) vẫn ngoài.
- **Version, tất định** (phần cũ, dùng `CollectInReach`) · `DropFieldTests.VersionIncrementsOnChanges`, `DeterministicSameSequence` · qua.
- **Click chọn ngoài tầm (Core)** · `DropPickTests.OutOfHandReachIgnored` + 7 test còn lại · qua.
- **Túi hạt đầu game** (dùng `InHandReach`) · `StarterBagTests` 1–7 · qua (kết quả không đổi).
- **Client đào/đặt 8 ô** · `PlayerCharacterPlayTests.ReachLimitsTools` · +8 ngoài, +7 trong, đặt +9 ngoài.
- **Client F / click 8 ô** · `DropPlayTests.CollectNearbyTakesOnlyWithin8`, `ClickPicksOnlyInReachAndFarDoesNotBlockDig` · chồng +2 nhặt được, chồng +9 không, không chặn đào.
- **Còn lại** · `run_tests` EditMode + PlayMode toàn bộ · qua.

### Lệnh executor chạy
1. Unity Editor đang mở project qua MCP: `run_tests` mode `EditMode` (toàn bộ) → dòng tổng kết.
2. `run_tests` mode `PlayMode` (toàn bộ) → dòng tổng kết.
3. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. Hai grep ở "Tiêu chí xong" → ghi kết quả.
5. Editor tắt (không có MCP): thay 1–2 bằng `python3 tools/unity_test.py EditMode` và `python3 tools/unity_test.py PlayMode`.

### User tự test
Trên Mac, scene `TerrainView` hoặc `SimView`:
1. Đào/đặt: khung trắng / bản mờ hiện tới khoảng 8 ô quanh nhân vật, xa hơn thì không; click ngoài 8 ô không làm gì.
2. F nhặt mọi vật rơi trong khoảng 8 ô; click nhặt vật rơi trong 8 ô, vật xa hơn không chặn đào.
3. Chữ trợ giúp góc trái ghi "trong 8 ô".
4. Cảm giác: 8 ô có vừa tay không.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch làm không chắc sửa chỗ nào).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Test không có trong "Các bước" fail.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · giờ bắt đầu/kết thúc · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả test
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
