# Kỹ thuật mô phỏng nông trại

> Trạng thái: Nháp
> Liên quan: [Kỹ thuật tạo world](world-tech.md) · [Kiến trúc](architecture.md) · [Cơ chế](../02-gameplay/mechanics.md) · [Rủi ro](risks.md)

Phần kỹ thuật nặng thứ hai của M1 (sau world): **lệnh làm việc**, **tìm đường sinh vật**, **tua nhanh**. Luật chơi ở [cơ chế](../02-gameplay/mechanics.md#lệnh-làm-việc) — file này chỉ nói cách làm. Toàn bộ là đề xuất của AI [ý tưởng] (2026-10-13), trừ chỗ ghi nguồn.

## Nguyên tắc
- **Một mô hình mô phỏng, hai cách hiển thị** ([cơ chế](../02-gameplay/mechanics.md#tua-nhanh)): khi đang chơi, mô hình chạy theo nhịp và phần hiển thị vẽ sinh vật đi lại; khi tua nhanh, chỉ chạy mô hình, không vẽ.
- **C# thuần, không phụ thuộc scene** ([kiến trúc](architecture.md#client-unity)) → chạy được trong test tự động và trong tua nhanh.
- **Tất định:** cùng trạng thái + cùng thời gian → cùng kết quả. Không dùng thời gian khung hình, không dùng `UnityEngine.Random` trong mô phỏng — dùng bộ sinh số ngẫu nhiên có seed riêng của mô phỏng.
- **Nhịp cố định:** mô phỏng chạy theo nhịp cố định (vd 10 nhịp/giây), tách khỏi tốc độ khung hình; phần hiển thị nội suy vị trí giữa hai nhịp.

## Dữ liệu
| Thực thể | Trường chính |
|---|---|
| Lệnh làm việc | Vùng (tập ô), hàng đợi (vd loại cây), danh sách công đoạn, trạng thái, độ ưu tiên (M2) |
| Công đoạn | Loại = **năng lực làm việc** cần (vd tưới nước), ô đích, thời gian gốc, vật tư cần, **tối đa 2 sinh vật đang làm** (user, 2026-10-13), tiến độ (giữ khi bị gián đoạn) |
| Sinh vật (phần làm việc) | 2 năng lực làm việc, độ thuần thục ×2, tốc độ di chuyển, sức khoẻ, **độ bền làm việc** (giảm khi làm, hồi khi nghỉ — user chấp nhận 2026-10-13), mức bệnh/mệt, trạng thái (rảnh / đang đi / đang làm / nghỉ / bệnh / mệt), khung chiếm chỗ, kiểu di chuyển |
| Kho | Vị trí, sức chứa, tồn kho |
| Đình trệ | Công đoạn, lý do (không có đường / thiếu vật tư / kho đầy / không có loài có năng lực), vị trí |

Chỉ số dùng ở đây đọc từ [chỉ số làm việc](../02-gameplay/mechanics.md#chỉ-số-sinh-vật); con số cân bằng bằng skill `game-balance`.

## Một nhịp mô phỏng
```mermaid
flowchart TB
    T[Nhịp mới] --> A[Gom công đoạn đang thiếu người]
    A --> B[Ghép với sinh vật rảnh<br/>có năng lực khớp]
    B --> C{Tới được ô đích?<br/>tra vùng liên thông}
    C -- không --> D[Đình trệ + thông báo]
    C -- có --> P[Xin đường đi A*<br/>trong ngân sách nhịp]
    P --> M[Sinh vật đi theo đường]
    M --> W[Làm công đoạn<br/>thời gian theo thuần thục, nội tại, mệt/bệnh]
    W --> N[Xong → công đoạn sau / vào kho]
    N --> K{Kho đầy?}
    K -- có --> D
    K -- không --> T
```

**Ghép việc:**
- Mỗi nhịp, mỗi công đoạn thiếu người chọn sinh vật rảnh có năng lực khớp, điểm = (độ dài đường ước tính) chia cho (độ thuần thục). Sinh vật gần và giỏi được chọn trước.
- Ước tính bằng khoảng cách trên lưới (rẻ); chỉ chạy A\* thật cho cặp đã chọn.
- Giữ tiến độ khi sinh vật bị lấy đi giữa chừng (nhập hồn, chuyển lệnh) — đúng trường hợp biên trong cơ chế.

**Người chơi làm công đoạn** (user, 2026-10-13): hành động của người chơi trên ô của lệnh (xới, gieo, tưới, thu hoạch) gửi vào mô phỏng như một sự kiện "công đoạn xong tại ô" ở nhịp kế tiếp; công đoạn đó không còn thiếu người → ghép việc bỏ qua. [ý tưởng]

**Thời gian một công đoạn:** `thời gian gốc ÷ hệ số thuần thục × hệ số nội tại × hệ số mệt/bệnh × hệ số người làm`; hệ số người làm = 1 khi một sinh vật; khi 2 sinh vật cùng làm, con thứ hai giúp ít hơn con đầu (vd × 0,6 thay vì × 0,5 — user chấp nhận 2026-10-13). Các hệ số là bảng dữ liệu, tính bằng `game-balance`.

**Nghỉ:** độ bền làm việc về 0 → sinh vật về trạng thái nghỉ (tại chỗ hoặc về nơi ở — [ý tưởng]) cho tới khi hồi đủ, rồi mới nhận việc lại. Bệnh/mệt theo mức độ: làm chậm → nghỉ nhiều hơn → dừng (user, 2026-10-13).

## Tìm đường
- **A\* trên lưới ô** của [world-tech](world-tech.md#tìm-đường-trên-lưới): ô đi được theo khung chiếm chỗ (rộng w, cao h), đi bộ leo/xuống 1 ô.
- **Vùng liên thông:** chia các ô đi được thành vùng (mỗi kích thước khung có bản riêng). Hai ô khác vùng → biết ngay "không tới được" → **đình trệ ngay**, không tốn A\*.
  - Đào/đặt ô → chỉ tính lại vùng quanh chỗ sửa (gộp hoặc tách vùng).
- **Ngân sách mỗi nhịp:** giới hạn số lần A\* mỗi nhịp; yêu cầu dư xếp hàng sang nhịp sau → không giật khung hình khi nhiều sinh vật cùng cần đường.
- **Đường bị cắt** (người chơi đào giữa đường) → sinh vật dừng ở ô hiện tại, xin đường mới.
- Bản đồ lớn hơn hoặc nhiều sinh vật hơn mà A\* chậm → cân nhắc tìm đường phân cấp (chia chunk thành cụm, tìm trên cụm trước) [chưa xác minh — đo ở M1 trước].
- Kiểu di chuyển bay, bơi: M2 — thêm luật ô đi được (bay: bỏ yêu cầu đất dưới chân; bơi: ô có nước).

## Tua nhanh
Mô hình **nhảy sự kiện** thay vì chạy từng nhịp:
1. Lấy thời gian vắng mặt (giờ server từ M2; giờ thiết bị ở M1).
2. Cho **nước** đạt trạng thái yên trước ([world-tech](world-tech.md#nước-chảy)).
3. Với mỗi cặp sinh vật–công đoạn: thời lượng = (độ dài đường ÷ tốc độ) + thời gian làm. Lấy sự kiện xong sớm nhất → cập nhật kho, công đoạn → ghép việc mới → lặp.
4. Dừng khi hết thời gian, hoặc mọi lệnh dừng vì giới hạn tự nhiên (kho đầy, hết hạt giống trong hàng đợi) — đúng ý "không cần giới hạn thời gian cứng" trong cơ chế.
5. Trả về: sản lượng, các đình trệ xảy ra, vị trí cuối của sinh vật.

- Độ dài đường dùng **độ dài A\* thật** (đã có trong bộ đệm đường hoặc tính lại một lần), không phải khoảng cách thẳng → khớp với lúc chơi.
- **Kiểm chứng:** test tự động chạy cùng một nông trại theo hai cách (nhịp từng nhịp vs nhảy sự kiện) trong cùng khoảng thời gian → sản lượng lệch không quá một ngưỡng (vd ≤ 2%) — rủi ro "kết quả tua nhanh lệch lúc chơi" trong [risks](risks.md).
- Mệt/bệnh (nhu cầu sinh vật) tính theo thời gian trung bình của khoảng vắng mặt — chi tiết khi cơ chế bệnh/mệt có luật ([cơ chế](../02-gameplay/mechanics.md#nhu-cầu-sinh-vật)).

## Hiển thị
- Phần hiển thị chỉ **đọc** trạng thái mô phỏng: vị trí (nội suy giữa nhịp), trạng thái → animation (đi, làm, nghỉ), biểu tượng đình trệ tại chỗ.
- Sinh vật ở xa camera: không cần vẽ animation chi tiết; mô hình vẫn chạy.

## Hiệu năng
- Số sinh vật hoạt động tối đa = **giới hạn sinh vật** (theo cấp độ người chơi, công trình — [cơ chế](../02-gameplay/mechanics.md#giới-hạn-sinh-vật--kho-sinh-vật)). Khi mở hết: **54** (không nạp), **60** (có ưu đãi nạp) (user, 2026-10-13) → M1 đo với 60 sinh vật trên máy mobile tầm trung.
- Đo ở M1: ms mỗi nhịp mô phỏng, số A\* mỗi giây, thời gian tua nhanh 8 giờ vắng mặt.

## Kế hoạch prototype (M1)
Chia các hạng mục "Lệnh làm việc 20–30", "Tìm đường 10–20", "Chỉ số làm việc 5–8" của [roadmap](../05-roadmap.md):

| # | Bước | Xong khi | Ước lượng (ngày-người) |
|---|---|---|---|
| 1 | Lõi mô phỏng: nhịp cố định, seed, dữ liệu sinh vật / lệnh / công đoạn / kho | Test: cùng đầu vào → cùng kết quả | 3–5 |
| 2 | A\* theo khung chiếm chỗ + vùng liên thông + cập nhật khi đào/đặt | Rãnh 1 × 2 đi được; tách vùng → đình trệ ngay | 6–10 |
| 3 | Lệnh trồng cây 4 công đoạn + ghép việc theo năng lực, thuần thục | 3 loài, mỗi loài 2 năng lực, làm xong một vụ | 6–9 |
| 4 | Đình trệ + thông báo (vị trí, lý do) | 4 lý do đều hiện đúng | 2–4 |
| 5 | Chỉ số làm việc (tốc độ, thuần thục) vào thời gian công đoạn | Đổi thuần thục → thời gian đổi đúng công thức | 2–3 |
| 6 | Hiển thị: nội suy vị trí, animation trạng thái | Sinh vật đi mượt ở 30 và 60 FPS | 3–5 |
| 7 | Tua nhanh nhảy sự kiện + test so với chạy từng nhịp | Lệch ≤ 2% sản lượng sau 8 giờ giả lập | 4–7 |
| 8 | Đo trên mobile (60 sinh vật) | Có số ms/nhịp, A\*/giây | 1–2 |
| | **Cộng** | | **27–45** (trong 35–58 của ba hạng mục) |

## Câu hỏi mở
- Ngưỡng các mức bệnh/mệt; nghỉ tại chỗ hay về nơi ở?
- Đã trả lời (user, 2026-10-13): tối đa 54 sinh vật hoạt động, 60 nếu nạp; độ bền làm việc và con thứ hai giúp ít hơn con đầu — chấp nhận; bệnh/mệt theo mức độ (chậm → nghỉ nhiều → dừng), sinh vật khoẻ cũng cần nghỉ; tối đa 2 sinh vật cùng làm một công đoạn, giảm thời gian.
