# 0006 — Công cụ kiểm tra asset (ngoại lệ M0)

> Trạng thái: Chốt
> Ngày: 2026-10-12

## Bối cảnh
- Thử nghiệm art bằng AI ([roadmap](../05-roadmap.md) mục C) dùng Blender + MCP. MCP chỉ là cầu nối: AI tự nhìn ảnh để đánh giá → không lặp lại được, không bảo đảm chất lượng asset (user, 2026-10-12).
- Sinh vật/nhân vật làm bằng AI ([art-style](../03-art-audio/art-style.md)) → hàng chục loài, mỗi loài nhiều biến thể; kiểm tay từng cái tốn công cho một người làm.
- Quy tắc M0 ([AGENTS.local.md](../../AGENTS.local.md)): không viết code thành phần nào trước M1. User cho phép **ngoại lệ** cho công cụ này (2026-10-12).

## Các lựa chọn
- **Tự viết script Blender headless** (`tools/asset_check/`): kiểm topology, tỉ lệ, pivot, UV, vật liệu, rig, xuất–nhập lại FBX/GLB; ngưỡng trong `rules.json`; exit 0/1 + báo cáo JSON. Ước lượng 2–3 ngày-người; bảo trì khi đổi version Blender (vd `Material.use_nodes` sẽ bị bỏ ở Blender 6.0 — cảnh báo khi chạy trên 5.0.1 [run]).
- **Công cụ có sẵn:** không tìm thấy công cụ miễn phí chạy headless bao đủ các mục (tra 2026-10-12). [UV Audit](https://superhivemarket.com/products/uv-audit) chạy headless nhưng trả phí, mã đóng, chủ yếu UV/mesh. [uGame](https://github.com/abcg-code/uGame) — link 404. [Khronos glTF-Validator](https://github.com/KhronosGroup/glTF-Validator) (Apache-2.0) chỉ kiểm đúng chuẩn glTF → dùng làm một phần nếu cài.
- **Không có công cụ:** AI/user xem ảnh từng asset. Không tốn công làm, nhưng lỗi tỉ lệ/trục/weight chỉ lộ khi vào Unity.

## Quyết định
**Chốt (user, 2026-10-12): tự viết script Blender headless**, đặt ở `tools/asset_check/` (cùng chỗ với `check_design.py`). Mỗi bước của quy trình asset ([art-style](../03-art-audio/art-style.md#quy-trình-asset-ý-tưởng)) phải qua script trước khi sang bước sau. Script là công cụ của quy trình art, không phải một phần của game.

## Hệ quả
- Ưu tiên trước mắt (user, 2026-10-12): **chưa làm model hoàn chỉnh**; tập trung dựng mesh world (địa hình). Phần kiểm duyệt (script này + kiểm ảnh concept) vẫn hoàn thiện để giữ đồng bộ, tránh lỗi vô lý.
- `selftest.py` nằm trong `verify` của `.ai/project.json`.
- Ngoại lệ M0 ghi trong [AGENTS.local.md](../../AGENTS.local.md).
- [components.md](../04-tech/components.md): thêm thành phần "Công cụ kiểm tra asset".
- [art-style.md](../03-art-audio/art-style.md): quy trình asset + quy cách kỹ thuật dẫn tới `rules.json`; ngưỡng polygon/texture/xương trong `rules.json` là **tạm** cho tới khi đo trên mobile ở M1.
- [roadmap](../05-roadmap.md): thêm vào mục C (M0); dùng lại ở M2 (tích hợp asset).
- Cùng thành phần: `tools/asset_gen/hf_generate.py` (tạo model thô qua HF Spaces, user chốt 2026-10-12 — [art-style](../03-art-audio/art-style.md)); chạy ngoài Blender, đầu ra đi qua `check_asset.py`.
- Việc còn lại: Unity có nhập đúng trục/tỉ lệ không thì chỉ kiểm được khi có project Unity (M1) — script hiện chỉ kiểm xuất rồi nhập lại trong Blender.
