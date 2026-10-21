# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-21 17:32

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Bước tiếp theo của M1: simulation bước 6a. Task contract [m1-sim-buoc-6a-luat-o-ruong.md](tasks/m1-sim-buoc-6a-luat-o-ruong.md) (user quyết 2026-10-19, [0020](../design/decisions/0020-o-ruong-tao-khi-xoi-va-huy-vu.md); làm sau 6b theo [0021](../design/decisions/0021-lam-6b-truoc-6a.md)) đang **chờ viết brief**.

## Đã làm (kèm kết quả kiểm chứng)
- World 7g xong. Brief 0025 đã duyệt; user xem 1–7 ổn [user 2026-10-21].
  - Kết quả chạy [run]: EditMode 340/340, PlayMode 68/68.
  - Điện thoại: avgFps 35,3 (thermal 2), holeFrames 0, hash `a7719c8999939263`.
  - Ảnh `client/Builds/bench-0025.png` thấy ô tròn.
- Giờ 7g đã ghi vào `design/05-roadmap.md` và `design/04-tech/world-tech.md`.

## Còn lại
1. Đọc task contract 6a + `design/04-tech/simulation-tech.md` dòng 6a (`:101`). Nếu còn điểm chưa chốt thì hỏi user, sau đó viết brief (số tiếp theo là 0026).
2. Giao Executor, review, ghi giờ.
3. Sau 6a: sim bước 7 (đo mobile 60 sinh vật giả, ≤ 5 ms/nhịp).

## Quyết định quan trọng & lý do
- Bài học điện thoại:
  - Máy hay rớt USB, dùng `adb connect 192.168.1.6:5555`.
  - Trước khi chụp, kiểm `isKeyguardShowing`: máy khoá thì ra ảnh đen và không có log BENCH.
  - Máy nóng (thermal 2) làm FPS còn khoảng 35.
- Phiên con: mở mới bằng `start_session` (clear phiên con bị chặn).

## File liên quan
- `.ai/tasks/m1-sim-buoc-6a-luat-o-ruong.md`, `design/decisions/0020-…md`, `0021-…md`
- `design/04-tech/simulation-tech.md` (kế hoạch prototype, dòng 6a)

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — world 7g đã xong (0025 duyệt); bước tiếp theo là sim 6a (ô ruộng chỉ có sau khi xới, đặt/đào thì huỷ vụ): /ai-workflow-kit:brief 6a
Đọc handoff và task contract 6a trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (viết brief, AGENTS.md §10.2).
```
