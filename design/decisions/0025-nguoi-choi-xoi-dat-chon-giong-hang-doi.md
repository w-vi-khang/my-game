# 0025 — Người chơi đánh dấu xới (biển canh tác), sinh vật xới tạo ô ruộng, bấm ô ruộng chọn giống, hàng đợi 2 loại cây, các kho thông nhau

> Trạng thái: Chốt
> Ngày: 2026-10-21
> Thay đổi: [0016](0016-o-ruong-kho-nha-va-sinh-vat-ket.md) (M1 một loại cây; nhiều loại cây ở M2; lệnh trồng cây đặt theo vùng), [0020](0020-o-ruong-tao-khi-xoi-va-huy-vu.md) (ô ruộng chỉ có khi **sinh vật** xới xong), [mechanics — Kho & lệnh trồng cây](../02-gameplay/mechanics.md#kho--lệnh-trồng-cây--luật-m1), "người chơi làm công đoạn" đang ở M2 ([simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1)).

## Bối cảnh
User yêu cầu ghi chú tính năng này (2026-10-21, lúc viết brief 0028). Chưa làm.
- Người chơi có thêm công cụ **Xới đất**, cùng cấp với Đào và Đặt ([brief 0027](../../.ai/briefs/0027-tach-dao-dat-ban-mo.md)).
  - Chọn mặt trên của khối cỏ hoặc khối đất → khối thành **ô ruộng**, trồng được.
  - Xới xong **chưa giao việc trồng**.
- Người chơi bấm vào ô ruộng → **chọn giống cây** cần trồng.
- **Hàng đợi vụ** (đã có từ 0016, 2 chỗ) dùng để lên lịch trồng **nhiều loại liên tiếp**.
- Hết hàng đợi → dừng mọi việc của ô ruộng đó. Luật này đã đúng như hiện tại: ô không có vụ thì không mở công đoạn.

Hiện tại [repo]:
- Ô ruộng sinh ra khi **lệnh trồng cây** đặt lên một vùng ô (`Simulation.AddFarmOrder`, `client/Assets/Game/Core/Sim/Simulation.cs:760`). Hàng đợi có sẵn 1 vụ.
- Mỗi vụ bắt đầu bằng công đoạn xới **của sinh vật**. Xới xong ô mới thành `Farmland` (0020).
- Mới có một loại cây: hàng đợi lưu loại cây của từng vụ (giá trị 0) nhưng chưa có bảng giống (0016).

## Các lựa chọn
Planner đưa ra 2026-10-21; user đã chọn — xem Quyết định.
- **Milestone:**
  - (a) M1, làm sau sim bước 7 và trước bước 8;
  - (b) M2, cùng "người chơi làm công đoạn" và "nhiều loại cây + cấp ô ruộng".
- **Ai xới:**
  - (a) Người chơi xới tức thì (đúng lời user);
  - (b) Người chơi chỉ đánh dấu, sinh vật L1 đi xới.
- **Vụ kế trên ô đã là ô ruộng:**
  - (a) bắt đầu từ gieo, bỏ công đoạn xới;
  - (b) vẫn xới lại như 0016.
- **Kho nhận nông sản:**
  - (a) kho gần nhất tới được;
  - (b) người chơi chọn kho khi chọn giống.
- **Số loại cây M1:** 2–3 loại, khác nhau ở thời gian lớn, hạt giống và sản lượng. Số liệu do game-balance đặt sau.
- **Lệnh trồng cây theo vùng** (`AddFarmOrder`):
  - (a) bỏ;
  - (b) giữ làm cách nhanh để xới và lên lịch nhiều ô cùng lúc.

Ước lượng ban đầu [ý tưởng, AI], tổng **4,5–8 ngày-người** (bản sau khi chọn: Hệ quả):

| Việc | Ngày-người |
|---|---|
| Công cụ Xới của người chơi | 0,5–1 |
| Ô ruộng không cần lệnh, đổi chuỗi công đoạn | 1–2 |
| Bảng giống cây trong `sim_m1.json` | 1–2 |
| Bảng chọn giống + xem/xoá hàng đợi khi bấm ô ruộng (PC + chạm) | 2–3 |

## Quyết định
**Chốt (user, 2026-10-21):**
1. **Milestone:** M1, làm sau sim bước 7 và **trước bước 8**. Chia hai bước: **sim 7b** (lõi) và **sim 7c** (client).
2. **Công cụ Xới** cùng cấp với Đào và Đặt.
   - Người chơi **chỉ đánh dấu** ô: mặt trên của khối đất hoặc cỏ, phía trên trống.
   - Đánh dấu xong thì **biển canh tác hiện ngay** trên ô đó.
   - Sinh vật có năng lực xới (L1) tới xới. Xới xong thì **biển mất** và ô thành ô ruộng.
   - Xới **không cần chọn giống trước**. Thứ tự: đánh dấu → biển → xới → ô ruộng.
3. **Chọn giống:** bấm ô ruộng → chọn giống → vụ được thêm vào cuối hàng đợi vụ (ô ruộng cấp 1 có 2 chỗ, như 0016).
   - Ô vừa xới xong: vụ đầu bắt đầu từ **gieo**.
   - Vụ kế trên ô đã thu hoạch: ô **vẫn là ô ruộng nhưng phải xới lại**, rồi mới gieo.
   - Hết hàng đợi → ô ruộng đứng yên, không mở công đoạn.
4. **Hai loại cây ở M1.** Thời gian lớn, hạt giống, sản lượng khác nhau; số liệu do `game-balance` đặt sau. Tên cây do user đặt; tạm gọi Cây A, Cây B.
5. **Bỏ lệnh trồng cây theo vùng** (`AddFarmOrder`) ở M1: người chơi đánh dấu **từng ô**.
   - Sau này người chơi **mở khoá được "chọn thông minh"** theo khuôn 2 × 2, 3 × 3, 1 × 2, 1 × 3, 2 × 1, 3 × 1… Việc này **không thuộc M1**, ghi vào [ideas](../ideas.md).
6. **Kho:**
   - Nông sản chở về **kho gần nhất tới được**.
   - **Các kho thông nhau:** hàng trong mọi kho tính chung; thêm kho thì thêm sức chứa (tổng sức chứa các kho).
   - Người chơi bấm **nút Kho** để xem vật phẩm trong kho.
   - Công cụ để người chơi tự đặt thêm kho để M2. M1 đặt kho bằng bố trí sẵn (`SimDemo`, `SimStress`).

**Planner đề xuất, user xác nhận khi duyệt brief 7b/7c** (như 0020):
- **"Đã xới" là trạng thái của ô ruộng:**
  - xới xong → bật;
  - thu hoạch xong → tắt.
  - Vụ bắt đầu khi trạng thái đang bật → vào thẳng gieo; đang tắt → xới trước.

  Cách này cho đúng cả ba trường hợp: vụ đầu sau khi đánh dấu, vụ kế, và ô ruộng đứng yên lâu rồi mới chọn giống.
- **Bỏ đánh dấu:** dùng công cụ Xới lên ô đang có biển → bỏ đánh dấu, biển mất. Nếu sinh vật đang tới xới thì về rảnh.
- **Đào hoặc che ô đang có biển (chưa xới)** → bỏ đánh dấu, không trả gì; giống luật "ô chưa xới" của 0020.
- **Đào hoặc che ô ruộng** → giữ luật 0020: huỷ vụ, trả hạt theo pha, xoá hàng đợi.
  - Đào: ô thành rỗng, +1 đất.
  - Che: ô trở lại đất.
  - "Ô rời lệnh" trong 0020 nay đọc là "ô thôi là ô ruộng".
- **Xoá vụ trong bảng hàng đợi:**
  - vụ chưa bắt đầu → chỉ rời hàng đợi;
  - vụ đang làm → huỷ vụ như 0020 (trả hạt theo pha, hàng đã thu vẫn chở về), nhưng ô vẫn là ô ruộng.
- **Hạt và nông sản theo loại cây:**
  - Kho bắt đầu có 10 hạt mỗi loại [số tạm].
  - Gieo trừ 1 hạt **đúng loại**; hết hạt loại đó → đình trệ "thiếu hạt" (như hiện tại).
  - Sức chứa tính chung mọi loại, như 0016.
  - Mỗi lần thu hoạch vẫn là 1 lượt vận chuyển.
- **Kho gần nhất** = kho có đường đi ngắn nhất từ ô ruộng, xét lúc giao công đoạn vận chuyển. Không kho nào tới được → đình trệ "không có đường". Kho đầy = tổng chung đầy.
- **Bảng kho** hiện:
  - nông sản và hạt của từng loại cây;
  - sức chứa đã dùng / tổng.

  Vật liệu đào được vẫn ở kho vật liệu của người chơi như cũ (túi đồ M2).
- **Chạm (mobile):** bảng chọn giống, bảng hàng đợi và nút Kho làm cho PC trước. Thao tác chạm đi cùng phần cảm ứng của hạng mục Điều khiển, như công cụ Đào/Đặt (brief 0027).
- **"Không dừng trên ô ruộng"** của [0019](0019-chan-nhau-ranh-quanh-nha-khong-dung-tren-ruong.md) áp cho ô đang có biển và mọi ô ruộng.

## Hệ quả
- **Pillar:**
  - P2: sinh vật làm việc trên nông trại.
  - P1: ô cỏ bị xới thì không còn tính cho môi trường sống (0020).
- **Thành phần:** client Unity. Lõi trong `Game.Core.Sim` + `design/data/sim_m1.json`; công cụ, biển, bảng chọn giống, bảng kho ở client.
- **Ước lượng** (AI, chưa xác minh), tổng **5,5–10 ngày-người**:

  | Việc | Bước | Ngày-người |
  |---|---|---|
  | Đánh dấu từng ô thay lệnh vùng, trạng thái "đã xới", bỏ đánh dấu; sửa `SimDemo`, `SimStress`, test | 7b | 1–2 |
  | 2 loại cây trong `sim_m1.json`, hạt và nông sản theo loại | 7b | 1–2 |
  | Kho thông nhau + kho gần nhất | 7b | 0,5–1 |
  | Công cụ Xới + biển canh tác | 7c | 0,5–1 |
  | Bảng chọn giống + xem/xoá hàng đợi khi bấm ô ruộng (PC) | 7c | 1,5–2,5 |
  | Nút Kho + bảng kho | 7c | 1–1,5 |

- **Roadmap:**
  - M1 79–128,5 → **84,5–138,5**.
  - Mục "hàng đợi nhiều loại cây + cấp ô ruộng 1–2" của M2 còn "cấp ô ruộng 0,5–1" → M2 **325–620**.
- **Bước 8 (môi trường sống):** ô ruộng không tính là đất hay cỏ, như 0020. Biển canh tác không phải công trình.
- **Test hiện có:** test dùng `AddFarmOrder` chuyển sang đánh dấu từng ô (+ thêm vụ). Mốc thời gian `FarmTests` có thể đổi vì vụ đầu không còn công đoạn xới riêng sau khi đánh dấu. Brief 7b ghi rõ số nào được đổi.
- **Cập nhật cùng lần:**
  - [mechanics](../02-gameplay/mechanics.md#kho--lệnh-trồng-cây--luật-m1);
  - [simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1): bước 7b, 7c;
  - [controls-ui](../02-gameplay/controls-ui.md): công cụ Xới, bảng chọn giống, nút Kho;
  - [roadmap](../05-roadmap.md), [glossary](../glossary.md), [ideas](../ideas.md), [summary](../summary.md) (tên 2 loại cây), [decisions/README](README.md).
