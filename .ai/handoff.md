# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-12

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M0 — kế hoạch & thiết kế. Đang chốt cơ chế cốt lõi trong `design/`, sau đó lập roadmap.

## Đã làm (kèm kết quả kiểm chứng)
- Brainstorm → user chọn trọng tâm: **hồi sinh môi trường sống + nông trại tự động hoá bằng sinh vật**; map có ranh giới thêm theo mùa, phần ngoài cốt truyện sinh theo seed.
- Pillars P1–P4 **user đã xác nhận** (P1 Hồi sinh để sưu tầm, P2 Sinh vật là cốt lõi, P3 Tự do trong khung truyện, P4 Chiến đấu bằng bộ phép của sinh vật) — `design/00-vision.md`.
- Đã ghi: lệnh làm việc/công đoạn/đình trệ/tua nhanh (giờ server), phép (2 bộ sưu tầm riêng, nhóm phép, ô phép 3/4 + ô đặc biệt), nhận sinh vật (rút thưởng chỉ ra loài đã hồi sinh), tiến hoá, cấp độ người chơi, game online F2P với 2 loại tiền (tiền nạp không ảnh hưởng sức mạnh PvP), PvP 1v1 thời gian thực 3–5 sinh vật + điểm xếp hạng, mở sau khi đạt ngưỡng PvP.
- Quyết định `design/decisions/0003-server.md` (**Đề xuất**, chờ user Chốt): cần Backend (M2) + Game server (PvP thời gian thực, sau phát hành).
- Co-op đảo sandbox → spin-off để sau (`design/ideas.md`).
- `.ai/project.json`: thêm `router` (kit 0.6.0) — bỏ từ khoá `skill`, tắt `bug-fix`/`feature-implementation`/`code-review` trong M0.
- Kiểm chứng [run 2026-10-11 22:53]: `python3 tools/check_design.py` → exit 0 (24 file, 52 chỗ trống, 28 [ý tưởng]); `validate_skills.py --project .` → 4 skill hợp lệ; router không còn gợi ý nhầm với 3 prompt từng sai.

## Còn lại
1. ~~Chỉ số cá thể trong PvP~~ — XONG 2026-10-12: user tự thiết kế (2 bảng chỉ số làm việc/chiến đấu, base + hệ số quy đổi + trần, phân bố loài + ngẫu nhiên + vượt trội + điểm cấp độ/bậc; PvP chỉ bỏ vượt trội, điểm cấp độ/bậc vẫn tính — thay quy tắc cũ "phát triển sau ngưỡng không ảnh hưởng PvP"). Ghi ở `design/02-gameplay/mechanics.md` mục "Chỉ số sinh vật"; I1–I6 vào `design/ideas.md`. Câu hỏi mở còn ở cuối mục đó.
1b. XONG 2026-10-12: nguyên tắc tiền nạp mới (ảnh hưởng tốc độ, chặn ở mức tối đa; ưu đãi nạp: vật phẩm hỗ trợ, túi đồ +10%, giới hạn sinh vật +10%); ghép trận theo hạng → cấp độ trung bình; quỹ điểm làm việc/chiến đấu riêng; phân bổ lại bằng vật phẩm; chỉ số làm việc cũng có phân bố loài + ngẫu nhiên; vượt trội xét mỗi lần sở hữu cá thể mới; cơ chế mới "Nhu cầu sinh vật" và "Giới hạn sinh vật & kho sinh vật" trong mechanics.md. Việc mới: tạo skill cân bằng (ghi ở 05-roadmap.md).
1c. XONG 2026-10-12: cấp độ trung bình = trung bình cấp độ sinh vật mang vào trận; "chặn quá may" cho vượt trội (không bảo hiểm); quyết định `design/decisions/0004-tien-nap.md` (Đề xuất, chờ user Chốt).
2. XONG — roadmap bản đầu `design/05-roadmap.md` (10 giờ/tuần, ×1,5; tổng M1–M4 ~9,5–18 năm). User: chưa cắt, giữ phạm vi (2026-10-12); đo tốc độ thật ở M1 rồi tính lại.
2b. XONG bản đầu (2026-10-12): `design/04-tech/architecture.md` (hệ thống, dữ liệu, giao tiếp, client), `design/04-tech/tech-stack.md` (client M1: URP, Input System 1.19, Burst 1.8 + Mesh.MeshData, A* tự viết, uGUI; netcode quyết ở M3), quyết định `0005-render-pipeline.md` (Đề xuất). Chờ user xem; chưa render thử Mermaid.
3. Còn mở (hỏi khi tới): chỉ số cá thể chi tiết; cơ chế "Nghiên cứu" (chưa có ước lượng); nguồn tiền trong game; mana; điều kiện mở map; yêu cầu ngưỡng PvP; user Chốt 0003, 0004, 0005.
4. Checklist chưa đạt: "Điều khiển PC + mobile" và "Tiền tệ & cửa hàng" không gắn pillar (ghi lý do); 0003 chưa Chốt; giá dịch vụ backend/hosting và quy định loot box `[chưa xác minh]`.

3b. XONG 2026-10-12: ba mức môi trường sống → danh sách yêu cầu của loài; bảo hiểm gói rút thưởng (công khai) + lệnh `pity` trong balance.py; skill mới: local-game-balance, local-game-story, local-level-design, local-naming (+ design/01-story/naming.md). Roadmap mục "Công việc M0" có danh sách cần Chốt (A1–A3, B1–B8). Tiếp theo đề xuất: art-style.md (B7) — thống nhất art style trước khi bắt đầu.

3c. XONG 2026-10-12: trang bị (sinh vật, bậc trang bị, ghép, rèn ở lò rèn, tính PvP; M3 13–23); bảo hiểm gói là cho cá thể vượt trội; art-style.md bản đầu (voxel mịn tham khảo Pokopia, model mịn + khung chiếm chỗ). Phát hiện: Pokopia trùng nhiều ý tưởng cốt lõi → ghi vision + risks. XONG 2026-10-12: địa hình khối theo Pokopia (AI xem ảnh Serebii), 3 trang bị/sinh vật không ngẫu nhiên, bảo hiểm ưu tiên + phần thưởng theo gói, điểm khác biệt với Pokopia; asset ứng viên trong art-style.md.

3d. XONG 2026-10-12: địa hình bề mặt mịn liền (voxel world 35–55), biến thể hiển thị (3–6, M2), không chibi, tỉ lệ 1 ô = 1 đơn vị (nhân vật 1,5 × 1), sinh vật/nhân vật làm bằng AI, ma năng (phép trùng). Tổng M1–M4 ~10,1–19,1 năm. ĐANG CHỜ user cài uv + mcp-for-blender để thử nghiệm art (kế hoạch trong 05-roadmap mục C).

## Quyết định quan trọng & lý do
- Game online, F2P, tài khoản bắt buộc; PvE ở world riêng; PvP thời gian thực do game server quyết định (user chọn "trận đấu nảy lửa", chống gian lận).
- Tiền nạp (user, 2026-10-12): ảnh hưởng tốc độ tiến trình là không tránh được; chỉ chặn ở mức tối đa (người không nạp cũng đạt được mọi mức tối đa; vượt trội bị bỏ trong PvP).
- Logic chiến đấu phải tách khỏi hiển thị từ M1–M2 để chạy được trên game server (Unity Dedicated Server, [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/dedicated-server-build.html)).
- Dùng thuật ngữ "lệnh làm việc" (nông trại) ≠ "nhiệm vụ" (cốt truyện); "phép" thay cho skill/kỹ năng.

## File liên quan
- `design/00-vision.md`, `design/02-gameplay/mechanics.md`, `design/02-gameplay/core-loop.md`, `design/02-gameplay/progression.md`, `design/02-gameplay/economy.md`
- `design/04-tech/components.md`, `design/04-tech/risks.md`, `design/decisions/0003-server.md`
- `design/glossary.md`, `design/ideas.md`, `design/05-roadmap.md`

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — M0 thiết kế game: lập roadmap.
Bước tiếp theo: dùng skill local-game-roadmap, đưa ước lượng từ design/02-gameplay/mechanics.md vào design/05-roadmap.md + gantt.
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
```
