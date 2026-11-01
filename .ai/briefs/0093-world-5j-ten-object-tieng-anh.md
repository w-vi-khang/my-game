# Brief 0093 — World 5j (lỗi 16): đổi tên object "Người chơi" → "Player", quét tên object trong scene

> Trạng thái: chờ làm
> Review: đầy đủ
> Model thực thi: Sonnet · low
> Cập nhật: 2026-11-01 20:50

## Mục tiêu
Lỗi 16 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md). Ba scene còn GameObject tên tiếng Việt "Người chơi". Đổi tên thành `Player`. Thêm test quét `m_Name` trong file `.unity`/`.prefab` để lỗi này không lặp lại. Task `m1-code-tieng-anh` trước đây chỉ quét `.cs` nên sót scene.

## Bối cảnh cần đọc
**Tên cần đổi** (Planner grep 2026-11-01) [run]. YAML lưu tên dạng escape `\uXXXX`. Cả ba dòng đều có đúng nội dung `  m_Name: "Người chơi"` (2 dấu cách đầu dòng):
- `client/Assets/Game/Client/Scenes/SimView.unity:332`
- `client/Assets/Game/Client/Scenes/TerrainView.unity:332`
- `client/Assets/Game/Client/Scenes/WalkBench.unity:339`

Ngoài ba dòng này, không file `.unity`/`.prefab`/`.asset` nào trong `client/Assets` có ký tự ngoài ASCII hay escape `\u` [run].

**Không code nào tìm object theo tên này** [run]. Grep `Find(`, `.name ==`, `"Player"` trong `client/Assets/Game/**/*.cs`:
- Code không gọi `GameObject.Find`. Scene tham chiếu object qua `fileID`, không qua tên.
- Test PlayMode tự tạo `new GameObject("Player")` (vd `Tests/Client/ClickMovePlayTests.cs:43`). Đổi tên trong scene không ảnh hưởng.
- `FindActionMap("Player", true)` (`Tests/Client/KeyRebindPlayTests.cs:80`) là action map của Input System, không liên quan.

**Test quét hiện có** — `client/Assets/Game/Tests/Core/Text/HardcodedTextScanTests.cs` (assembly `Game.Core.Tests`, EditMode):
- `:12–13` doc comment của lớp (cần sửa, xem bước 2).
- `:19` `static readonly Regex Vietnamese = new Regex("[À-ỹĐđ]");`
- `:26` `static string Root => Path.Combine(Application.dataPath, "Game");` → thư mục `client/Assets/Game`.
- `:42` `public void NoVietnameseLiterals()` — mẫu gom lỗi vào `List<string> bad` rồi `Assert.IsEmpty(bad, string.Join("\n", bad));`.
- `:71` `public void AllUsedKeysExist()` — thêm test mới ngay trước test này.
- File đã có sẵn `using System; System.Collections.Generic; System.IO; System.Linq; System.Text.RegularExpressions; NUnit.Framework; UnityEngine;`. Không cần thêm `using`.

## Quyết định đã chốt (không được đổi)
1. Tên mới là `Player` (user, ghi trong task 5j lỗi 16). Unity ghi tên ASCII không cần nháy, nên dòng mới là `  m_Name: Player`.
2. Đổi bằng cách sửa text YAML đúng 3 dòng trên. Không mở scene trong Editor để đổi tên, vì Unity có thể ghi lại cả file và làm diff lớn.
3. Test mới chỉ kiểm chữ có dấu, dùng regex `Vietnamese` sẵn có, áp lên tên đã giải mã `\uXXXX`. Không kiểm chữ không dấu (`Unaccented`), vì dễ báo nhầm với tên object tiếng Anh.
4. Test quét `*.unity` và `*.prefab` dưới `Root` (`client/Assets/Game`). Không quét `Assets/Scenes/SampleScene.unity` (scene mẫu của template, nằm ngoài `Game/`).
5. Test không có ngoại lệ `// vi-text`: tên object không được có tiếng Việt ở bất kỳ đâu.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Client/Scenes/SimView.unity`, `TerrainView.unity`, `WalkBench.unity` (mỗi file đúng 1 dòng)
  - `client/Assets/Game/Tests/Core/Text/HardcodedTextScanTests.cs`
  - file brief này (mục Kết quả trở xuống)
- Không được sửa: mọi file khác, kể cả `.meta` và `client/ProjectSettings/`.

## Các bước
1. **Kiểm Editor trước khi sửa scene.**
   - MCP `manage_scene`, action `get_active`.
   - Nếu scene đang mở là một trong ba scene trên VÀ đang có thay đổi chưa lưu (dirty) → dừng, đặt `cần Opus`. Lý do: sửa file ngoài có thể làm mất thay đổi chưa lưu.
   - Không dirty → làm tiếp. Ghi tên scene đang mở vào Kết quả.
2. **Thêm test** vào `HardcodedTextScanTests.cs`.
   - Thêm trường sau dòng `:19`:
     ```csharp
             static readonly Regex Escaped = new Regex(@"\\u([0-9A-Fa-f]{4})");
     ```
   - Thêm test ngay trước `[Test]` của `AllUsedKeysExist` (trước `:70`), kèm 1 dòng trống ngăn cách:
     ```csharp
             [Test]
             public void NoVietnameseObjectNames()
             {
                 string root = Root;
                 var files = Directory.GetFiles(root, "*.unity", SearchOption.AllDirectories)
                     .Concat(Directory.GetFiles(root, "*.prefab", SearchOption.AllDirectories))
                     .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToList();
                 files.Sort(StringComparer.Ordinal);
                 Assert.IsNotEmpty(files);
                 var bad = new List<string>();
                 foreach (var f in files)
                 {
                     var lines = Lines(f);
                     for (int i = 0; i < lines.Length; i++)
                     {
                         string t = lines[i].Trim();
                         if (!t.StartsWith("m_Name:", StringComparison.Ordinal)) continue;
                         string name = Escaped.Replace(t.Substring(7).Trim(), m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                         if (Vietnamese.IsMatch(name)) bad.Add(f + ":" + (i + 1) + ": " + name);
                     }
                 }
                 Assert.IsEmpty(bad, string.Join("\n", bad));
             }
     ```
   - Sửa doc comment `:12–13` thành nguyên văn:
     ```csharp
         /// <summary>Quét mọi .cs trong client/Assets/Game: chữ tiếng Việt chỉ nằm trong Resources/Lang/vi.json (0032 C, 0049).
         /// Ngoại lệ: dòng gắn // vi-text trong Tests/ và Core/WorldMapParams.cs (chữ hiển thị kỳ vọng, dữ liệu tạm).
         /// Quét thêm m_Name trong .unity/.prefab: tên object không có tiếng Việt, không có ngoại lệ (5j lỗi 16).</summary>
     ```
   - Xong khi: `refresh_unity` rồi `read_console` Error → 0. Chạy test mới trên scene CŨ → **fail**, thông báo liệt kê đúng 3 dòng `Client/Scenes/SimView.unity:332: Người chơi`, `Client/Scenes/TerrainView.unity:332: …`, `Client/Scenes/WalkBench.unity:339: …`. Ghi output vào Kết quả test.
3. **Đổi tên trong 3 scene** bằng sửa text.
   - Trong mỗi file, thay đúng dòng `  m_Name: "Người chơi"` bằng `  m_Name: Player`.
   - Dùng công cụ Edit, hoặc `sed -i ''` với chuỗi khớp nguyên văn. Không đổi dòng nào khác.
   - Xong khi:
     - `git diff --stat` → mỗi scene `1 insertion, 1 deletion`.
     - `grep -rn 'Ng\\u01B0' client/Assets` → không còn kết quả.
4. **Cho Unity nạp lại.**
   - `refresh_unity`.
   - Nếu scene đang mở (bước 1) là một trong ba scene và Unity hiện hộp thoại hỏi reload → dừng, `cần Opus`, ghi lại.
   - `manage_scene` action `load`, path `Assets/Game/Client/Scenes/SimView.unity`, rồi `get_hierarchy` → có object gốc tên `Player`, không còn `Người chơi`.
   - Sau đó load lại scene đang mở ở bước 1 (nếu khác SimView).
   - Không lưu scene nào.
   - Xong khi: `git status` không có thay đổi mới ở `.unity` ngoài 3 dòng của bước 3.

## Tiêu chí xong
- `NoVietnameseObjectNames` fail trước bước 3 (3 dòng), đạt sau bước 3.
- EditMode toàn bộ đạt.
- `read_console` Error = 0.
- `git diff` chỉ có: 3 dòng `m_Name` trong 3 scene, và phần thêm/sửa trong `HardcodedTextScanTests.cs` đúng như bước 2.

## Test sau khi xong

### Phần cần test
- Test quét tên object (bước 2, QĐ 3–5) · `NoVietnameseObjectNames` chạy trên scene cũ · fail, liệt kê đúng 3 dòng ở bước 2.
- Đổi tên (bước 3, QĐ 1–2) · `NoVietnameseObjectNames` sau bước 3 · đạt.
- Đổi tên (bước 3) · grep + `git diff --stat` · không còn `Ngư`, mỗi scene đổi đúng 1 dòng.
- Unity đọc được scene sau sửa (bước 4) · `manage_scene` load SimView + `get_hierarchy` · có `Player`.
- Các test quét cũ không đổi (bước 2 sửa cùng file) · `NoVietnameseLiterals`, `AllUsedKeysExist`, `BaseFileValid`, `ReachInText` · đạt.
- Không ảnh hưởng phần khác (regression) · EditMode toàn bộ · đạt.

### Lệnh executor chạy
1. Sau bước 2, trước bước 3: MCP `run_tests` EditMode, lọc `NoVietnameseObjectNames` → ghi fail + nội dung thông báo.
2. Sau bước 3: `run_tests` EditMode, lớp `HardcodedTextScanTests` → số đạt/tổng (kỳ vọng 5/5).
3. `run_tests` EditMode toàn bộ → số đạt/tổng.
4. `read_console` Error → 0.
5. `git diff --stat` và `git status` → chỉ 4 file trong Phạm vi (+ file brief).

Không chạy PlayMode: tên object không được code dùng (xem Bối cảnh). Không cần dọn `EditorSettings.asset`.

### User tự test
Không.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd dòng `m_Name` không đúng số dòng/nội dung như Bối cảnh).
- Scene đang mở bị dirty (bước 1), hoặc Unity hiện hộp thoại reload (bước 4).
- Test mới báo thêm dòng ngoài 3 dòng đã biết.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả
Base commit: 49f9563
Chạy bằng: claude-sonnet-5-5 · low
Bước 1: scene đang mở SimView, isDirty=false.
Bước 2: đã sửa HardcodedTextScanTests.cs (chưa commit, đúng như brief). refresh_unity compile OK.

## Kết quả test
## Kiểm chứng
Chưa kiểm được: client_test/client_test_play (tools/unity_test.py) cần Editor tắt, nhưng Editor đang mở và ở Play Mode (dùng chung qua MCP, 0010); run_tests MCP bị từ chối. Checklist chưa đối chiếu: brief đang bị chặn, test chưa chạy.

## Vướng mắc
`run_tests` EditMode lỗi 2 lần: "Cannot start a test run while the Editor is in or entering Play Mode". Editor đang ở Play Mode (không phải do phiên này). Brief không nói được stop Play Mode; Editor dùng chung nên không tự dừng. Đề xuất: Planner/user dừng Play Mode (hoặc cho phép executor `manage_editor stop`), rồi chạy lại từ bước 2 (chạy test trên scene cũ). Chưa đổi scene nào.

**Trả lời (Planner, 2026-11-01 20:50):** Planner đã dừng Play Mode (`manage_editor stop`). Bước 1–2 giữ nguyên kết quả: file test đã sửa, chưa commit. Làm tiếp từ "Lệnh executor chạy" 1 (chạy `NoVietnameseObjectNames` trên scene cũ), rồi bước 3–4. Nếu lần sau gặp Editor ở Play Mode do phiên khác để lại: được dùng `manage_editor` action `stop` một lần, ghi vào Kết quả.

---

## Review
