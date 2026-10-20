# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-20 (sau khi exec 0012 xong)

<!-- Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file. Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
World 7b3 = brief [0012](briefs/0012-world-bien-world-goc-nhin-hud.md) (biên world chậm dần + sương, HUD góc nhìn) + brief [0013](briefs/0013-world-mep-co-kin.md) (đóng mặt dưới mép cỏ). Sau đó 7c minimap.

## Đã làm (kèm kết quả kiểm chứng)
- Design: dòng 7b3 trong world-tech (1–2 [ý tưởng]), roadmap M1 72,5–118,5, ý tưởng world hành tinh vào ideas.md (`b46d613`). `check_design.py` hợp lệ [run].
- Brief 0012 (`c34653a`) + 0013 (`66661de`) đã viết, Sonnet · medium.
- 0012 exec xong ở phiên con `local_5466ea66-…` (child-sonnet-medium): code `ced4c10`, kết quả `20ceaff`, trạng thái `chờ review` [repo]. Theo báo cáo phiên con: EditMode 269/269, PlayMode 19/19, ảnh `client/Logs/edge_*.png` — **chưa review**.
- Đã đề xuất thẻ review 0012 (task_a4493c50, phiên Opus · medium, `/ai-workflow-kit:review-brief 0012 --child`) — user chưa bấm.

## Còn lại
1. Review 0012 (đầy đủ + user xem): đọc diff `ced4c10`, đối chiếu quyết định; user xem theo mục "User tự test"; ghi giờ thực tế vào roadmap.
2. Exec 0013 (sau 0012, dùng chung Unity Editor) → review → **Planner đo FPS điện thoại bằng Bench** (mép cỏ tăng ~×2 tam giác; cần ≥ 30 FPS). Khi đo: tạo lại Bench từ TerrainView (đã có WorldEdgeFog) → trả `Bench.unity.meta` (guid) + `UnityConnectSettings.asset`.
3. Sau 7b3: brief 7c minimap.

## Quyết định quan trọng & lý do
- Trần bay y = Size.Y + 16 chỉ chậm dần, **không sương** (Planner hiểu từ "cũng chậm dần" — đã báo user; user muốn sương ở trần thì đổi).
- Sương biên: end = baseEnd·(1/baseEnd)^t, start = end·(baseStart/baseEnd)·(1−t); WorldEdgeFog gọi `UnderwaterView.Refresh()` trước để không phụ thuộc thứ tự LateUpdate.
- Mép cỏ: mặt cắt P0 → P1 → P3 → B(tTop, −0,11) → Q(−R, −0,11); đáy phẳng cùng độ sâu → không khe giữa dải và góc.
- Điện thoại cắm USB: AI tự `adb -s PFQCRWVWR46HU4GQ install -r`, đọc logcat. `clear_session` phiên con bị auto mode chặn → mở phiên con mới.

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — world 7b3: brief 0012 đã exec xong (chờ review), brief 0013 (mép cỏ kín) chưa exec.
Bước tiếp theo: /ai-workflow-kit:review-brief 0012 (hoặc bấm thẻ review đã đề xuất), rồi exec 0013.
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (review brief, AGENTS.md §10.2).
```
