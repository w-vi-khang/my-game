# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-11 22:53

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
1. **User chọn phương án "chỉ số cá thể trong PvP"** (chưa ghi vào design/). Ràng buộc: R1 tiền nạp không tăng sức mạnh PvP; R2 phát triển sau ngưỡng PvP không ảnh hưởng PvP; R3 rút thưởng có giá trị nhờ chỉ số cá thể.

   | # | Phương án | Giữ R1–R3? | Ước lượng thô (AI) |
   |---|---|---|---|
   | I1 | Chuẩn hoá hoàn toàn: PvP dùng chỉ số chuẩn của loài ở ngưỡng; chỉ số cá thể chỉ cho PvE/world | R1 R2 ✅, R3 chỉ ở PvE | 2–3 ngày |
   | I2 | Khuynh hướng cùng tổng: tổng chỉ số PvP như nhau, chỉ số cá thể quyết định phân bổ (±~10%) — kiểu Nature của Pokémon ([PokéBase](https://pokemondb.net/pokebase/53654/what-do-natures-do?show=53657)) | ✅ cả ba | 5–8 ngày |
   | I3 | Trần dễ đạt bằng chơi/tiền trong game — kiểu Hyper Training ([Bulbapedia](https://bulbapedia.bulbagarden.net/wiki/Hyper_Training)) | ❌ R1 (nạp → có cá thể tốt sớm hơn) | 5–8 ngày |
   | I4 | Chỉ số cá thể chỉ cho làm việc, chiến đấu cố định theo loài | ✅ R1 R2, ⚠️ trái ý "hợp chiến đấu" | 1–2 ngày |
   | I5 | Hai thể thức: xếp hạng chuẩn hoá, tự do dùng đủ chỉ số | ⚠️ thể thức tự do vi phạm R1 | 8–12 ngày |
   | I6 | "Nét riêng" cân bằng thay cho con số | ✅ cả ba | 10–20 ngày + cân bằng |

   AI đề xuất **I2 trên nền I1**. Sau khi user chọn: ghi vào `design/02-gameplay/mechanics.md` (mục "Chế độ nhiều người", thay câu hỏi mở về chỉ số cá thể), phương án còn lại vào `design/ideas.md`, thuật ngữ mới (vd "Khuynh hướng") vào `design/glossary.md`, chạy `python3 tools/check_design.py`.
2. Lập roadmap bằng skill `local-game-roadmap`: đưa ước lượng từ `mechanics.md` vào bảng milestone `design/05-roadmap.md` + gantt.
3. Còn mở (hỏi khi tới): chỉ số cá thể chi tiết; cơ chế "Nghiên cứu" (chưa có ước lượng); nguồn tiền trong game; mana; điều kiện mở map; yêu cầu ngưỡng PvP; user Chốt 0003.
4. Checklist chưa đạt: "Điều khiển PC + mobile" và "Tiền tệ & cửa hàng" không gắn pillar (ghi lý do); 0003 chưa Chốt; giá dịch vụ backend/hosting và quy định loot box `[chưa xác minh]`.

## Quyết định quan trọng & lý do
- Game online, F2P, tài khoản bắt buộc; PvE ở world riêng; PvP thời gian thực do game server quyết định (user chọn "trận đấu nảy lửa", chống gian lận).
- Tiền nạp chỉ: làm đẹp, hiệu ứng, tăng cơ hội, đẩy nhanh xây dựng — không ảnh hưởng chỉ số/sức mạnh PvP.
- Logic chiến đấu phải tách khỏi hiển thị từ M1–M2 để chạy được trên game server (Unity Dedicated Server, [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/dedicated-server-build.html)).
- Dùng thuật ngữ "lệnh làm việc" (nông trại) ≠ "nhiệm vụ" (cốt truyện); "phép" thay cho skill/kỹ năng.

## File liên quan
- `design/00-vision.md`, `design/02-gameplay/mechanics.md`, `design/02-gameplay/core-loop.md`, `design/02-gameplay/progression.md`, `design/02-gameplay/economy.md`
- `design/04-tech/components.md`, `design/04-tech/risks.md`, `design/decisions/0003-server.md`
- `design/glossary.md`, `design/ideas.md`, `design/05-roadmap.md`

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — M0 thiết kế game: chốt phương án "chỉ số cá thể trong PvP" rồi lập roadmap.
Bước tiếp theo: trình lại bảng phương án I1–I6 trong handoff để tôi chọn, rồi ghi phương án được chọn vào design/02-gameplay/mechanics.md (các phương án còn lại vào design/ideas.md).
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
```
