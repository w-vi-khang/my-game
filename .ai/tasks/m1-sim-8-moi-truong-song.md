# Task: M1 sim 8 — môi trường sống (đếm yêu cầu theo sự kiện, loài trở lại)

> Model thực thi: Sonnet · medium (AGENTS.md §10.2).
> Trạng thái:
> - Phần đã duyệt: [simulation-tech "Môi trường sống"](../../design/04-tech/simulation-tech.md#môi-trường-sống) (user duyệt 2026-10-13) và dòng 8 của kế hoạch prototype.
> - Luật gameplay: [mechanics "Môi trường sống" + "Sinh vật trở lại — luật M1"](../../design/02-gameplay/mechanics.md#sinh-vật-trở-lại--luật-m1).
> - Điều kiện "buổi": [0024](../../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md) mục 7.
> - Ô ruộng không tính là đất hay cỏ: [0020](../../design/decisions/0020-o-ruong-tao-khi-xoi-va-huy-vu.md), [0025](../../design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md).
> - Ước lượng 3–6 ngày-người (roadmap), chia **8a lõi** 1,5–2,5 + **8b client** 1,5–3,5 [ước lượng Planner].
> - Task contract do Planner lập 2026-10-24 khi user AFK. 8a chỉ dùng phần đã duyệt. **8b chờ user trả lời các câu hỏi bên dưới.**

## Mục tiêu
- **8a (brief [0069](../briefs/0069-sim-8a-moi-truong-song-loi.md)):** lõi `Game.Core`:
  - dữ liệu yêu cầu của loài trong JSON;
  - bộ đếm ô theo sự kiện `WorldEditor.CellChanged`, đổi ngay khi đào/đặt/xới/mưa/nước lan;
  - xét yêu cầu (ô, cây trồng, công trình, loài đang ở, buổi);
  - loài đủ yêu cầu → đánh dấu đã trở lại và báo ra ngoài.
  - Client chưa đổi hành vi.
- **8b (chưa viết brief):**
  - sinh 2 cá thể ở rìa vùng, đi về phía người chơi, tự vào đội làm việc (luật M1);
  - bảng danh sách yêu cầu của loài trên PC;
  - nối tracker vào `SimView`.
- **Pillar:** P1 (cải tạo thế giới để đưa loài trở lại) — mechanics bảng cơ chế dòng "Môi trường sống".
- **Thành phần:** client Unity. Luật nằm ở `Game.Core` (namespace mới `Game.Core.Habitat`), hiển thị ở `Game.Client`.

## Ngoài phạm vi
- Mức đáp ứng / nhu cầu sinh vật, bệnh, mệt, quà (M2).
- Vùng theo bố cục map (M2), lưu bộ đếm (không lưu — tính lại khi nạp).
- Thẻ bài, giới hạn số sinh vật.

## Tiêu chí chấp nhận
Đúng cột "Xong khi" dòng 8: "Đào/đặt → tiến độ đổi ngay; đủ yêu cầu → loài trở lại".
- [ ] 8a — EditMode:
  - đào/đặt/xới/mưa/nước lan → số đếm đổi ngay, luôn bằng số đếm quét lại từ đầu (sau ≥ 200 lần sửa ngẫu nhiên);
  - ô ruộng không tính là cỏ;
  - mỗi loại yêu cầu đúng;
  - đủ yêu cầu → loài trở lại đúng một lần, theo thứ tự cấu hình;
  - tất định;
  - `Game.Core` không tham chiếu UnityEngine.
- [ ] 8b — PlayMode: đào/đặt trong scene → số trên bảng đổi cùng khung; đủ yêu cầu → 2 cá thể xuất hiện và vào đội làm việc. User xem.

## Ràng buộc
- Số nguyên, tất định. Không quét lại cả vùng mỗi nhịp; quét một lần khi tạo tracker.
- Yêu cầu là dữ liệu: thêm loài = thêm JSON, không thêm code.
- Không đổi mã băm mô phỏng hiện có (8a không sửa `Simulation`/`SimHash`).

## Quyết định đã chốt (ai chốt, khi nào)
- Cơ chế (danh sách yêu cầu theo loài, đếm theo sự kiện, vùng M1 = cả map, đạt hết yêu cầu mở khoá → loài trở lại, 2 cá thể ở rìa map, chỉ số = base): user duyệt 2026-10-13.
- Điều kiện "buổi" là một loại yêu cầu: user chốt 0024 (2026-10-21).
- Ô ruộng không tính là đất/cỏ: user 0020 (2026-10-19), 0025 (2026-10-21).
- Đề xuất Planner 2026-10-24, đổi được, ghi `[số tạm]` / chưa chốt ở `habitat_m1.json`:
  - Yêu cầu mẫu lấy từ mechanics ("ví dụ để thử"):
    - L1: ≥ 20 ô cỏ + ≥ 1 cây đã gieo;
    - L2: ≥ 10 ô cỏ cạnh nước;
    - L3: ≥ 15 ô đá lộ thiên + ≥ 1 kho.
  - "Cây" = ô ruộng đang có vụ đã gieo (world M1 chưa có cây tự nhiên).
  - "Cạnh hồ" = ô cạnh nước. Lấy ô cỏ có ít nhất một trong 4 ô ngang ở cùng tầng hoặc tầng trên là nước.
  - "Lộ thiên" = ô ngay trên là trống hoặc nằm ngoài world.
  - `unlockedAtStart` mặc định = cả L1–L3, để demo hiện tại không đổi tới khi user trả lời câu (b).
  - Vùng mặc định = cả world (theo text 2026-10-13). Hỗ trợ thêm hình chữ nhật cột để 8b chọn.

## Câu hỏi còn mở (cho user — chặn 8b, không chặn 8a)
- **(a) Vùng đếm M1:** cả Map 1 / cả world (text duyệt 2026-10-13, lúc đó world chỉ có 1 map) hay vùng mô phỏng (đồng bằng 48 × 48 giữa Map 1)?
  - Map 1 có sẵn rất nhiều cỏ và đá → đếm cả map thì L1/L3 gần như đạt ngay, nên mở khoá không còn là thử thách.
  - Đề xuất Planner: vùng mô phỏng 48 × 48. Hoặc giữ cả map nhưng tăng ngưỡng.
- **(b) Loài mở sẵn trong demo:** hiện demo có sẵn L1–L3.
  - Muốn thấy "loài trở lại" phải khoá ít nhất một loài lúc đầu.
  - Đề xuất Planner: mở sẵn L1 (xới, gieo), khoá L2 + L3. Người chơi tự tưới/thu (đã làm được từ 0028) cho tới khi L2 trở lại.
- **(c) Chỗ hiện danh sách yêu cầu trên PC:** 0034 bỏ "loài ghim" khỏi HUD PC.
  - Đề xuất Planner: bảng riêng mở bằng một phím (đổi được trong bảng đổi phím 7l f), liệt kê từng loài + từng yêu cầu `đã/cần` + dấu đạt.
  - Hoặc thêm một tab trong Cài đặt?
