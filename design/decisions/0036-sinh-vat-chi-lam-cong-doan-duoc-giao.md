# 0036 — Sinh vật chỉ làm công đoạn được giao; vòng tầm khi chuột phải ngoài tầm, F hiện vùng Thu + gió hút

> Trạng thái: Chốt
> Ngày: 2026-10-24
> Thay đổi: [0028](0028-nhan-vat-tui-vat-roi-tu-trong-trot.md) mục 12 ("sinh vật vẫn làm như cũ"); [0034](0034-dieu-khien-pc-click-di-chuyen.md) mục 3 (lúc hiện vòng tầm thao tác, hiển thị phím Thu F).

## Bối cảnh
User 2026-10-24, sau khi chơi thử 7l c1 (brief 0058):
- Vòng tầm thao tác hiện ngay khi di chuột ra ngoài tầm → muốn chỉ hiện khi **bấm chuột phải** ngoài tầm.
- F Thu chưa thấy vùng hút → muốn hiện **vùng 2 ô** kèm **hiệu ứng gió hút** vật rơi về phía nhân vật.
- Sinh vật hiện tự làm tiếp mọi công đoạn của ô ruộng, kể cả khi người chơi tự làm (0028 mục 12). User muốn sinh vật **chỉ làm việc được giao**.

Hiện trạng [repo]: [mechanics](../02-gameplay/mechanics.md) dòng 83 "Sinh vật vẫn làm như cũ (0025–0027)"; 0034 mục 3 "con trỏ ra ngoài tầm thao tác → vòng tầm".

## Các lựa chọn
- Giữ 0028 mục 12: sinh vật làm tiếp mọi ô. Người chơi không giữ được ô "của riêng mình".
- **Sinh vật chỉ làm công đoạn được giao** (user): người chơi tự làm được mà không bị sinh vật chen vào; thêm cờ giao việc ở lõi + nút Tiếp tục trên ô tiến trình.

User trả lời 3 câu hỏi làm rõ (2026-10-24): Tiếp tục = 1 công đoạn kế; người chơi chen vào ô đã giao → sinh vật làm tiếp; người chơi tự xới ô có biển mà hàng đợi có hạt → sinh vật tự gieo.

## Quyết định
1. **Sinh vật chỉ làm công đoạn được giao.** Có 3 cách giao:
   - **Biển canh tác** = giao xới. Xới xong, hàng đợi trống → ô đứng yên.
   - **Hàng đợi có hạt** = giao trồng: đủ chuỗi gieo → tưới → thu hoạch (chở về kho) → xới lại → vụ kế theo hàng đợi, như 0025–0027.
   - **Nút Tiếp tục** trên ô tiến trình của bảng ô ruộng = giao **đúng 1 công đoạn kế**.
2. **Người chơi tự gieo** (c2: hạt chỉ gieo được ô hàng đợi trống — [world-tech 7l c](../04-tech/world-tech.md)) → vụ **tự làm**:
   - sinh vật không tự tưới. Ô tiến trình hiện nút **Tiếp tục** → sinh vật tới tưới;
   - cây chín → sinh vật không tự thu hoạch, để đó. Bấm Tiếp tục lần nữa → sinh vật thu hoạch;
   - thu hoạch xong → ô chờ xới lại, cũng chỉ làm khi bấm Tiếp tục (hoặc người chơi tự xới).
   - Người chơi tự làm công đoạn (tự thu hoạch, phép Mưa tưới) thì vẫn được như 0028.
3. **Người chơi chen vào ô đã giao trồng** (tự gieo / tự thu hoạch / Mưa trước khi sinh vật tới): công đoạn đó xong ngay như luật "Lệnh người chơi" hiện có, rồi sinh vật **làm tiếp** các công đoạn còn lại và vụ sau.
4. **Người chơi tự xới ô có biển:** hàng đợi có hạt → sinh vật tự gieo và làm tiếp đủ chuỗi (như mục 1); hàng đợi trống → ô đứng yên.
5. **Vòng tầm thao tác:** chỉ hiện khi **bấm chuột phải** mà ô đích ngoài tầm thao tác (không hiện khi chỉ di chuột). Hiện rồi mờ dần trong khoảng 1 giây [số tạm]. Thay dòng "con trỏ ra ngoài tầm" của 0034 mục 3.
6. **F Thu:** bấm F → hiện **vùng 2 ô** quanh nhân vật (vòng mờ) + **hiệu ứng gió hút** (vệt / hạt bay về phía nhân vật); vật rơi trong 2 ô bay về nhân vật rồi vào túi.
   - Thời gian hiện ~0,5 giây, vật rơi bay ~0,3 giây [số tạm]; hình hiệu ứng tạm (LineRenderer / ParticleSystem có sẵn), art sau.
   - Không có vật rơi trong 2 ô vẫn hiện vùng + gió (đề xuất Planner, user xem ở c2).

## Hệ quả
- **Thành phần:** client Unity (Game.Core: cờ giao việc ô ruộng + luật Sim; client: nút Tiếp tục, vòng tầm, hiệu ứng F). Không thêm thành phần.
- **Pillar:** P2 "Sinh vật là cốt lõi" — giao việc là thao tác chủ động, sinh vật làm theo lệnh, không lấn việc người chơi tự làm.
- **Lịch làm** (user 2026-10-24):
  - mục 5–6 gộp vào **world 7l c2** (+0,25–0,5 ngày-người) [ước lượng Planner];
  - mục 1–4 là **sim bước 7g**, làm **sau 7l c** (c2 + c3), 1–2 ngày-người [ước lượng Planner]. Vượt quỹ M1 thêm (đã vượt từ 0028/0034, user chấp nhận).
- **Cập nhật:** [mechanics](../02-gameplay/mechanics.md) (luật M1 người chơi tự trồng trọt), [simulation-tech](../04-tech/simulation-tech.md) (bước 7g), [world-tech](../04-tech/world-tech.md) (7l c), [roadmap](../05-roadmap.md), [glossary](../glossary.md) (Tiếp tục / vụ tự làm).
