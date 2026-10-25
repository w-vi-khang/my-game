# Handoff — 2026-10-25 (tối)

## Điều phối
- Phiên này là Planner (Opus). Executor = phiên con Sonnet · medium. Phiên con đang dùng: **exec-0075** (local_6c0ab03f-9253-47b6-9631-26bdfb1af2e4, do phiên Planner trước mở). Phiên Planner mới không clear được phiên này → mở phiên con mới (`start_session`, model claude-sonnet-5-5, effort medium, use_worktree false, prompt `/ai-workflow-kit:exec NNNN --child`).
- Unity Editor dùng chung qua MCP → mỗi lúc chỉ một phiên chạy.

## Đã xong hôm nay
- 0040 nước chảy (nguồn + lan giới hạn) → brief 0075 **đã duyệt**.
- User chơi thử: nước trong rãnh phải chảy hết, chỉ giới hạn khi ra chỗ rộng; làm thác nước → [0041](../design/decisions/0041-nuoc-chay-trong-ranh-hep-va-thac-nuoc.md) (Chốt). Tài liệu đã cập nhật (M1 124,5–203; world-tech 5e/5f; mechanics; glossary).
- Brief 0076 (5f) code đạt review (EditMode 640, PlayMode 275), trạng thái `chờ review`. **Lỗi:** thác không hiển thị — `client/Assets/Game/World/BlockMesher.cs:414-419` chỉ vẽ mặt trên ô nước khi ô trên rỗng, nên cột nước rơi vô hình.

## Bước tiếp
1. Viết **brief 0077** (task `m1-world-nuoc-chay`, phần 5f, trong 1–2 ngày-người của 0041): vẽ mặt bên của ô nước khi ô ngang (trong map) là `Empty` (cùng submesh/material nước `Water.mat`; mặt bên tới đủ chiều cao ô vì M1 vẽ đầy ô). Cân nhắc mặt đáy khi ô dưới rỗng (hiếm). Đọc `AddWaterSurface` trong `BlockMesher.cs` để làm mặt bên cùng kiểu (đỉnh, UV, pháp tuyến). Test: BlockMesherTests (số mặt nước của một cột nước rơi 4 ô = 4 × 4 mặt bên + 1 mặt trên; mặt nước cạnh khối đặc không vẽ), mesh kín/không mặt thừa giữa hai ô nước; PlayMode chụp ảnh thác. Rủi ro: số tam giác chunk biển (mặt bên chỉ khi kề Empty nên biển nguyên vẹn không đổi — kiểm bằng test đếm tam giác hiện có/`TerrainBench` nếu có số chuẩn).
2. Giao Executor, review, rồi **user xem gộp**: mục "User tự test" của brief 0076 (thác đổ vào ao không tràn; rãnh dài chảy hết; ra chỗ rộng lan 7 ô; chặn giữa rãnh → đoạn sau rút; lấp nguồn → rút; biển như cũ) + thác nhìn thấy được. Đạt → 0076 + 0077 `đã duyệt`, world-tech 5e/5f "Xong".
3. Còn chờ user: chơi thử gộp bước 7 mục A–F ([playtest-buoc-7.md](playtest-buoc-7.md)). Sau đó rà tiêu chí xong M1 (roadmap).

## Model đề xuất
Opus · medium (Planner: brief 0077 + review).
