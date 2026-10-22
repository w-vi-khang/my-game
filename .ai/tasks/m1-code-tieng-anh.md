# Task: code tiếng Anh toàn client (ngoài 8 file brief 0046)

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2 — code theo brief đủ chi tiết; Planner Opus · medium viết brief + review)
> Ngày: 2026-10-22 · thuộc M1 (mở rộng task [0032 cài đặt và bảng chuỗi](m1-cai-dat-va-bang-chuoi.md))

## Mục tiêu
Theo chỉ đạo user (2026-10-22): **mọi phần thuộc code dùng tiếng Anh**; chữ hiển thị cho người chơi bằng tiếng Việt **chỉ nằm trong `client/Assets/Game/Client/Resources/Lang/vi.json`**. Brief 0046 đã làm cho 8 file (`SiteStatus`, `CreatureStatus`, `SimView`, `TerrainEditInput`, `PlayerFarmActions`, `MinimapView`, `TimeDial`, `SettingsView`). Task này làm phần còn lại của `client/Assets/Game/**/*.cs`, rồi mở rộng `HardcodedTextScanTests` ra toàn bộ để chặn chữ tiếng Việt quay lại.

Hiện trạng [run 2026-10-22, commit 259355e, regex `"(?:[^"\\]|\\.)*"` chứa `[À-ỹĐđ]`, bỏ dòng `//`]: **671 dòng / 88 file** ngoài 8 file của 0046.
- Theo loại: assert/thông báo test 394 · exception 163 · log (`Debug.Log*`, `TestContext.WriteLine`) 28 · menu Editor 12 · tên GameObject/mesh 8 · khác 66 (bench overlay, `ToString()`, tên khối `AddBlock`, hằng tên).
- Nặng nhất: `Core/Sim/SimConfig.cs` 45 (exception kiểm cấu hình), `Tests/World/BlockMesherTests.cs` 43, `Tests/World/WorldMapGeneratorTests.cs` 30, `Core/Sim/Simulation.cs` 17, `Core/WorldMapParams.cs` 16, `Client/TerrainChunkView.cs` 10, `Client/Editor/*` (17 `MenuItem("Game/…")` + exception).
- Chỗ không dấu (regex không bắt): `Client/SimView.cs:209` `Box("Kho " + s.Id)` + `Tests/Client/PlayerBagSowPlayTests.cs:431` tìm `"Kho 0"`. Thêm: `Core/View/SimClock.cs:18,20` exception.
- Danh sách đầy đủ: chạy lại script quét lúc viết brief (không chép vào đây, sẽ cũ).

## Ngoài phạm vi
- 8 file của brief 0046 (đã xong). Comment `//`, `///` giữ tiếng Việt.
- Tên dấu mốc tạm `"Công trình 1A…3B"` (`Core/WorldMapParams.cs:57-62`), tên map, id cây/loài: dữ liệu, giữ nguyên (user 2026-10-22); ghi vào danh sách cần đặt tên nếu chưa có.
- `design/`, `tools/`, scene, `.ai/briefs` cũ (menu cũ nhắc trong brief cũ là lịch sử, không sửa).
- Các điểm user thấy khi xem 0046 (chữ "Map 1", nút ngôn ngữ `tt`/`vi`, khoá "Dựng lại {0} chunk…" bắt đầu bằng `\n`, nhãn sinh vật vẽ đè bảng Cài đặt): việc riêng của Planner 0046 / task khác.
- Không đổi hành vi, bố cục, số liệu, định dạng số.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] 0 literal chứa `[À-ỹĐđ]` ngoài comment trong `client/Assets/Game/**/*.cs`, trừ danh sách miễn ở QĐ 4 — kiểm bằng: `HardcodedTextScanTests.NoVietnameseLiterals` (đã mở rộng) qua.
- [ ] Không còn chữ tiếng Việt không dấu trong danh sách từ (QĐ 5), vd `"Kho "` — kiểm bằng: test quét không dấu qua.
- [ ] Tên GameObject/mesh/khối (`"Dấu mốc"` ở `TerrainChunkView.cs:50` + `LandmarkView`, `"Thân"/"Đầu"/"Mũi"` ở `PlayerCharacter.cs:108-110`, `"Kho "`…) sang tiếng Anh; test tìm theo tên cũ (`LandmarkViewPlayTests.cs:45,91,109`, `PlayerBagSowPlayTests.cs:431`…) đổi theo — kiểm bằng: PlayMode toàn bộ qua.
- [ ] Exception, `Debug.Log*`, `TestContext.WriteLine`, thông báo assert sang tiếng Anh; **giữ nguyên tiền tố đầu dòng và số/định dạng** — kiểm bằng: diff review + grep các tiền tố ở QĐ 3 vẫn còn đủ.
- [ ] 17 menu `Game/…` sang tiếng Anh, giữ `Build APK <tên>` — kiểm bằng: `grep -n MenuItem client/Assets/Game/Client/Editor/*.cs` không có ký tự tiếng Việt.
- [ ] Bench overlay (`TerrainBench`, `WalkBench` `result`) sang tiếng Anh trong code (chữ cho người làm game, không qua `vi.json`).
- [ ] EditMode + PlayMode toàn bộ qua; test chỉ đổi **thông báo** assert, không đổi giá trị kỳ vọng, trừ kỳ vọng là tên object đã đổi.

## Ràng buộc
- `Game.Core` vẫn không tham chiếu UnityEngine (test quét đọc file nằm trong assembly test, như 0046 QĐ 7).
- Unity Editor dùng chung qua MCP (0010); dọn `EditorSettings.asset` sau PlayMode.
- Ước lượng: ~1 ngày-người (cơ học, nhiều file) — 2 brief: **(A)** code chạy (Core, World, Client, Editor) + test tìm object theo tên; **(B)** thông báo/log trong test + mở rộng test quét. B sau A.
- Ghi giờ thực tế từng brief.

## Quyết định đã chốt (ai chốt, khi nào)
1. Mọi phần thuộc code tiếng Anh; chữ hiển thị người chơi chỉ qua `vi.json`; comment giữ tiếng Việt (user, 2026-10-22; giống 0046 QĐ 9).
2. Chữ cho người làm game (bench overlay, menu Editor, log, exception) → tiếng Anh viết thẳng trong code, không qua `vi.json` (user, 2026-10-22).
3. Giữ tiền tố log đã có: `CHECKPOINT`, `DEMOPLAIN`, `LANDMARKVIEW`, `LANDMARKVIEWTEST`, `LANG`, `MESHSTATS`, `MESHTIME`, `MINIMAP`, `MINIMAPCORNER`, `MINIMAPTEST`, `PLAYER`, `PLAYTEST`, `SETTINGS`, `SIMMEM`, `SIMVIEWTEST`, `STARTERBAG`, `TERRAINVIEW`, `UNBALANCED`, `WALKBENCH` (+ các tiền tố của 0046). `tools/` hiện không grep nội dung log tiếng Việt [run grep `tools/`].
4. Test quét mở rộng ra **mọi `.cs` trong `client/Assets/Game`**, bỏ comment. Miễn chỉ theo danh sách ngắn trong test, mỗi mục kèm lý do: file test so chữ hiển thị với `vi.json` (`SiteStatusTests`, `CreatureStatusTests`, `StringsTests`, chính `HardcodedTextScanTests`) và dữ liệu giữ nguyên (`WorldMapParams.cs` tên dấu mốc). Danh sách cuối do Planner chốt trong brief B (user, 2026-10-22).
5. Thêm danh sách từ tiếng Việt không dấu (vd `Kho`, `Tui`) để bắt chỗ như `SimView.cs:209` (user, 2026-10-22). Danh sách từ cụ thể + cách khớp (nguyên từ, phân biệt hoa thường) do Planner chốt trong brief B.

## Câu hỏi còn mở
_(đã chốt 2026-10-22, Planner, trong brief 0049)_
- Test so chữ hiển thị tiếng Việt bằng literal: **giữ literal** (để test kiểm đúng chữ người chơi thấy), gắn ` // vi-text` cuối dòng. Dấu chỉ hợp lệ trong `Tests/**` và `Core/WorldMapParams.cs` (danh sách miễn); dấu sai chỗ hoặc thừa → test quét báo lỗi.
- Từ không dấu: `kho tui nha ruong cay khoi hinh bien huong khong duoc cua gieo xoi tuoi hoach buoi binh minh hoang dem`, nguyên từ, không phân biệt hoa thường. Chạy thử trên HEAD 0c6a82c: chỉ bắt `EditOcclusionPlayTests.cs:53` và `SeaSkyTintPlayTests.cs:14` (đều là chữ tiếng Việt thật), không bắt nhầm literal tiếng Anh [run].
- Brief A = 0048 (đã duyệt), brief B = 0049.
