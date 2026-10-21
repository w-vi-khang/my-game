# Brief 0022 — World 7f-1: lõi buổi trong ngày (`TimeOfDay`, khung mốc, buổi, điều kiện buổi) + `time_m1.json`

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 15:17

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Viết lõi C# thuần (trong `Game.Core`) cho buổi trong ngày theo [0024](../../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md): tính pha trong ngày từ giờ đồng hồ cộng độ lệch, chọn 1 trong 4 mốc (cung xoay luôn theo chiều tiến, có khung mốc), buổi theo pha, điều kiện "buổi". Kèm `design/data/time_m1.json` và test EditMode. Brief này chưa có phần hiển thị: bầu trời và nạp cấu hình vào client làm ở 7f-2, vòng chọn mốc ở 7f-3.

Task contract: [.ai/tasks/m1-world-buoc-7f-buoi-trong-ngay.md](../tasks/m1-world-buoc-7f-buoi-trong-ngay.md) (brief 1/3).

## Bối cảnh cần đọc
- [0024](../../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md) mục Quyết định 1–7: hai đồng hồ tách rời, 1 ngày = 86.400.000 ms, 4 mốc, chọn mốc thì đặt offset, cung chiều tiến, 4 buổi.
- `design/02-gameplay/mechanics.md` mục "Buổi trong ngày": dòng **Khung mốc** (user chốt 2026-10-21).
- `client/Assets/Game/Core/Sim/SimConfig.cs:151-157`: mẫu hàm parse tên enum, chép theo đúng kiểu này:
  ```csharp
  public static WorkAbility ParseAbility(string s)
  {
      if (!string.IsNullOrEmpty(s) && char.IsLetter(s[0])
          && Enum.TryParse(s, false, out WorkAbility a) && Enum.IsDefined(typeof(WorkAbility), a))
          return a;
      throw new ArgumentException("Năng lực không hợp lệ: " + s);
  }
  ```
- `client/Assets/Game/Core/Sim/SimConfig.cs:112-118`: mẫu lớp cấu hình `[Serializable]`, có giá trị mặc định bằng file JSON, kèm `Validate()` ném `ArgumentException`.
- `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs:11-35`: mẫu test đọc `design/data/*.json` (`Application.dataPath`, `JsonUtility.FromJson`, `ConfigMatchesCodeDefaults` so `JsonUtility.ToJson`).
- `client/Assets/Game/Tests/Core/Sim/SimDeterminismTests.cs:9-43`: `static Simulation BuildFarm(uint seed)` và `RunHashes` (dùng `SimHash.Compute(sim.State)`), đem dùng lại cho test hash.
- `client/Assets/Game/Core/Game.Core.asmdef`: `"noEngineReferences": true`, nên code mới KHÔNG được `using UnityEngine`. Test thì được (asmdef `Game.Core.Tests`, chỉ chạy trên Editor).

## Quyết định đã chốt (không được đổi)
1. **Đơn vị pha là ms nguyên** `int` trong [0; 86.400.000). Hằng `TimeConfig.DayMs = 86_400_000`. Không dùng float/double trong lõi. Lý do: chính xác tuyệt đối, test so bằng `==`.
2. **JSON ghi pha theo phần nghìn ngày** (‰). Đổi sang ms bằng `‰ × 86_400` (`TimeConfig.MsPerPermille = 86_400`), phép đổi này chính xác. Lý do: số trong JSON dễ đọc và khớp tài liệu (0,25 → 250).
3. **Nguồn giờ là `Func<long>`** trả ms giờ đồng hồ máy. Mốc 0 của nguồn phải rơi vào nửa đêm giờ máy (vd ms kể từ nửa đêm). Lõi lấy `mod DayMs` dương nên số âm hay số lớn đều được. 7f-2 mới chọn nguồn thật. Test truyền biến giả.
4. **Đồng hồ máy nhảy giờ** (user đổi giờ, đổi múi giờ): pha đi theo đồng hồ ngay lập tức, `OffsetMs` giữ nguyên, lõi không làm mượt. Nếu cần xử lý cho phần nhìn thấy thì để 7f-2. Lý do: pha là hàm thuần của (giờ, offset), không tích luỹ nên không lệch dần.
5. **Khung mốc** (user 2026-10-21): khung của một mốc là [mốc đó; mốc kế tiếp), khung hoàng hôn quay vòng qua nửa đêm về giữa đêm. Nếu pha hiện tại nằm trong khung của mốc được chọn thì `SelectLandmark` trả 0 và **không đổi gì**. Ngược lại, đặt offset để pha bằng mốc và trả cung `ForwardArc(pha cũ, mốc)`, luôn thuộc (0; 0,75 ngày].
6. `SelectLandmark` **chỉ đọc nguồn giờ một lần**, rồi dùng đúng giá trị đó để tính cả pha cũ lẫn offset mới.
7. **Buổi** lấy từ 4 biên trong JSON: Night [nightStart; dawnStart) có quay vòng, Dawn [dawnStart; dayStart), Day [dayStart; duskStart), Dusk [duskStart; nightStart). Buổi và khung mốc là hai thứ khác nhau.
8. Điều kiện buổi `PeriodCondition` có `string[] periods` (tên enum `DayPeriod`, phân biệt hoa thường). Thoả khi buổi hiện tại có trong danh sách. Không nối vào đâu cả, vì bộ môi trường sống chưa có code.
9. Phần **không** làm trong brief này: animation xoay 3 giây và việc bỏ qua lệnh chọn trong lúc xoay (để 7f-2); `ConfigLoader.LoadTimeConfig`, bản sao trong `Resources/Config` và test so bản sao (để 7f-2, lúc client bắt đầu dùng). Trường `rotateDurationMs` có trong JSON nhưng 7f-1 chỉ kiểm hợp lệ, không dùng tới.
10. Namespace `Game.Core.DayCycle`, thư mục `client/Assets/Game/Core/DayCycle/`. Không đặt tên `Time` để khỏi trùng với `UnityEngine.Time` ở phía client.

## Phạm vi
- Được tạo:
  - `client/Assets/Game/Core/DayCycle/TimeConfig.cs`, `TimeOfDay.cs`, `PeriodCondition.cs` (cùng `.meta` của file và thư mục do Unity sinh).
  - `client/Assets/Game/Tests/Core/DayCycle/TimeConfigTests.cs`, `TimeOfDayTests.cs`, `PeriodConditionTests.cs` (cùng `.meta`).
  - `design/data/time_m1.json`.
- Được sửa: `client/Assets/Game/Tests/Core/Sim/SimDeterminismTests.cs`, chỉ đổi `static Simulation BuildFarm(uint seed)` thành `internal static Simulation BuildFarm(uint seed)`.
- Không được sửa: mọi file khác. Cụ thể không đụng `Game.Core/Sim/*` (Simulation, SimData, SimHash…), `Game.Client/*` (ConfigLoader, SimView, scene…), `client/Assets/Game/Client/Resources/`, `ProjectSettings/`, `design/` ngoài `design/data/time_m1.json`. Không build APK.

## Các bước
1. **`design/data/time_m1.json`**: tạo với nội dung đúng như dưới (thụt 4 dấu cách, cuối file xuống dòng).
   ```json
   {
       "configVersion": 1,
       "midnightPermille": 0,
       "dawnPermille": 250,
       "noonPermille": 500,
       "duskPermille": 750,
       "dawnStartPermille": 200,
       "dayStartPermille": 300,
       "duskStartPermille": 700,
       "nightStartPermille": 800,
       "rotateDurationMs": 3000
   }
   ```
   Xong khi: `python3 -m json.tool design/data/time_m1.json` exit 0.

2. **`Core/DayCycle/TimeConfig.cs`** (namespace `Game.Core.DayCycle`, `using System;`):
   - `public enum TimeLandmark : byte { Midnight = 0, Dawn = 1, Noon = 2, Dusk = 3 }`. Doc comment: "4 mốc chọn thời điểm (0024)."
   - `public enum DayPeriod : byte { Dawn = 0, Day = 1, Dusk = 2, Night = 3 }`. Doc comment: "4 buổi dùng cho điều kiện môi trường sống (0024)."
   - `[Serializable] public class TimeConfig`, doc comment: "Cấu hình buổi trong ngày M1. Nguồn chuẩn: design/data/time_m1.json (đọc bằng JsonUtility — tên trường phải khớp JSON). Pha ghi theo phần nghìn ngày (‰); số là số tạm (0024)." Có các thành phần sau:
     - Các trường public `int`, giá trị mặc định đúng bằng JSON ở bước 1: `configVersion = 1`, `midnightPermille = 0`, `dawnPermille = 250`, `noonPermille = 500`, `duskPermille = 750`, `dawnStartPermille = 200`, `dayStartPermille = 300`, `duskStartPermille = 700`, `nightStartPermille = 800`, `rotateDurationMs = 3000`.
     - `public const int DayMs = 86_400_000;` và `public const int MsPerPermille = 86_400;`
     - `public int LandmarkMs(TimeLandmark m)`: trả `‰ × MsPerPermille` theo mốc. Mốc ngoài enum thì ném `ArgumentException`.
     - `public TimeLandmark WindowAt(int phaseMs)`: phaseMs ngoài [0; DayMs) thì ném `ArgumentOutOfRangeException`. Còn lại: `< midnightMs` → Dusk (phần quay vòng khi midnight > 0); `< dawnMs` → Midnight; `< noonMs` → Dawn; `< duskMs` → Noon; còn lại → Dusk.
     - `public DayPeriod PeriodAt(int phaseMs)`: phaseMs ngoài [0; DayMs) thì ném `ArgumentOutOfRangeException`. Còn lại: `< dawnStartMs` → Night; `< dayStartMs` → Dawn; `< duskStartMs` → Day; `< nightStartMs` → Dusk; còn lại → Night.
     - `public void Validate()` ném `ArgumentException` khi:
       - không thoả `0 <= midnightPermille < dawnPermille < noonPermille < duskPermille < 1000`, thông báo `"Mốc phải tăng dần trong [0, 1000)."`;
       - không thoả `0 <= dawnStartPermille < dayStartPermille < duskStartPermille < nightStartPermille < 1000`, thông báo `"Biên buổi phải tăng dần trong [0, 1000)."`;
       - `rotateDurationMs <= 0`, thông báo `"rotateDurationMs phải > 0."`.
   Xong khi: Unity biên dịch xong, console không có lỗi.

3. **`Core/DayCycle/PeriodCondition.cs`**:
   - `[Serializable] public class PeriodCondition`, doc comment: "Điều kiện môi trường sống loại "buổi": thoả khi đang ở một trong các buổi liệt kê (0024). Chưa nối vào bộ môi trường sống (chưa có code)." Có:
     - `public string[] periods;`
     - `public PeriodCondition() { }` và `public PeriodCondition(params string[] periods) { this.periods = periods; }`
     - `public static DayPeriod ParsePeriod(string s)`: chép đúng mẫu `ParseAbility` ở trên (đổi kiểu thành `DayPeriod`), thông báo `"Buổi không hợp lệ: " + s`.
     - `public void Validate()`: `periods` null hoặc rỗng thì ném `ArgumentException("periods không được rỗng.")`. Phần tử nào không parse được thì để `ParsePeriod` ném. Có buổi trùng thì ném `ArgumentException("periods trùng buổi: " + p)`.
     - `public bool IsMet(DayPeriod current)`: `periods == null` thì trả false. Ngược lại trả true nếu có phần tử mà `ParsePeriod(s) == current`.

4. **`Core/DayCycle/TimeOfDay.cs`**:
   ```csharp
   /// <summary>
   /// Buổi trong ngày (0024): pha = (giờ đồng hồ + OffsetMs) mod 1 ngày, ms nguyên. Tách khỏi đồng hồ mô phỏng — không đọc/ghi SimState.
   /// Chọn mốc: còn trong khung của mốc đó → không đổi gì; ngược lại đặt OffsetMs để pha = mốc, buổi trôi tiếp theo giờ thực.
   /// </summary>
   public sealed class TimeOfDay
   ```
   Có các thành phần sau:
   - Constructor `public TimeOfDay(TimeConfig config, Func<long> clockMs)`: tham số nào null thì ném `ArgumentNullException`. Sau đó gọi `config.Validate()`. `OffsetMs` bắt đầu bằng 0.
   - `public TimeConfig Config { get; }`
   - `public int OffsetMs { get; private set; }`, luôn trong [0; DayMs).
   - `public int PhaseMs => (int)Mod(clockMs() + OffsetMs, TimeConfig.DayMs);`
   - `public DayPeriod Period => Config.PeriodAt(PhaseMs);`
   - `public TimeLandmark CurrentWindow => Config.WindowAt(PhaseMs);`
   - `public bool IsMet(PeriodCondition condition) => condition.IsMet(Period);`
   - `public int SelectLandmark(TimeLandmark m)`, doc comment "Trả cung xoay chiều tiến (ms); 0 = đang trong khung mốc, không đổi gì.". Làm theo thứ tự:
     1. `long now = clockMs();`
     2. `int phase = (int)Mod(now + OffsetMs, DayMs);`
     3. Nếu `Config.WindowAt(phase) == m` thì `return 0;`
     4. `int target = Config.LandmarkMs(m);`
     5. `OffsetMs = (int)Mod(target - now, DayMs);`
     6. `return ForwardArc(phase, target);`
   - `public static int ForwardArc(int fromMs, int toMs)`: tham số ngoài [0; DayMs) thì ném `ArgumentOutOfRangeException`. Trả `(int)Mod((long)toMs - fromMs, DayMs)`.
   - `static long Mod(long a, long m) { long r = a % m; return r < 0 ? r + m : r; }`
   Xong khi: biên dịch xong, console 0 lỗi, và `grep -rn "UnityEngine" client/Assets/Game/Core/DayCycle/` không in gì.

5. **`SimDeterminismTests.cs:9`**: đổi `static Simulation BuildFarm(uint seed)` thành `internal static Simulation BuildFarm(uint seed)`, không sửa gì khác.

6. **Test** trong `client/Assets/Game/Tests/Core/DayCycle/`, namespace `Game.Core.Tests`, `using Game.Core.DayCycle;`. Trong test đặt `const int H = 3_600_000; const int Day = TimeConfig.DayMs;`. Đồng hồ giả là trường `long now;` và lớp được tạo bằng `new TimeOfDay(new TimeConfig(), () => now)`.
   - **`TimeConfigTests.cs`**:
     - `ConfigParsesAndValidates`: đọc `design/data/time_m1.json` (đường dẫn theo mẫu `SimConfigTests.cs:12`), `Validate` không ném. Kiểm `LandmarkMs(Dawn) == 21_600_000`, `LandmarkMs(Dusk) == 64_800_000`, `PeriodAt(17_280_000) == Dawn`, `rotateDurationMs == 3000`.
     - `ConfigMatchesCodeDefaults`: `JsonUtility.ToJson(new TimeConfig())` bằng `ToJson` của bản đọc từ file.
     - `ValidateRejectsBadValues`: mỗi lần dùng một `new TimeConfig()` rồi sửa một trường; trường hợp nào cũng phải `Assert.Throws<ArgumentException>`. Các trường hợp: `dawnPermille = 0`, `duskPermille = 1000`, `midnightPermille = -1`, `noonPermille = 250`, `dayStartPermille = 200`, `nightStartPermille = 1000`, `dawnStartPermille = -1`, `rotateDurationMs = 0`. Thêm `Assert.DoesNotThrow(new TimeConfig().Validate)`.
   - **`PeriodConditionTests.cs`**:
     - `SinglePeriod`: `new PeriodCondition("Night")` thì `IsMet(Night)` là true, còn Dawn/Day/Dusk là false.
     - `MultiplePeriods`: `("Dawn", "Dusk")` thì Dawn, Dusk là true; Day, Night là false.
     - `ValidateRejectsBadValues`: các trường hợp sau đều `Assert.Throws<ArgumentException>`: `periods = null`; `new string[0]`; `"night"`; `"Noon"`; `"1"`; `("Day", "Day")`. Thêm: `JsonUtility.FromJson<PeriodCondition>("{\"periods\":[\"Dawn\",\"Night\"]}")` thì `Validate` không ném và có `IsMet(Night)`.
     - `TimeOfDaySelectChangesPeriod`: `now = 20 * H + 24 * 60_000` (pha 0,85) thì `IsMet(new PeriodCondition("Night"))` là true, `("Day")` là false. Sau `SelectLandmark(Noon)` thì ngược lại: `("Day")` true, `("Night")` false.
   - **`TimeOfDayTests.cs`**:
     - `SameClockSameOffsetSamePhase`: `now = 5 * H + 123`. Tạo 2 instance `a`, `b` dùng chung `now`: `PhaseMs` bằng nhau và bằng `5 * H + 123`. Cả hai cùng `SelectLandmark(Noon)` thì `OffsetMs` và `PhaseMs` bằng nhau. Sau `now += 7 * H` vẫn bằng nhau. Thêm `now = 20000L * Day + 1000` thì `PhaseMs == 1000`, và `now = -1000` (instance mới) thì `PhaseMs == Day - 1000`.
     - `SelectSetsPhaseThenKeepsFlowing`: `now = 3 * H`, `SelectLandmark(Noon)` trả `9 * H` và `PhaseMs == 12 * H`. Sau `now += 5000` thì `PhaseMs == 12 * H + 5000`. Sau `now += 12 * H` thì `PhaseMs == 5000` (qua nửa đêm) và `Period == Night`.
     - `SameWindowDoesNothing`: `now = 0`, `SelectLandmark(Noon)`. Sau `now += 5000` gọi `SelectLandmark(Noon)` thì trả 0, `OffsetMs` không đổi, `PhaseMs == 12 * H + 5000`. `now = 6 * H - 1` (offset 12 giờ nên `PhaseMs == 18 * H - 1`) thì `SelectLandmark(Noon)` trả 0. `now = 6 * H` (`PhaseMs == 18 * H`) thì `SelectLandmark(Noon)` trả `18 * H` (= 64.800.000 = 0,75 ngày) và `PhaseMs == 12 * H`.
     - `ArcAlwaysForward4x4`: duyệt mọi `s`, `t` trong 4 mốc. Với mỗi cặp tạo instance mới, `now = cfg.LandmarkMs(s)` (offset 0, pha đúng bằng mốc `s`). Kỳ vọng `SelectLandmark(t)` trả `((int)t - (int)s + 4) % 4 * 6 * H`, sau đó `PhaseMs == cfg.LandmarkMs(t)`, và nếu `s == t` thì `OffsetMs == 0`. Ghi thêm 4 assert tường minh: Noon→Dawn `18 * H`, Dawn→Noon `6 * H`, Midnight→Dusk `18 * H`, Dusk→Midnight `6 * H`.
     - `ForwardArcValues`: `ForwardArc(12*H, 6*H) == 18*H`; `ForwardArc(6*H, 12*H) == 6*H`; `ForwardArc(777, 777) == 0`; `ForwardArc(Day - 1, 0) == 1`. `ForwardArc(-1, 0)` và `ForwardArc(0, Day)` ném `ArgumentOutOfRangeException`.
     - `PeriodBoundsIncludingMidnight`: với `cfg = new TimeConfig()`, `PeriodAt` của 0 → Night; 17_279_999 → Night; 17_280_000 → Dawn; 25_919_999 → Dawn; 25_920_000 → Day; 60_479_999 → Day; 60_480_000 → Dusk; 69_119_999 → Dusk; 69_120_000 → Night; `Day - 1` → Night. `PeriodAt(-1)` và `PeriodAt(Day)` ném `ArgumentOutOfRangeException`. Kiểm qua `TimeOfDay`: `now = 69_120_000` thì `Period == Night`; `now = Day` (qua nửa đêm) thì Night; `now = Day + 17_280_000` thì Dawn.
     - `WindowBounds`: `WindowAt` của 0 → Midnight; `6*H-1` → Midnight; `6*H` → Dawn; `12*H-1` → Dawn; `12*H` → Noon; `18*H-1` → Noon; `18*H` → Dusk; `Day-1` → Dusk. `WindowAt(Day)` ném `ArgumentOutOfRangeException`.
     - `ClockJumpFollowsClockKeepsOffset`: `now = 0`, `SelectLandmark(Noon)` thì `OffsetMs == 12*H`. `now = 5*H` thì `PhaseMs == 17*H`. `now = -2*H` thì `PhaseMs == 10*H`. `OffsetMs` vẫn `== 12*H`.
     - `ConstructorRejectsBadArgs`: `new TimeOfDay(null, () => 0)` và `new TimeOfDay(new TimeConfig(), null)` ném `ArgumentNullException`. Config có `dawnPermille = 0` thì ném `ArgumentException`.
     - `SelectingLandmarkDoesNotChangeSimHash`: `a = SimDeterminismTests.BuildFarm(20261014)`, `b = SimDeterminismTests.BuildFarm(20261014)`, `tod` dùng đồng hồ giả, `now = 3 * H`. Chạy `for (int i = 0; i < 600; i++)`: mỗi nhịp `a.Tick(); b.Tick();`, rồi ghi `SimHash.Compute(a.State)` và `SimHash.Compute(b.State)` vào 2 danh sách. Khi `i % 50 == 0` (trước khi Tick) thì thêm `now += 5 * H;` và `tod.SelectLandmark((TimeLandmark)(i / 50 % 4));`. Kiểm `CollectionAssert.AreEqual(ha, hb)`, và kiểm có ít nhất một lần `SelectLandmark` trả khác 0 (đếm lại để chắc đã thật sự đổi offset). Cần `using Game.Core.Sim;`.
   Xong khi: MCP `run_tests` EditMode lọc `TimeConfigTests`, `PeriodConditionTests`, `TimeOfDayTests`, `SimDeterminismTests` thì đạt hết.

7. **Chạy toàn bộ + commit:**
   1. EditMode cả bộ: phải đạt hết, gồm 322 test cũ cộng số test mới.
   2. `git status --short`: chỉ còn các file trong Phạm vi (kể cả `.meta`). Commit code với message `brief 0022: lõi buổi trong ngày (TimeOfDay, khung mốc, điều kiện buổi)`.
   3. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc", đặt trạng thái `chờ review`, commit brief với message `brief 0022: kết quả`.

## Tiêu chí xong
- 3 file lõi trong `Core/DayCycle/` đúng chữ ký ở bước 2–4. `grep -rn "UnityEngine" client/Assets/Game/Core/DayCycle/` không in gì.
- `design/data/time_m1.json` đúng nội dung bước 1. `ConfigMatchesCodeDefaults` đạt.
- EditMode cả bộ đạt hết (322 cũ + mới), không có test cũ nào bị sửa ngoài việc đổi `BuildFarm` sang `internal`.
- Chỉ các file trong Phạm vi bị đổi.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Cấu hình JSON + kiểm hợp lệ | 1–2 | `TimeConfigTests` (3) | File parse và validate được, bằng mặc định trong code; 8 giá trị sai đều ném |
| Biên buổi, gồm đoạn qua nửa đêm | 2 | `PeriodBoundsIncludingMidnight` | Đúng tại biên và ngay trước biên; Night quay vòng; ngoài phạm vi thì ném |
| Khung mốc | 2 | `WindowBounds` | 4 khung một phần tư ngày, đúng tại biên |
| Điều kiện buổi | 3 | `PeriodConditionTests` (4) | Một buổi, nhiều buổi, dữ liệu sai đều ném, JSON parse được; chọn mốc thì đổi kết quả điều kiện |
| Cùng giờ + offset → cùng pha | 4 | `SameClockSameOffsetSamePhase` | Bằng nhau; mod đúng với giờ lớn và giờ âm |
| Chọn mốc → pha = mốc rồi trôi tiếp | 4 | `SelectSetsPhaseThenKeepsFlowing` | Pha = 12 giờ ngay lúc chọn, +5000 ms sau 5 giây, qua nửa đêm đúng |
| Khung mốc khi chọn (cùng mốc → 0) | 4 | `SameWindowDoesNothing` | Trong khung trả 0, không đổi offset; ra khỏi khung trả 0,75 ngày |
| Cung luôn theo chiều tiến | 4 | `ArcAlwaysForward4x4`, `ForwardArcValues` | Đủ 16 cặp; trưa→bình minh 0,75; bình minh→trưa 0,25; cùng mốc 0 |
| Đồng hồ nhảy giờ | 4 | `ClockJumpFollowsClockKeepsOffset` | Pha theo giờ máy, offset giữ nguyên |
| Đối số sai | 4 | `ConstructorRejectsBadArgs` | null thì ném `ArgumentNullException`; config sai thì ném `ArgumentException` |
| Đồng hồ mô phỏng độc lập | 4–5 | `SelectingLandmarkDoesNotChangeSimHash` | 600 nhịp có hash trùng nhau dù đã chọn mốc nhiều lần |
| Không tham chiếu engine | 2–4 | `grep -rn "UnityEngine" client/Assets/Game/Core/DayCycle/` | Không in gì |
| Regression mô phỏng | 5 | `SimDeterminismTests` (cũ) | Đạt |
| Regression chung | — | EditMode cả bộ | 322 cũ + mới, đạt hết |

### Lệnh executor chạy
Unity Editor dùng chung qua MCP (quyết định 0010). Nếu Editor đang tắt thì thay MCP bằng `python3 tools/unity_test.py EditMode`. Brief này **không** chạy PlayMode, nên không cần bước dọn `EditorSettings.asset`. Nếu lỡ chạy PlayMode thì sau đó phải chạy `git checkout -- client/ProjectSettings/EditorSettings.asset`.
1. `python3 -m json.tool design/data/time_m1.json > /dev/null` → exit 0.
2. MCP `refresh_unity` → `read_console` (types error) → 0 lỗi.
3. `grep -rn "UnityEngine" client/Assets/Game/Core/DayCycle/` → không in gì.
4. MCP `run_tests` EditMode lọc `TimeConfigTests`, `PeriodConditionTests`, `TimeOfDayTests`, `SimDeterminismTests` → đạt.
5. MCP `run_tests` EditMode cả bộ → ghi dòng tổng kết.
6. `python3 tools/check_design.py` → hợp lệ.
7. `git status --short` → commit (bước 7).

### User tự test
Không có, brief này chỉ làm lõi. Phần xem bầu trời làm ở 7f-2, vòng chọn mốc ở 7f-3.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Cần dùng API Unity trong `Game.Core`.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Bắt đầu: 2026-10-21 14:52
- Base commit: d9a60c4
- Chạy bằng: claude-sonnet-5-5 · medium
- Kết thúc: 2026-10-21 14:57 · commit code 688d197
- File đổi: design/data/time_m1.json; Core/DayCycle/{TimeConfig,PeriodCondition,TimeOfDay}.cs; Tests/Core/DayCycle/{TimeConfigTests,PeriodConditionTests,TimeOfDayTests}.cs (+.meta); SimDeterminismTests.cs (BuildFarm → internal).
- Lệch brief: không.

## Kết quả test
Tất cả phần cần test: **đạt** (test mới 17 = TimeConfigTests 3 + PeriodConditionTests 4 + TimeOfDayTests 10, đều pass; SimDeterminismTests cũ pass).
Lệnh:
1. json.tool → exit 0.
2. refresh_unity (kết nối đóng do domain reload) → read_console error: 0 lỗi; test chạy được nên đã biên dịch.
3. grep UnityEngine trong Core/DayCycle → không in gì.
4. run_tests EditMode assembly Game.Core.Tests → 230/230 đạt.
5. run_tests EditMode cả bộ → 339 total, 339 passed, 0 failed (322 + 17).
6. check_design.py → ✓ design/ hợp lệ: 49 file.
7. git status chỉ file trong Phạm vi; đã commit.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
- Mức: đầy đủ (Opus · medium, phiên con). Đã đọc Kết quả, Kết quả test, `git show --stat 688d197` và diff toàn bộ file lõi + test.
- `Chạy bằng` claude-sonnet-5-5 · medium khớp `Model thực thi` Sonnet · medium.
- Phần cần test: 14/14 dòng có kết quả (17 test mới đạt; EditMode cả bộ 339/339 = 322 + 17; grep UnityEngine trống; check_design hợp lệ). Không chạy lại vì kết quả đủ, khớp số đếm.
- Phạm vi: 16 file đổi đúng danh sách được tạo/sửa (+ `.meta`); `SimDeterminismTests.cs` chỉ đổi 1 dòng `BuildFarm` → `internal`. Không đụng Sim/*, Client/*, Resources, ProjectSettings, design/ ngoài `time_m1.json`.
- Quyết định đã chốt 1–10: đối chiếu từng mục trong diff — ms nguyên `int`, không float; `MsPerPermille = 86_400`; `Func<long>` + `Mod` dương; `SelectLandmark` đọc giờ 1 lần, trong khung trả 0 không đổi offset, ngoài khung đặt offset và trả `ForwardArc`; `WindowAt`/`PeriodAt` đúng thứ tự biên, ném ngoài [0; DayMs); `PeriodCondition` chép đúng mẫu `ParseAbility`; namespace `Game.Core.DayCycle`; không có phần 7f-2/7f-3. `time_m1.json` đúng nội dung bước 1.
- Test khớp từng ca ở bước 6 (giá trị biên, 16 cặp mốc + 4 assert tường minh, giờ âm/lớn, đồng hồ nhảy giờ).
- Ghi chú (không chặn): `SelectingLandmarkDoesNotChangeSimHash` hiện chỉ chứng minh hai farm cùng seed cho hash trùng; `TimeOfDay` vốn không giữ tham chiếu `Simulation` nên test này là bảo vệ kiến trúc (khi 7f-2 nối vào client), không phải phép đo mạnh. Đúng như brief yêu cầu.
- Giờ thực tế: 14:52–14:57 (~5 phút). Cộng vào roadmap khi xong cả bước 7f (3 brief).
- Kết luận: đã duyệt
