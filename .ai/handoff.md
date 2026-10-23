# Handoff — 2026-10-24

## Đang làm
Task `m1-world-7l-dieu-khien-pc` ([task contract](tasks/m1-world-7l-dieu-khien-pc.md)).
- **7l c1** (brief 0058) **đã duyệt** 2026-10-24 (review đầy đủ + user xem đạt); giờ thực tế ghi ở world-tech dòng 7l c (commit `bed6052`).
- User góp ý sau chơi thử → quyết định [0036](../design/decisions/0036-sinh-vat-chi-lam-cong-doan-duoc-giao.md) (Chốt, commit `680697c`):
  - mục 5–6 gộp vào **c2**: vòng tầm chỉ hiện khi bấm chuột phải ngoài tầm (mờ dần ~1 giây); F hiện vùng 2 ô + gió hút vật rơi bay về nhân vật.
  - mục 1–4 (sinh vật chỉ làm công đoạn được giao, nút Tiếp tục) = **sim 7g**, task riêng **sau 7l c** (c2 + c3).
- File lạ chưa track: `client/Assets/InitTestScene644b8064-….unity(.meta)` — scene tạm của Unity Test Runner, không phải của review; xoá khi Editor không chạy test.

## Bước tiếp
1. Planner (Opus) viết brief **7l c2**: cầm vật phẩm từ túi (khối bản mờ / đỏ, hạt chỉ ô ruộng hàng đợi trống, camera quan sát là vật phẩm), xoá Tab tạm, + 0036 mục 5–6. Tra docs Unity 6000.4 cho API hiệu ứng (LineRenderer / ParticleSystem).
2. Sau đó c3 (máu / mana), 7l d, 7l e; sim 7g theo 0036.

## Model đề xuất
Opus · medium (viết brief c2).
