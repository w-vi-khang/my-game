# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-21 20:00

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Chọn và làm bước M1 kế tiếp sau sim bước 7. Có hai việc đang chờ:
1. **Quyết định [0025](../design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md)** (Đề xuất, user yêu cầu ghi 2026-10-21): người chơi có công cụ Xới đất (cùng cấp Đào/Đặt) tạo ô ruộng; bấm ô ruộng chọn giống cây; hàng đợi nhiều loại cây; hết hàng đợi thì ô dừng. Đụng nội dung đã Chốt ở 0016/0020 → phải hỏi user các lựa chọn trong 0025 trước:
   - milestone (M1 sau bước 7 hay M2);
   - người chơi xới ngay hay chỉ đánh dấu;
   - vụ kế có xới lại không;
   - kho nhận nông sản;
   - số loại cây M1;
   - có giữ lệnh trồng cây theo vùng không.
2. **Sim bước 8** — môi trường sống: đếm yêu cầu theo sự kiện, 3 loài × 2–3 yêu cầu; ước lượng 3–6 ngày-người ([simulation-tech](../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1)).

## Đã làm (kèm kết quả kiểm chứng)
- **Sim bước 7** (brief 0028, task `.ai/tasks/m1-sim-buoc-7-do-mobile-60-sinh-vat.md`): **đã duyệt 2026-10-21**.
  - Điện thoại Xiaomi: 2386 nhịp, p95 0,956 ms (≤ 5 ms), 31,9 FPS. Pha tốn nhất: rảnh 188,5 µs, đi 130,8 µs.
  - EditMode 371/371, PlayMode 79/79.
  - User xem scene `SimBench` đạt.
  - Số đo đã ghi vào simulation-tech bước 7; giờ thực tế đã ghi vào roadmap.
- Có sẵn để dùng lại:
  - scene đo `Assets/Game/Client/Scenes/SimBench.unity`: đồng bằng 48 × 48 giữa Map 1, chỉ trong scene này;
  - `SimStress` (Core), `SimBenchMeter`, menu "Game/Build APK SimBench".

## Còn lại
1. Hỏi user chọn việc 1 hay 2 (tối đa 2 câu). Nếu chọn 0025 thì hỏi các lựa chọn trong file đó, ghi quyết định, rồi task contract → brief.
2. Sau 4–6 hạng mục M1 thì tính lại hệ số ước lượng ([roadmap](../design/05-roadmap.md#kiểm-tra-phạm-vi)).

## Quyết định quan trọng & lý do
- **Điện thoại:**
  - USB thường không hiện trong adb.
  - Kết nối không dây: dò cổng bằng `adb mdns services`. Không dùng `192.168.1.7:6060` — đó là **hộp Android TV (ADT_3)**, không phải điện thoại; lần đo nhầm trên đó cho 6 FPS.
  - Điện thoại đúng: Xiaomi 24095PCADG, IP 192.168.1.6.
- **Sau build APK:** `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`. **Sau PlayMode:** `git checkout -- client/ProjectSettings/EditorSettings.asset`.

## File liên quan
- `design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md`, `design/04-tech/simulation-tech.md` (bảng bước), `design/02-gameplay/mechanics.md` (môi trường sống, kho & lệnh trồng cây).
- `client/Assets/Game/Core/Sim/{Simulation,SimDemo,SimStress}.cs`, `client/Assets/Game/Client/SimView.cs`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — chọn bước M1 kế: quyết định 0025 (người chơi xới đất + chọn giống + hàng đợi) hoặc sim bước 8 (môi trường sống).
Đọc handoff trước, tóm tắt 3 dòng rồi hỏi user chọn việc nào.
Model đề xuất: Opus · medium (Planner: hỏi quyết định, task contract, brief — AGENTS.md §10.2).
```
