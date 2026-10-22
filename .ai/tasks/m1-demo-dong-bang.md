# Task: M1 — demo đặt trên đồng bằng san phẳng (SimView + TerrainView), giãn bố trí trại

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: user xác nhận 2026-10-22 (bố trí + camera mục 4, bỏ `FindSite`/`DemoSite`). Brief [0032](../briefs/0032-demo-dong-bang.md). Chen trước khi user xem brief 0031 (user yêu cầu 2026-10-22).

## Mục tiêu
Các scene demo (SimView, TerrainView) diễn ra trên một **đồng bằng phẳng, dễ quan sát** thay vì cửa sổ 9 × 7 "gần phẳng" giữa đồi núi; trại demo **giãn ra** để nhìn rõ nhà / kho / ruộng / sinh vật và còn đất trống quanh để thử Xới, đào, đặt.

Pillar: hỗ trợ kiểm thử P2 (sinh vật làm việc trên nông trại) — công cụ demo của prototype, không phải tính năng game.

Thành phần phụ trách: **client Unity** — `Game.Core.Sim.SimDemo` (C# thuần, test EditMode), `Game.Client.SimView` + component mới cho TerrainView (test PlayMode), scene `TerrainView.unity`.

Làm:
1. **Đồng bằng:** dùng lại `SimStress.FlattenPlain` (san phẳng 48 × 48 cột quanh trọng tâm Map 1, mặt cỏ, bỏ dấu mốc trong vùng — như SimBench, brief 0028).
2. **SimView (world nhiều map):** luôn san đồng bằng (không chỉ khi `stressBench`); trại demo đặt giữa đồng bằng theo bố trí giãn (mục 4); camera nhìn vào trại. Bỏ `SimDemo.FindSite` / `DemoSite` (không còn dùng) cùng test của chúng; bố trí đảo v2 (`SimDemo.Build(sim)`, không có map) giữ nguyên.
3. **TerrainView:** component mới bật san đồng bằng + đặt camera nhìn vào giữa đồng bằng; gắn vào scene `TerrainView.unity`. Test cũ không gắn component → không đổi.
4. **Bố trí giãn** (user xác nhận 2026-10-22), toạ độ (dx, dz) tính từ tâm đồng bằng, mặt đất y = g; camera ở phía −z nhìn về +z:

   ```text
   dz
   +5   . . A A . . . B B . . .
   +4   . . A A . . . B B . . .
   +3   . . . . . . . . . . . .
   +2   . . . 1 2 . . . 3 . . .
   +1   . . . . . . . . . . . .
    0   . . . . . . + . . . . .      + = tâm
   -1   R R . . . . . . . . . K
   -2   R R . . . . . . . . . .
        -6      -3        0  +2      +5   dx
   ```
   - Ruộng R 2 × 2: (−6, −2), (−5, −2), (−6, −1), (−5, −1), y = g.
   - Kho K: (+5, −1), y = g + 1.
   - Nhà A 2 × 2 góc (−4, +4), nhà B 2 × 2 góc (+1, +4), y = g + 1.
   - Sinh vật L1 (−3, +2), L2 (−2, +2) ở nhà A; L3 (+2, +2) ở nhà B.
   - Vẫn 2 nhà, 1 kho, 4 ô ruộng, 3 sinh vật, mỗi ô 1 vụ Cây A ban đầu (như 0030).
   - Camera: (tâm x + 0,5, g + 16, tâm z − 21), xoay Euler(35, 0, 0).

## Ngoài phạm vi
- Đổi thuật toán sinh địa hình / seed; đồng bằng tự nhiên (không san).
- Đổi luật mô phỏng, số liệu `sim_m1.json`.
- SimBench (đã dùng đồng bằng — giữ nguyên bố trí 60 sinh vật).
- Bố trí đảo v2 và các test dùng nó (`SimDemoTests`, `SimViewPlayTests`, `SimFarmUiPlayTests`).
- Scene Bench, HashCheck.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Bố trí giãn đúng toạ độ trên một đồng bằng (nhà/kho/ô ruộng/sinh vật), một vòng trồng–thu–chở về kho chạy xong, `KeepFarming` không chồng chéo — kiểm bằng: test EditMode (thay `SimDemoMapsTests`).
- [ ] SimView (MapsV3): đồng bằng 48 × 48 phẳng quanh trại, không dấu mốc trong đồng bằng, trại đúng bố trí, camera nhìn thấy tâm trại — kiểm bằng: test PlayMode `SimViewMapsPlayTests`, `EditOcclusionPlayTests` (sửa theo đồng bằng).
- [ ] TerrainView có component: đồng bằng phẳng, camera đặt đúng — kiểm bằng: test PlayMode mới. Không có component: world như cũ — kiểm bằng: test PlayMode cũ không gắn component (`LandmarkViewPlayTests`, `MinimapViewPlayTests`, `TerrainStreamingPlayTests`) đạt, không sửa.
- [ ] SimBench + các test khác không đổi; EditMode + PlayMode hết đỏ; `Game.Core` không thêm `using UnityEngine` — kiểm bằng: `run_tests` qua MCP + grep.
- [ ] User xem SimView và TerrainView: đồng bằng dễ quan sát, trại nhìn rõ — kiểm bằng: user chơi thử (mức review `user xem`).

## Ràng buộc
- Unity 6000.4.8f1; `Game.Core` No Engine References; mô phỏng số nguyên.
- Unity Editor dùng chung qua MCP (0010); sau PlayMode `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Ghi giờ bắt đầu/kết thúc. Ước lượng **0,5–1 ngày-người** (M1, công cụ demo — cộng vào dòng "Thử chơi + đo hiệu năng" của roadmap).

## Quyết định đã chốt (ai chốt, khi nào)
- User 2026-10-22: san phẳng như SimBench; áp cho SimView + TerrainView; giãn bố trí trại.

- Planner đề xuất, user xác nhận 2026-10-22: bố trí giãn + camera ở mục 4; bỏ `FindSite`/`DemoSite`.

## Câu hỏi còn mở
- (Không còn.)
