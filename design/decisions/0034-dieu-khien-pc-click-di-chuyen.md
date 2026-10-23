# 0034 — Điều khiển PC kiểu click di chuyển, chế độ hành động, túi ô vuông, máu/mana, đổi phím M1

> Trạng thái: Chốt
> Ngày: 2026-10-23
> Thay đổi: [0031](0031-tam-voi-moi-thao-tac-8-o.md) (tầm 8 ô → tầm thao tác 10 / tầm thực hiện 2) · [0033](0033-tach-lop-hanh-dong-dieu-khien.md) mục 2 (bảng đổi phím M3 → M1, luật giữ nguyên) · [0028](0028-nhan-vat-tui-vat-roi-tu-trong-trot.md) (phím, gieo, túi) · [0004](0004-tien-nap.md) (túi đồ không có mức tối đa → có giới hạn ô) · nguyên tắc "nhắm theo tâm màn hình" của [controls-ui](../02-gameplay/controls-ui.md) (chỉ còn cho cảm ứng + tay cầm) · camera "góc 3 xoay tự do" (user 2026-10-13) trên PC.

## Bối cảnh
User (2026-10-23) muốn làm lại toàn bộ điều khiển và tương tác trên PC: góc camera mặc định như LoL, chuột trái click để đi, phím cho hành động, chuột phải cho thao tác chọn, túi ô vuông + túi phụ + kho chuyển đồ qua lại, HUD gọn, máu/mana, đổi mọi phím. Hiện nay [repo]: WASD đi, giữ chuột giữa xoay camera, Tab đổi công cụ Đào → Đặt → Xới → Camera, chuột trái dùng công cụ, hộp viền trắng theo tâm nhắm, tầm với 8 ô (0031), túi/kho dạng danh sách nút, chữ hướng dẫn và dòng "Đang ở buổi" trên HUD.

Planner hỏi 13 câu + 3 câu bổ sung; user trả lời 2026-10-23 (mục Quyết định). Số ghi `[số tạm]` và chỗ ghi `(đề xuất Planner)` là Planner đề xuất theo lời user "bạn tự quy định" (câu 10) hoặc để chạy được, chỉnh trong `design/data/*.json` (0032).

## Các lựa chọn
Mỗi câu Planner đưa đề xuất; user chọn hoặc sửa. Các điểm user sửa so với đề xuất:
- Chuột trái **luôn** để đi (không làm hành động); hành động bằng chuột phải.
- Tầm hai lớp: click được trong 10 ô, nhưng phải đi tới gần 2 ô mới làm.
- Đặt khối = cầm khối từ túi, không phải chế độ có phím riêng.
- Túi có giới hạn, phân trang (không cuộn).
- Chuyển đồ bằng phím 1/2/3 + Tab đổi đích, ngoài kéo thả.
- Click trái ô ruộng: đi tới gần 2 ô rồi mới hiện bảng.

## Quyết định
User, 2026-10-23. **Chỉ PC (chuột + bàn phím).** Điện thoại và tay cầm giữ joystick + nhắm theo tâm như cũ.

### 1. Di chuyển
- **Chuột trái lên world = đi tới đó**, tầm click vô hạn. Đích và hướng đi **quy về cùng độ cao với chỗ nhân vật đang đứng** (chỉ lấy x, z); đi **thẳng** theo hướng đó tới khi tới đích hoặc không đi tiếp được (tự nhảy bậc 1 ô như hiện nay). Đang đi mà click chỗ khác → đổi hướng ngay. (đề xuất Planner) Giữ chuột trái → đích bám theo con trỏ.
- **Dấu click:** hiệu ứng tức thời tại **đúng điểm đã click** (cả khi cao/thấp hơn nhân vật), mờ dần [số tạm] 0,5 giây.
- **Vòng dưới chân:** vòng tròn mờ dưới chân nhân vật, có mũi tên chỉ hướng đang đi (mũi tên ẩn khi đứng yên (đề xuất Planner)).
- **Nhảy = A.** Đang đi mà nhảy → vẫn bay về phía đích trên không.
- **Lướt = Shift** [số tạm]: lao 4 ô theo hướng đang đi (đứng yên thì theo hướng nhân vật quay mặt) trong 0,15 giây, dừng khi chạm khối, hồi 3 giây, không tốn mana, dùng được trên không 1 lần mỗi lần rời đất; lướt xong tiếp tục đi về đích đang có.
- **WASD không còn để đi.**

### 2. Camera
- **Mặc định:** nhìn chéo từ trên cao như góc mặc định của LoL, luôn bám nhân vật (không kéo màn hình theo mép). Góc cúi, khoảng cách mặc định [số tạm] 55°, 14 ô — chỉnh trong Cài đặt (0032).
- **Giữ chuột giữa + di chuột:** xoay camera (ngang + dọc). Lăn chuột: thu phóng.
- **Space:** camera về mặc định — sau lưng, trên đầu nhân vật, theo hướng nhân vật đang nhìn.
- **X:** bật/tắt góc nhìn thứ nhất. Camera ở trước mắt nhân vật, gắn theo hướng nhân vật, không xoay tự do được; click trái vẫn để đi.

### 3. Chế độ hành động và chuột phải
- **Chuột phải** = thao tác chọn: làm hành động của chế độ đang bật, dùng vật đang cầm, hoặc hành động riêng của chỗ được click.
- **Chế độ** (bấm lại phím đó để tắt, về mặc định):

  | Chế độ | Phím | Chuột phải |
  |---|---|---|
  | Mặc định | D | Hành động riêng của chỗ click (thu hoạch, mở kho, nhặt) |
  | Đào | Q | Đào khối |
  | Xới | E | Lên ô chưa có biển: đánh dấu xới. Lên biển: tự đi tới xới (Planner). Bỏ đánh dấu: click trái lên biển → đi tới 2 ô → bảng có nút **Bỏ đánh dấu** (user đồng ý 2026-10-23) |
  | Tưới (phép Mưa) | R | Phép Mưa lên ô ruộng / ô đất |

  Đầu game ở chế độ mặc định, chuột tự do.
- **Tô khối đang chọn:** trong chế độ, khối dưới con trỏ (trong tầm thao tác) phủ một **lớp đỏ nhạt**. Bỏ hộp viền trắng chạy theo tâm nhắm.
- **Tầm (thay 0031):**
  - **Tầm thao tác = 10 ô:** chỗ chuột phải click được.
  - **Tầm thực hiện = 2 ô:** nhân vật tự đi tới gần chỗ đã click trong 2 ô rồi mới làm (đào, đặt, gieo, xới, tưới, thu hoạch, mở kho, đặt camera, xem bảng ô ruộng).
  - Đo như cũ: Euclid từ tâm thân tới tâm ô. Tầm đào 2 ô nâng cấp được sau → [ideas](../ideas.md).
  - Đang có chế độ hoặc vật cầm mà con trỏ ra ngoài tầm thao tác → **vòng tầm thao tác** (bán kính 10) hiện mờ dưới đất, không tô khối.
  - Đi tới đích bị kẹt → huỷ, báo `Không tới được` (đề xuất Planner).
- **Thu = F:** bấm là hút ngay mọi vật rơi trong **2 ô**.
- **Click trái ô ruộng:** tính là xem thông tin — nhân vật đi tới gần 2 ô rồi mới hiện bảng ô ruộng. Ở chế độ Đào mà chuột phải ô ruộng thì ô ruộng bị đào (như luật hiện có).
- **Thứ tự ưu tiên của chuột phải** (cao → thấp):
  1. Dùng vật đang cầm, nếu chỗ click là đích hợp lệ của vật đó.
  2. Hành động riêng của chỗ click (thu hoạch cây chín, mở kho, nhặt vật rơi).
  3. Đặt khối đang cầm — luôn thấp nhất, để không đè mất ô ruộng hoặc cây.

  Ví dụ: cầm hạt, chuột phải lên ô có cây chín → thu hoạch.

### 4. Cầm vật phẩm
- Đặt khối, gieo hạt, đặt camera quan sát = **dùng vật phẩm trong túi**. Chuột phải lên ô trong túi (hoặc phím của ô túi phụ) → túi đóng, nhân vật cầm vật đó → chuột phải lên chỗ muốn dùng.
- **Khối:** hiện **bản mờ** ở ô đặt được; con trỏ ở chỗ không đặt được → bản mờ đỏ. Hết khối → tự thoát (không hiện đỏ vì hết).
- **Hạt:** chuột phải lên ô ruộng đã xới → đi tới, gieo. **Chỉ gieo tay được ô ruộng không có hạt nào trong hàng đợi.** Bảng chọn giống + hàng đợi của ô ruộng giữ nguyên.
- **Camera quan sát:** vật phẩm "Camera quan sát" ×1 trong túi, không mất khi dùng; chuột phải chỗ đặt → đặt camera trên ô đó [số tạm] 2 ô, hướng ngang theo camera chính lúc đặt. M1 tối đa 1, đặt lại → dời chỗ (0029). Xem / quay về: V như brief 0053. Gộp phần còn lại của world 7j vào task này.
- Thoát cầm: D, Esc, hoặc bấm phím chế độ khác.

### 5. Máu và mana (năng lượng)
- **Mana** là thanh năng lượng của nhân vật.
  - Đào, đặt khối, phép Mưa tốn mana; hành động khác không tốn.
  - Mana hồi dần **khi di chuyển**.
  - Không đủ mana → không làm, thanh mana nháy.
  - [số tạm] Tối đa 100; đào 5, đặt 3, Mưa 15; hồi 2 mana mỗi ô đi được.
- **Máu** mất khi:
  - rơi từ chỗ quá cao: [số tạm] rơi > 4 ô → mất 10 máu mỗi ô vượt;
  - **nước ngập đầu**: mất dần [số tạm] 5 máu/giây; chưa ngập đầu thì không mất.
- (đề xuất Planner) Tối đa 100. Hồi 1 máu/giây khi 5 giây không mất máu và không ngập đầu. Máu về 0 → hiện lại ở chỗ xuất phát, đầy máu + mana, không mất đồ.

### 6. Túi, túi phụ, kho
- **Túi:**
  - **có giới hạn**, ban đầu **10 ô**;
  - lưới ô vuông chia **trang** (không cuộn), qua trang cuối thì quay về trang 1;
  - mỗi loại vật phẩm một ô: hình vật phẩm + số lượng ở góc dưới-phải, tối đa **999/ô**, quá thì sang ô mới;
  - [số tạm] 10 ô mỗi trang.
- **Túi phụ:**
  - thuộc túi, ban đầu **2 ô**, có phân trang riêng; chuyển trang túi không ảnh hưởng túi phụ;
  - nằm cố định giữa-dưới màn hình; **P** bật/tắt hiện;
  - mở túi thì túi phụ luôn hiện cùng, không bị che;
  - phím **1–0** dùng ô túi phụ tương ứng (cầm vật đó).
- **Kho:** chuột phải khối kho (trong tầm thao tác) → đi tới 2 ô → mở. Bảng kho cũng là lưới ô. Kho vẫn chỉ nhận hạt + nông sản (0028). (đề xuất Planner) Nhân vật đi xa kho > 10 ô → bảng kho tự đóng. Bỏ phím K.
- Mở được cùng lúc túi + túi phụ + kho.
- **Chuyển đồ** (đồ chuyển hẳn sang nơi khác):
  - Kéo thả một ô sang nơi khác: chuyển cả chồng.
  - Bằng phím: click trái chọn một ô → **đích tự chọn theo ưu tiên túi > túi phụ > kho** (không phải nơi đang chứa ô đó; chỉ trong các nơi đang mở) → **Tab** đổi đích (chỉ có tác dụng khi có hơn một đích) → **1** chuyển toàn bộ, **2** một nửa, **3** một phần ba.
  - Làm tròn: nửa sau [số tạm, Planner] làm tròn lên, tối thiểu 1.
  - Trong lúc túi/kho mở, phím 1–3 dùng để chuyển, không dùng ô túi phụ.
- **Thông tin vật phẩm:** click trái một ô trong túi / túi phụ / kho → bảng ở **lề trái** màn hình: tên, cách kiếm được, số lượng trong túi (gồm túi phụ), số lượng trong kho.
- Túi đầy → vật phẩm không vào được thì nằm lại (vật rơi) (đề xuất Planner).

### 7. HUD và menu
- **Bỏ khỏi màn hình:**
  - tâm nhắm, hộp viền trắng;
  - dòng chữ hướng dẫn phím (chuyển vào Cài đặt, mục Hướng dẫn);
  - loài đang ghim, biểu tượng đình trệ;
  - nút và dòng "Đang ở buổi: …";
  - nhãn chữ trên đầu sinh vật.
- **Còn lại:**
  - **góc trên-phải:** minimap;
  - **góc trên-trái:** avatar + thanh máu + thanh mana;
  - **trên cùng:** thông số (FPS…);
  - **giữa-dưới:** túi phụ.
- **Ô ruộng:** phía trên mỗi ô ruộng có **icon** (nông sản / giai đoạn) với **vòng tiến trình** bao quanh. Chi tiết xem bằng click trái.
- **Sinh vật:** trạng thái hiện bằng icon nhỏ trên đầu thay cho chữ.
- **Menu phép** [phím G, Planner] chứa các chức năng dạng phép:
  - Đổi buổi: chỉ hiện icon của buổi hiện tại, chọn mở vòng 4 mốc như cũ (0024);
  - phép Mưa.

  Bỏ phím T.

### 8. Đổi phím ở M1
- **Mọi phím** trong bảng trên đổi được trong Cài đặt → **Điều khiển**, gồm cả nút chuột.
- Giữ luật đã chốt ở 0033 mục 2: lưu trong `settings.json`, có nút "Mặc định", trùng trong cùng ngữ cảnh → hỏi đổi chỗ, Esc cố định, phím debug không vào bảng.
- **Chuyển từ M3 về M1.**

## Hệ quả
- **Pillar:** không thêm cơ chế chơi mới. Phục vụ ràng buộc nền tảng PC: điều khiển dễ, màn hình gọn cho vòng lặp 30 giây (P1 hồi sinh qua cải tạo đất, P2 nông trại). Máu/mana gắn P1 (đào/đặt có giá) (đề xuất Planner).
- **Thành phần:** chỉ client Unity.
  - Luật (tầm, mana, máu, túi ô vuông, chia chồng, ưu tiên chuột phải) ở `Game.Core`, số trong `design/data/*.json`.
  - Input/camera/UI ở client.
  - Cảm ứng không đổi.
- **Ước lượng (của AI, chưa xác minh):** M1 +14–20 ngày-người, chia 6 bước world 7l a–f ([world-tech](../04-tech/world-tech.md#kế-hoạch-prototype-m1)).

  | Bước | Nội dung | Ngày-người |
  |---|---|---|
  | a | Luật lõi | 2–3 |
  | b | Di chuyển + camera | 3–4 |
  | c | Chế độ + cầm vật phẩm | 2–3 |
  | d | Túi / túi phụ / kho ô vuông | 3–4 |
  | e | HUD + menu phép + icon ruộng/sinh vật | 2–3 |
  | f | Bảng đổi phím | 1,5–2,5 |

  - M3 "Điều khiển (phần M3)" 5–10 → 3,5–7,5 (đổi phím rời M3).
  - M1 107,5–177 → **121,5–197**, vượt quỹ thêm.
  - Cắt trước nếu cần: góc nhìn thứ nhất, lướt, kéo thả (giữ phím 1/2/3).
- **Kinh tế — đã trả lời (user, 2026-10-23: chọn (a)):** túi có giới hạn → ưu đãi nạp "túi đồ +10%" ([economy](../02-gameplay/economy.md)) giờ vượt mức tối đa người không nạp đạt được, trái 0004. Ba hướng:
  - (a) ngoại lệ như giới hạn sinh vật;
  - (b) bỏ ưu đãi này;
  - (c) chỉ mở ô sớm hơn, mức tối đa như nhau.

  → **(a): túi đồ +10% là ngoại lệ thứ hai của 0004**, như giới hạn sinh vật 54 → 60: người nạp có thêm 10% số ô túi kể cả khi cả hai đã mở hết. Không ảnh hưởng PvP. Cập nhật [economy](../02-gameplay/economy.md), 0004, glossary, `.ai/project.json`.
- **Cập nhật:**
  - [controls-ui](../02-gameplay/controls-ui.md), [mechanics](../02-gameplay/mechanics.md#nhân-vật-túi-đồ-tự-trồng-trọt--luật-m1), [glossary](../glossary.md), [world-tech](../04-tech/world-tech.md) 7j + 7l, [architecture](../04-tech/architecture.md), [roadmap](../05-roadmap.md), [ideas](../ideas.md);
  - 0004 / 0031 / 0033 (ghi chú), `decisions/README.md`, ràng buộc balance trong `.ai/project.json`.
- **Task contract:** `.ai/tasks/m1-world-7l-dieu-khien-pc.md` (gộp phần còn lại của 7j, brief 0053).
