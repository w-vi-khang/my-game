# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-16

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1 — kế hoạch prototype simulation (`design/04-tech/simulation-tech.md` "Kế hoạch prototype (M1)"). Đã xong bước 1–4. Tiếp theo là **bước 5 — thuần thục + tốc độ vào thời gian công đoạn**:
- Xong khi: "Đổi thuần thục → thời gian đổi đúng công thức".
- Ước lượng 1 ngày-người.

## Đã làm (kèm kết quả kiểm chứng)
- Cách làm:
  - Planner (Opus) viết task contract `.ai/tasks/` + brief `.ai/briefs/NNNN`.
  - Executor (Sonnet) chạy `/ai-workflow-kit:exec NNNN` ở phiên khác.
  - Planner chạy `/ai-workflow-kit:review-brief NNNN` (mức "đầy đủ" = đọc diff).
- **Bước 4** (brief 0004; code `5394feb`, review `b7e7a0c`, quyết định 0017):
  - `NoAbility` (chỉ tính sinh vật có nhà);
  - `SimEvent` trong `State.Events` (`StallStarted` / `StallCleared` / `CreatureRespawned`, so `ReportedStall` ở cuối nhịp);
  - `AddHouse` / `AssignHome` (khối đặc 2 × 2 × 2, chặn ô);
  - `HomeReachable` tính lại khi có sự kiện (cờ `homeDirty`);
  - `StuckPhase`: sinh vật rảnh + không có đường về nhà 300 nhịp → hiện lại ở ô kề nhà, hàng đã rơi (`DroppedBy`) theo về;
  - test cũ gắn nhà qua `TestWorlds.HomeAll`.

  [run] EditMode **138/138**.
- Đã ghi vào simulation-tech hàng 4 và roadmap "Theo dõi M1" (16 dòng giờ thực tế — đủ để tính lại hệ số ước lượng khi user muốn).

## Còn lại
1. Hỏi user các điểm mở của bước 5, rồi viết task contract + brief 0005:
   - **Công thức.** `simulation-tech.md:49` ghi: `thời gian gốc ÷ hệ số thuần thục × …` (hệ số là bảng dữ liệu, tính bằng `game-balance`). Còn phải chốt:
     - thuần thục (hiện 1..∞, cả 3 loài đang là 100) ánh xạ ra hệ số thế nào: tuyến tính `BaseTicks × 100 / mastery`, hay bảng theo mức;
     - làm tròn lên hay xuống (phải là số nguyên nhịp);
     - có giới hạn dưới không.
   - **"Tốc độ"** trong tên bước 5 là tốc độ **làm** việc, hay cả tốc độ di chuyển? Tốc độ di chuyển đã có từ bước 1 (`moveSpeedMilliCellsPerSecond`).
   - Thuần thục 2 "vận chuyển được nhiều hàng hơn mỗi lượt" (`mechanics.md:92`, `:291`) — làm ở M1 hay để M2?
   - Số liệu thuần thục của L1–L3 trong `sim_m1.json` có đổi không, hay giữ 100 để test cũ không đổi nhịp?
   - Hệ số nội tại, mệt/bệnh, người làm thứ 2 → ngoài M1 (đã ghi ở "Chuyển sang M2").
2. Đặt cách tính số nhịp trong brief: "làm ngay" = t + B, với t là nhịp giao việc.

## Quyết định quan trọng & lý do
- Brief phải có test cho trường hợp biên nhiều bước. Số nhịp do Planner tính tay. Executor không sửa số trong test cho khớp code.
- Unity Editor dùng chung qua MCP (0010). `tools/unity_test.py` chỉ dùng khi Editor tắt.
- Không tự đổi nội dung đã Chốt. Đổi kế hoạch → ghi quyết định mới (0014–0017).
- Ghi chú từ review 0004 (để bước 7 xem):
  - `HasHomedAbility` duyệt toàn bộ sinh vật cho mỗi stage mở; `ReportPhase` duyệt mọi stage → O(stage × sinh vật) mỗi nhịp.
  - `Events` không giới hạn kích thước; nơi đọc ở bước 6 phải tự `Clear()`.

## File liên quan
- `design/04-tech/simulation-tech.md` (`:49` công thức thời gian, hàng 5 kế hoạch), `design/02-gameplay/mechanics.md` (`:92`, `:290-291`), `design/data/sim_m1.json`, `design/05-roadmap.md`.
- `client/Assets/Game/Core/Sim/{SimConfig,Simulation}.cs`:
  - `Config.BaseTicks(a)`;
  - `Creature.MasteryFor(a)`;
  - `WorkStage.BaseTicks` / `WorkRequired = BaseTicks × 1000`;
  - `WorkPhase` cộng 1000 mỗi nhịp.
- `.ai/briefs/0004*`, `.ai/tasks/m1-sim-buoc-4-dinh-tre-nha-ket.md`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — M1 simulation xong bước 1–4 (brief 0001–0004 đã duyệt). Đọc handoff, tóm tắt 3 dòng; hỏi tôi các điểm mở của bước 5 (thuần thục + tốc độ vào thời gian công đoạn), rồi viết task contract + brief 0005 (/ai-workflow-kit:brief).
```
