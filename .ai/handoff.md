# Handoff — 2026-10-23

## Đang làm
Task `m1-world-7l-dieu-khien-pc` ([task contract](tasks/m1-world-7l-dieu-khien-pc.md)) — làm lại điều khiển PC theo [0034](../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md) (Chốt, user 2026-10-23). Task 7j (camera quan sát, brief 0053) gộp vào 7l c, không duyệt riêng.

## Trạng thái
- Thiết kế xong: 0034 + controls-ui, mechanics, glossary, world-tech (7l a–f), architecture, roadmap (M1 121,5–197; M3 65,5–117,5), ideas, ghi chú 0004/0031/0033, `.ai/project.json` (ràng buộc balance). `check_design.py` exit 0. Chưa commit.
- Task contract: **chờ user xác nhận**.
- Câu hỏi mở đã trả lời (user 2026-10-23): túi +10% = ngoại lệ thứ hai của 0004; bỏ đánh dấu xới = nút trong bảng khi click trái lên biển.
- File `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` đang sửa dở từ trước (không thuộc task này).

## Bước tiếp
1. User xác nhận task contract (và các số tạm / đề xuất Planner trong 0034 nếu muốn sửa).
2. Planner viết brief 7l a (luật lõi Game.Core) → `/ai-workflow-kit:exec`.

## Model đề xuất
Opus · medium cho brief 7l a.
