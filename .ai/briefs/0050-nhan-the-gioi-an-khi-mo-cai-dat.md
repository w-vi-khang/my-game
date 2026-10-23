# Brief 0050 — ẩn nhãn thế giới khi bảng Cài đặt mở

> Trạng thái: chờ làm
> Review: nhẹ + user xem
> Model thực thi: Sonnet · low
> Cập nhật: 2026-10-23 10:05

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Sửa lỗi user thấy khi xem brief 0046: nhãn trên đầu sinh vật vẽ đè lên bảng Cài đặt. Khi bảng Cài đặt mở, `SimView` không vẽ nhãn thế giới (nhãn sinh vật + dấu `!` đỏ). Roadmap: hạng mục [0032](../../design/decisions/0032-cai-dat-nguoi-choi-va-bang-chuoi.md), ước lượng 0,25 ngày-người.

## Bối cảnh cần đọc
- Nguyên nhân [repo]: `SettingsView` và `SimView` là hai script IMGUI trên cùng GameObject (`client/Assets/Game/Client/SimView.cs:79` thêm `SettingsView`). `SimView.OnGUI` (`SimView.cs:651`) vẽ nhãn ở cuối hàm, không xét bảng Cài đặt. Nguyên văn `SimView.cs:676-685`:
  ```csharp
              var cam = Camera.main;
              if (cam == null)
                  return;
              var cs = Sim.State.Creatures;
              for (int i = 0; i < cs.Count && i < bodies.Count; i++)
                  DrawLabel(cam, bodies[i].position + Vector3.up * (cs[i].SizeY * 0.4f + 0.35f), CreatureStatus.Text(GameSettings.Text, Sim.State, cs[i]), scale);
              foreach (var e in markers.Active)
                  DrawLabel(cam, transform.TransformPoint(new Vector3(e.Cell.X + 0.5f, e.Cell.Y + 1.3f, e.Cell.Z + 0.5f)),
                      "<color=red><b>! " + CreatureStatus.ReasonText(GameSettings.Text, e.Reason) + "</b></color>", scale);
          }
  ```
- `SettingsView` (`client/Assets/Game/Client/SettingsView.cs`): `static SettingsView active;` (`:18`), `public bool IsOpen { get; private set; }` (`:25`), `active` gán trong `Awake`, xoá trong `OnDestroy`.
- Mẫu có sẵn để làm theo: `client/Assets/Game/Client/MinimapView.cs:21` và `TimeDial.cs:17`:
  ```csharp
          public static bool IsAnyOpen => active != null && active.IsOpen;
  ```
- Test có sẵn: lớp `SettingsScenePlayTests` trong `client/Assets/Game/Tests/Client/SettingsPlayTests.cs:150` (SetUp dựng `SimView` + `SettingsView`, trường `sv`, `settings`). Test `Keys_OpenClose_AndSave` (`:221`) mở/đóng bảng bằng `settings.HandleKeys(oPressed, escPressed)`.
- Không dùng API Unity mới.

## Quyết định đã chốt (không được đổi)
1. Cách sửa: **ẩn** nhãn thế giới khi bảng Cài đặt mở, không đổi thứ tự vẽ (`GUI.depth`). Lý do: hộp `GUI.Box` mặc định trong suốt một phần, nên dù vẽ bảng lên trên thì chữ bên dưới vẫn lộ ra. Cách này cũng giống cách code đang làm với minimap (`!MinimapView.IsAnyOpen`).
2. Chỉ ẩn nhãn thế giới (nhãn sinh vật + dấu `!`). HUD trái, dòng trạng thái và `DrawFarmGui` giữ nguyên.
3. Comment mới viết bằng tiếng Anh. Test quét `HardcodedTextScanTests` chặn chữ tiếng Việt trong code.

## Phạm vi
- Được sửa: `client/Assets/Game/Client/SettingsView.cs`, `client/Assets/Game/Client/SimView.cs`, `client/Assets/Game/Tests/Client/SettingsPlayTests.cs`, file brief này.
- Không được sửa: mọi file khác (gồm `design/`, roadmap: Planner ghi khi review).

## Các bước
1. `SettingsView.cs`: ngay dưới dòng `static SettingsView active;` (`:18`), thêm:
   ```csharp
   public static bool IsAnyOpen => active != null && active.IsOpen;
   ```
   Xong khi: build không lỗi.
2. `SimView.cs`: thêm thuộc tính tĩnh ngay trên `void OnGUI()` (`:651`):
   ```csharp
   /// <summary>World-space labels (creature status, red "!" markers) are hidden while the Settings panel is open so they do not draw over it.</summary>
   public static bool WorldLabelsVisible => !SettingsView.IsAnyOpen;
   ```
   Trong `OnGUI`, đổi
   ```csharp
               if (cam == null)
                   return;
   ```
   thành
   ```csharp
               if (cam == null || !WorldLabelsVisible)
                   return;
   ```
   Xong khi: build không lỗi, không đổi dòng nào khác trong `OnGUI`.
3. `SettingsPlayTests.cs`, lớp `SettingsScenePlayTests`: thêm test ngay sau `Keys_OpenClose_AndSave`:
   ```csharp
   [UnityTest]
   public IEnumerator WorldLabels_HiddenWhileSettingsOpen()
   {
       Assert.IsFalse(SettingsView.IsAnyOpen);
       Assert.IsTrue(SimView.WorldLabelsVisible);
       settings.HandleKeys(true, false);
       Assert.IsTrue(SettingsView.IsAnyOpen);
       Assert.IsFalse(SimView.WorldLabelsVisible, "World labels must be hidden while Settings is open");
       settings.HandleKeys(true, false);
       Assert.IsFalse(SettingsView.IsAnyOpen);
       Assert.IsTrue(SimView.WorldLabelsVisible);
       yield return null;
   }
   ```
   Xong khi: test qua.

## Tiêu chí xong
- `SettingsScenePlayTests` qua hết, gồm test mới `WorldLabels_HiddenWhileSettingsOpen`.
- Toàn bộ EditMode + PlayMode qua, số test = trước khi làm + 1 PlayMode (trước: EditMode 498, PlayMode 167 theo brief 0049).
- `git status` sạch sau bước dọn.

## Test sau khi xong

### Phần cần test
- Bước 1–2 (`SettingsView.IsAnyOpen`, `SimView.WorldLabelsVisible`, nhánh return trong `OnGUI`): test `WorldLabels_HiddenWhileSettingsOpen` → qua.
- Bảng Cài đặt mở/đóng, lưu file (có thể bị ảnh hưởng do sửa `SettingsView`): `SettingsScenePlayTests` (`Apply_ReachesScene`, `Keys_OpenClose_AndSave`, `LanguageSwitch_UpdatesUgui`…) → qua.
- Không còn chữ tiếng Việt trong code mới: `HardcodedTextScanTests` (EditMode) → qua.
- Phần còn lại của client (regression): toàn bộ EditMode + PlayMode → qua, số test như Tiêu chí xong.

### Lệnh executor chạy
Unity Editor mở project `client/` qua MCP (`run_tests`), quyết định 0010.
1. Ghi giờ bắt đầu: `date "+%H:%M"`.
2. `read_console` sau khi sửa code → không có lỗi biên dịch.
3. `run_tests` PlayMode, lọc lớp `SettingsScenePlayTests` → qua hết.
4. `run_tests` EditMode (toàn bộ) → 498/498.
5. `run_tests` PlayMode (toàn bộ) → 168/168.
6. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`, rồi `git status --short` → chỉ còn 3 file trong Phạm vi + brief.
7. Ghi giờ kết thúc: `date "+%H:%M"`.

### User tự test
Mở scene `client/Assets/Game/Client/Scenes/SimView.unity`, bấm Play:
1. Đi tới gần các sinh vật để thấy nhãn trên đầu. Bấm `O` (hoặc nút Cài đặt góc dưới phải) để mở bảng Cài đặt. Cần thấy: **không còn nhãn sinh vật hay dấu `!` đỏ nào** trên hay quanh bảng.
2. Bấm `O` hoặc `Esc` để đóng bảng. Cần thấy: nhãn sinh vật hiện lại ngay.
3. HUD trái và dòng trạng thái vẫn hiện cả khi bảng mở (không đổi).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd dòng trích ở Bối cảnh không còn khớp).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

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
