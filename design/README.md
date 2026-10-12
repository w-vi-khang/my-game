# Thiết kế my-game

> Trạng thái: Nháp

Nguồn chuẩn cho mọi quyết định về game — mọi thành phần (client Unity và các phần khác). Kiểm tra cấu trúc: `python3 tools/check_design.py --summary`.

## Thứ tự làm (gợi ý)
1. **Vision** — pitch, pillars, phạm vi. Mọi thứ sau đó đối chiếu với pillars.
2. **Core loop → cơ chế** — chứng minh game vui ở mức ý tưởng.
3. **Thành phần hệ thống → kiến trúc → công nghệ + rủi ro** — game cần những thành phần nào ngoài client Unity (server, công cụ, dịch vụ...), mỗi phần làm gì, rủi ro nào cần prototype sớm.
4. **Roadmap M0–M2** — ước lượng, đủ để bắt đầu prototype (M1).
5. Story, art, âm thanh, level, kinh tế — viết dần, song song với prototype.

Quy ước: mỗi file có `> Trạng thái: Nháp | Đang xem xét | Chốt`; đề xuất chưa chấp nhận ghi `[ý tưởng]`; chỗ chưa viết là `_(chưa có)_`; sơ đồ dùng Mermaid. Chỉ chủ project chuyển file sang **Chốt**.

## Mục lục & trạng thái
| Tài liệu | Nội dung | Trạng thái |
|---|---|---|
| [summary.md](summary.md) | Tóm tắt game một trang + danh sách cần đặt tên | Nháp |
| [00-vision.md](00-vision.md) | Pitch, pillars, người chơi, phạm vi | Nháp |
| [01-story/world.md](01-story/world.md) | Thế giới, bối cảnh, tông giọng | Nháp |
| [01-story/characters.md](01-story/characters.md) | Nhân vật | Nháp |
| [01-story/plot.md](01-story/plot.md) | Cốt truyện, chương, kết thúc | Nháp |
| [01-story/naming.md](01-story/naming.md) | Quy ước đặt tên: tên game, vùng đất, loài, nhân vật, người chơi | Nháp |
| [02-gameplay/core-loop.md](02-gameplay/core-loop.md) | Vòng lặp cốt lõi | Nháp |
| [02-gameplay/mechanics.md](02-gameplay/mechanics.md) | Cơ chế | Nháp |
| [02-gameplay/controls-ui.md](02-gameplay/controls-ui.md) | Điều khiển, màn hình, HUD, onboarding | Nháp |
| [02-gameplay/progression.md](02-gameplay/progression.md) | Tiến trình, độ khó, mở khoá | Nháp |
| [02-gameplay/economy.md](02-gameplay/economy.md) | Tài nguyên, cân bằng | Nháp |
| [02-gameplay/levels.md](02-gameplay/levels.md) | Level, nội dung | Nháp |
| [03-art-audio/art-style.md](03-art-audio/art-style.md) | Phong cách hình ảnh, nguồn asset | Nháp |
| [03-art-audio/audio.md](03-art-audio/audio.md) | Nhạc, SFX | Nháp |
| [04-tech/components.md](04-tech/components.md) | Thành phần hệ thống: client Unity + những gì khác | Nháp |
| [04-tech/tech-stack.md](04-tech/tech-stack.md) | Công nghệ theo từng thành phần | Nháp |
| [04-tech/architecture.md](04-tech/architecture.md) | Kiến trúc hệ thống, dữ liệu, giao tiếp, client | Nháp |
| [04-tech/risks.md](04-tech/risks.md) | Rủi ro | Nháp |
| [05-roadmap.md](05-roadmap.md) | Milestones, ước lượng | Nháp |
| [glossary.md](glossary.md) | Thuật ngữ | Nháp |
| [ideas.md](ideas.md) | Kho ý tưởng | Nháp |
| [README.md](README.md) | Mục lục này | Nháp |

Quyết định đã chốt: [decisions/](decisions/README.md).
