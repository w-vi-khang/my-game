# Brief 0068 — Ổn định 3 test chỉ hỏng khi chạy cả bộ

> Trạng thái: chờ review
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24 10:17

<!-- Không có task contract riêng: việc sửa test, không đổi hành vi game. Nguồn: .ai/handoff.md mục "Việc treo — test chỉ hỏng khi chạy cả bộ". -->

## Mục tiêu
Ba test chạy riêng thì qua nhưng hỏng khi chạy cả bộ, nhất là khi Unity Editor chạy nền, không focus. Brief này làm cho chúng qua ổn định trong lần chạy cả bộ EditMode/PlayMode qua MCP. Chỉ sửa **test**, không đổi code game (trừ trường hợp ở bước 2.4).

## Bối cảnh cần đọc
- Ghi nhận lỗi trước đây:
  - `.ai/briefs/0061-7l-d1-tui-o-vuong-loi.md:305`, `0063…:215` và `0066…:266`: `IncrementalRebuildMatchesFullRebuild` quá timeout 180000 ms (194–205 s khi Editor không focus, chạy riêng ~40 s).
  - `0064…:310`, `0065…:267` và `0067…:226`: `DayCycleOffNearSeaUnchanged` lệch 0,021–0,079 > 0,002 khi chạy cả bộ.
  - `0066…:267` và `0066…:301`: `CreatureIconMatchesState` hỏng một lần khi chạy cả bộ.
- **Test 1** `client/Assets/Game/Tests/World/IncrementalRebuildTests.cs:25-67`. Test sửa 200 ô; cứ 10 lần sửa thì gọi `BlockMesher.BuildAll(world)` cho cả world và so với mesh dựng tăng dần, tức 20 lần dựng lại toàn bộ. Hai assert cuối: `Assert.AreEqual(200, edits, …)` và `Assert.AreEqual(20, checks)`.
- **Test 2** `client/Assets/Game/Tests/Client/SeaSkyTintPlayTests.cs:208-219` (`DayCycleOffNearSeaUnchanged`):
  - Test so màu biển ở 40 m với mốc cố định `BeforeOffNear40` (dòng 22), cho phép lệch tối đa 0,002.
  - SetUp ở dòng 34-86 và TearDown ở dòng 88-106 chỉ lưu/khôi phục skybox và fog.
  - `DayCycleView.Apply()` đặt thêm `RenderSettings.ambientMode/ambient*Color` và nhiều shader global (`client/Assets/Game/Client/DayCycleView.cs:62`, `:106-126`).
  - `WorldEdgeFog` đặt global `_EdgeFogBlend` (`client/Assets/Game/Client/WorldEdgeFog.cs:47`, `:55`).
  - Shader biển: `client/Assets/Game/Client/Shaders/Ocean.shader`.
  - Nguyên nhân lệch **chưa xác minh**. Nghi: trạng thái toàn cục do test chạy trước để lại, như Light hoặc Camera chưa bị huỷ, ambient intensity / reflection, hoặc một shader global mà Ocean.shader đọc nhưng SetUp không đặt.
- **Test 3** `client/Assets/Game/Tests/Client/WorldIconsPlayTests.cs:101-116` (`CreatureIconMatchesState`):
  - Test gọi 500 lần `sv.Sim.Tick()`, `yield return null`, rồi so icon với `State`.
  - `SimView.Update` tự chạy nhịp theo `Time.deltaTime` (`client/Assets/Game/Client/SimView.cs:130-140`: `int n = clock.Advance(Time.deltaTime);`, tối đa `maxTicksPerFrame = 5` nhịp mỗi khung).
  - `WorldIconsView` cập nhật icon trong `LateUpdate` (`client/Assets/Game/Client/WorldIconsView.cs:164`).
  - Coroutine của test chạy tiếp **sau Update, trước LateUpdate**. Vì vậy ở khung test đọc, `SimView.Update` có thể đã chạy thêm nhịp, trong khi icon vẫn là của khung trước.
  - Khung dài, như khi Editor không focus (memory: Editor không focus làm khung Play chậm), thì `Time.deltaTime` lớn và chắc chắn có nhịp chen vào.
  - `PlotIconMatchesStageAndProgress` (dòng 85-99) cũng theo cùng mẫu.

## Quyết định đã chốt (không được đổi)
1. Không nới ngưỡng so sánh: 0,002 của test 2, so bằng tuyệt đối icon của test 3, so mesh bằng tuyệt đối của test 1. Lý do: ngưỡng đang bắt đúng lỗi thật (brief 0034 đã kiểm test bắt được lỗi).
2. Test 1:
   - Giữ 200 lần sửa và phân bố toạ độ như cũ.
   - Đổi tần suất so toàn bộ từ mỗi 10 lần sửa thành **mỗi 40 lần sửa**, tức 5 lần so.
   - Đổi assert thành `Assert.AreEqual(5, checks)`.
   - Lý do: lỗi dựng tăng dần còn nằm lại trong `meshes` cho tới lần so kế tiếp, nên lần so cuối (sau 200 lần sửa) vẫn bắt được. Thời gian giảm khoảng 4 lần.
   - **Không** thêm `[Timeout]`.
3. Test 3: trong hai test `CreatureIconMatchesState` và `PlotIconMatchesStageAndProgress`, cho `SimView` **không tự chạy nhịp** bằng `Time.timeScale = 0f` ở đầu test. Lưu giá trị cũ vào field, khôi phục trong `TearDown`. Không thêm API mới vào `SimView` cho việc này.
   - Lý do: `clock.Advance(Time.deltaTime)` nhận 0 thì trả 0 nhịp. Icon cập nhật trong `LateUpdate` không phụ thuộc thời gian.
   - Nếu đo thấy `Time.deltaTime` vẫn ≠ 0 khi `timeScale = 0`, hoặc icon ngừng cập nhật → dừng, `cần Opus`.
4. Test 2: tìm đúng trạng thái toàn cục gây lệch (bước 2), rồi **đặt tường minh trạng thái đó trong SetUp và khôi phục trong TearDown** của `SeaSkyTintPlayTests`. Nếu nguyên nhân là một test khác bỏ sót GameObject hoặc trạng thái toàn cục → sửa thêm TearDown của test đó cho dọn sạch (được phép, xem Phạm vi). Không sửa shader, không sửa `DayCycleView`.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Tests/World/IncrementalRebuildTests.cs`
  - `client/Assets/Game/Tests/Client/WorldIconsPlayTests.cs`
  - `client/Assets/Game/Tests/Client/SeaSkyTintPlayTests.cs`
  - SetUp/TearDown của test PlayMode khác trong `client/Assets/Game/Tests/` **chỉ khi** bước 2 chỉ ra test đó để lại trạng thái. Ghi tên vào Kết quả.
  - Brief này (mục Kết quả trở xuống).
- Không được sửa: mọi file ngoài `client/Assets/Game/Tests/` (code game, shader, scene, `design/`). Working tree có thay đổi chưa commit của phiên khác (`design/…`, `tools/terrain_catalog*`, `.claude/skills/local-terrain-gen/`): không sửa, không stage, không khôi phục.

## Các bước
1. **Test 1** · `IncrementalRebuildTests.cs`:
   - `if (edits % 10 == 0)` → `if (edits % 40 == 0)`.
   - `Assert.AreEqual(20, checks);` → `Assert.AreEqual(5, checks);`.
   - Sửa chú thích nếu có nhắc "mỗi 10".
   - Xong khi: biên dịch được, test chạy riêng qua. Ghi thời gian chạy (giây) từ kết quả `run_tests`.
2. **Test 2** · tìm nguyên nhân rồi sửa:
   1. Thêm tạm vào đầu `DayCycleOffNearSeaUnchanged` một `Debug.Log("[0068diag] …")` in:
      - `RenderSettings.ambientMode`, `ambientLight`, `ambientSkyColor`, `ambientEquatorColor`, `ambientGroundColor`, `ambientIntensity`, `reflectionIntensity`, `defaultReflectionMode`, `RenderSettings.sun` (tên hoặc null);
      - số `Light` và số `Camera` đang hoạt động (`Object.FindObjectsByType<Light>(FindObjectsSortMode.None)`, tương tự cho `Camera`), kèm tên của chúng;
      - giá trị **mọi** shader global mà `Ocean.shader` khai báo nhưng không nằm trong Properties block (đọc shader để lấy danh sách; dùng `Shader.GetGlobalFloat/GetGlobalColor/GetGlobalVector`).
   2. Chạy **riêng** `SeaSkyTintPlayTests` rồi chạy **cả bộ PlayMode**. Đọc dòng `[0068diag]` của cả hai lần qua `read_console`, so khác nhau.
   3. Đặt tường minh trong SetUp (lưu cũ, khôi phục trong TearDown) mọi giá trị khác nhau giữa hai lần. Nếu khác là có Light/Camera thừa → tìm test để lại chúng (tên GameObject), sửa TearDown của test đó cho huỷ, và trong SetUp của `SeaSkyTintPlayTests` không dựa vào chuyện scene sạch.
   4. Không tìm được khác biệt nào giải thích lệch, hoặc phải sửa ngoài `client/Assets/Game/Tests/` → dừng, `cần Opus`. Ghi hai bộ số `[0068diag]` vào Vướng mắc.
   5. Xoá dòng log tạm `[0068diag]` sau khi sửa xong.
   - Xong khi: chạy cả bộ PlayMode thì `DayCycleOffNearSeaUnchanged` qua. Kết quả ghi rõ nguyên nhân tìm được (giá trị nào khác, test nào để lại).
3. **Test 3** · `WorldIconsPlayTests.cs`:
   - Thêm field `float savedTimeScale;`.
   - Trong `SetUp` lưu `savedTimeScale = Time.timeScale;` (trước `yield` đầu tiên). Trong `TearDown` đặt `Time.timeScale = savedTimeScale;` (trước `Object.Destroy`).
   - Dòng đầu của `CreatureIconMatchesState` và `PlotIconMatchesStageAndProgress`: `Time.timeScale = 0f; // SimView không tự chạy nhịp giữa lúc icon cập nhật (LateUpdate) và lúc test đọc State — brief 0068`.
   - Xong khi: `WorldIconsPlayTests` chạy riêng 5/5 qua.

## Tiêu chí xong
- Ba test đã sửa đều qua trong **hai lần chạy cả bộ liên tiếp** của EditMode và của PlayMode qua MCP `run_tests`, khi Editor ở trạng thái như lúc chạy (không cần focus).
- Không còn test hỏng khác trong các lần chạy đó. Nếu có test hỏng khác → ghi tên và lỗi, chạy riêng một lần, rồi ghi kết quả. Không sửa test ngoài phạm vi.
- Không còn dòng `[0068diag]` trong code.
- `git diff --stat` chỉ có file trong `client/Assets/Game/Tests/` (và brief này).

## Test sau khi xong

### Phần cần test
- Test 1 (đổi tần suất so): `IncrementalRebuildTests` (cả lớp) chạy riêng → qua. Ghi thời gian `IncrementalRebuildMatchesFullRebuild`, mong khoảng ≤ 15 s khi focus, trước đây ~40 s. Sau đó chạy trong cả bộ EditMode ×2 → qua, không timeout.
- Test 1 vẫn bắt được lỗi: kiểm bằng suy luận (Quyết định 2). Không cần cố tình làm hỏng mesher.
- Test 2 (trạng thái toàn cục): `SeaSkyTintPlayTests` chạy riêng → 6/6 qua. Cả bộ PlayMode ×2 → `DayCycleOffNearSeaUnchanged` qua.
- Test PlayMode khác có TearDown bị sửa ở bước 2.3 (nếu có): chạy riêng lớp đó → qua.
- Test 3 (`timeScale`): `WorldIconsPlayTests` chạy riêng → 5/5 qua. Cả bộ PlayMode ×2 → `CreatureIconMatchesState` và `PlotIconMatchesStageAndProgress` qua.
- Test 3 không làm hỏng test sau nó: các test chạy sau `WorldIconsPlayTests` trong cả bộ không hỏng vì `timeScale = 0` (TearDown đã khôi phục), được kiểm qua lần chạy cả bộ.
- Regression chung: EditMode cả bộ và PlayMode cả bộ, mỗi bộ ×2, tổng qua = tổng số test. Console 0 Error sau biên dịch.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode, lọc `Game.World.Tests.IncrementalRebuildTests` → ghi thời gian.
3. MCP `run_tests` PlayMode, lọc `Game.Client.Tests.WorldIconsPlayTests` rồi `Game.Client.Tests.SeaSkyTintPlayTests`.
4. Bước 2.2 (chẩn đoán) theo Các bước.
5. MCP `run_tests` EditMode toàn bộ ×2, PlayMode toàn bộ ×2. Mỗi lần ghi dòng tổng (tổng / qua / hỏng + tên test hỏng).
6. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` (PlayMode làm bẩn file này). `git status --short` chỉ còn file của brief này và thay đổi chưa commit của phiên khác. Xoá `client/Assets/InitTestScene*.unity(.meta)` nếu xuất hiện và không còn test nào đang chạy.

Ghi giờ bắt đầu/kết thúc vào Kết quả.

### User tự test
Không.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd nới ngưỡng, sửa code game/shader).
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 6084ba3
Chạy bằng: claude-sonnet-5-5 · medium
Giờ: bắt đầu 09:52, kết thúc 10:17 (~25 phút; phần lớn là chờ chạy cả bộ).
Commit: 4478ba7. File đổi: `IncrementalRebuildTests.cs`, `WorldIconsPlayTests.cs`, `SeaSkyTintPlayTests.cs`, `ControlModePlayTests.cs` (thêm — bước 2.3).
- Test 1: so toàn bộ mỗi 40 lần sửa, assert 5 lần so.
- Test 3: `Time.timeScale = 0f` đầu hai test, lưu/khôi phục trong SetUp/TearDown.
- Test 2, nguyên nhân (`[0068diag]`): chạy riêng có 1 `Light`; chạy cả bộ có **2 `Light`** Directional (ambient, probe, fog, global đều giống hệt). Light thừa do `ControlModePlayTests.SpellMenuRainEntersRainMode` tạo cục bộ (`lightGo`) và không bao giờ huỷ → thành đèn chính thứ hai, màu biển lệch 0,0173. Sửa: ControlModePlayTests giữ `extraLightGo` và huỷ trong TearDown; `SeaSkyTintPlayTests.SetUp` tắt mọi Light đang có trước khi tạo Light của mình, TearDown bật lại (không dựa vào scene sạch). Đã xoá log `[0068diag]`.
- Đã dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; không có InitTestScene*.

## Kết quả test
- Test 1: đạt — `IncrementalRebuildTests` 3/3, `IncrementalRebuildMatchesFullRebuild` 9,6 s (Editor không focus; trước ~40 s riêng, 194–205 s cả bộ); EditMode cả bộ ×2: 582/582, 582/582 (310 s, 304 s), không timeout.
- Test 2: đạt — `DayCycleOffNearSeaUnchanged` qua trong cả bộ PlayMode ×2. Chạy riêng `SeaSkyTintPlayTests` 6/6 (trước khi sửa; sau sửa được phủ bởi cả bộ). Trước sửa: cả bộ 266/267, test này lệch 0,0173.
- Test 3: đạt — `CreatureIconMatchesState`, `PlotIconMatchesStageAndProgress` qua trong cả bộ PlayMode ×2; test chạy sau không hỏng. Chưa chạy riêng `WorldIconsPlayTests` 5/5 sau sửa (chỉ trong cả bộ).
- ControlModePlayTests: đạt trong cả bộ (không chạy riêng).
- Regression: PlayMode cả bộ ×2: 267/267, 267/267 (170 s, 171 s); console 0 Error.

## Vướng mắc
Không. Lưu ý: cầu MCP mất kết nối khi vào Play, `get_test_job` vẫn lấy được kết quả sau khi chờ.

---
<!-- PLANNER ghi khi review. -->

## Review
