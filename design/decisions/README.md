# Quyết định thiết kế

> Trạng thái: Chốt

Mỗi quyết định quan trọng là một file `NNNN-ten-ngan.md` theo [_template.md](_template.md). Quyết định đã Chốt không sửa nội dung — muốn đổi thì tạo quyết định mới thay thế (ghi "Thay thế: NNNN").

| # | Quyết định | Trạng thái |
|---|---|---|
| [0001](0001-client-unity.md) | Client dùng Unity (chỉ client) | Chốt |
| [0002](0002-repo-layout.md) | Thiết kế trước, cùng repo với các thành phần | Chốt |
| [0003](0003-server.md) | Cần Server: backend và game server (PvP thời gian thực) | Chốt |
| [0004](0004-tien-nap.md) | Tiền nạp: ảnh hưởng tốc độ, chặn ở mức tối đa | Chốt |
| [0005](0005-render-pipeline.md) | Render pipeline của client: URP | Chốt |
| [0006](0006-cong-cu-kiem-tra-asset.md) | Công cụ kiểm tra asset (ngoại lệ M0) | Chốt |
| [0007](0007-m1-toi-gian.md) | M1 tối giản (áp dụng C1) | Chốt (map nhỏ cố định thay bởi world 3 map ở 0022) |
| [0008](0008-sua-loi-sau-ra-soat-m0.md) | Sửa lời văn trong file đã Chốt sau rà soát M0 | Chốt |
| [0009](0009-dai-duong-va-suong-mu-o-ria-map.md) | Map luôn có đại dương bao quanh, rìa xa là tường sương mù | Chốt ("mọi map" → "cả world" ở 0022) |
| [0010](0010-mcp-for-unity.md) | MCP for Unity: AI làm việc trong Unity Editor đang mở (công cụ phát triển) | Chốt |
| [0011](0011-dia-hinh-khoi-bo-canh.md) | Địa hình hiển thị dạng khối bo cạnh, mép cỏ phủ (thay bề mặt mịn liền) | Chốt |
| [0012](0012-bien-tran-va-luat-dat-khoi-m1.md) | Biển tràn theo mực nước + luật đặt khối mới (M1) | Chốt (chữ "loang ngay" thay bởi 0013) |
| [0013](0013-nuoc-lan-theo-nhip.md) | Nước biển lan theo nhịp (thay "loang ngay" của 0012) | Chốt |
| [0014](0014-ghep-viec-o-sim-buoc-1.md) | Ghép việc + đi theo khoảng cách làm ở simulation bước 1; người chơi nhận việc để M2 | Chốt |
| [0015](0015-tim-duong-khung-va-dao-duoi-chan.md) | Tìm đường M1: khung chiếm chỗ 3 chiều, không đi vào nước, đào dưới chân thì sinh vật chạy ra | Chốt |
| [0016](0016-o-ruong-kho-nha-va-sinh-vat-ket.md) | Ô ruộng có hàng đợi, đứng ô kề để làm, kho là công trình; nhà sinh vật + sinh vật bị kẹt ở simulation bước 4 | Chốt (định nghĩa "kẹt" làm rõ ở 0017; định nghĩa "ô ruộng" thay ở 0020) |
| [0017](0017-dinh-tre-thong-bao-nha-mau-va-dem-ket.md) | Simulation bước 4: thông báo là sự kiện trong lõi, thiếu năng lực, nhà mẫu khối đặc, chỉ đếm kẹt khi rảnh | Chốt |
| [0018](0018-thuan-thuc-bang-muc.md) | Simulation bước 5: thuần thục vào thời gian công đoạn qua bảng mức, áp theo tốc độ mỗi nhịp | Chốt |
| [0019](0019-chan-nhau-ranh-quanh-nha-khong-dung-tren-ruong.md) | Simulation bước 6b: sinh vật chặn nhau khi đi, rảnh về quanh nhà + lang thang (kéo từ M2), không dừng trên ô ruộng | Chốt |
| [0020](0020-o-ruong-tao-khi-xoi-va-huy-vu.md) | Simulation bước 6a (sau 6b — 0021): ô ruộng chỉ có sau khi xới; đặt khối lên / đào ô ruộng → huỷ vụ, trả hạt; phép tưới nước (ô đất → cỏ) giữ M2 | Chốt |
| [0021](0021-lam-6b-truoc-6a.md) | Làm simulation bước 6b trước 6a; thêm test map M1: 0 lần đứng chung ô, 0 lần rảnh dừng trên ô ruộng | Chốt |
| [0022](0022-world-nhieu-map-va-minimap-m1.md) | World liền một khối, 3 map giáp nhau bằng ranh giới cong; minimap phóng to được (lai: nền từ bộ sinh + sửa theo đào/đặt); nạp dần chunk ngay M1; làm trước sim 6a | Chốt ("dấu mốc" làm rõ ở 0023) |
| [0023](0023-dau-moc-cong-trinh-checkpoint-va-dan-duong.md) | Dấu mốc trên bản đồ = công trình đặc trưng của từng map (khối giữ chỗ do bộ sinh đặt) + 1 checkpoint mỗi map (M1, bước 7d); dịch chuyển tới checkpoint + đường chỉ dẫn đi bộ để M2 | Chốt |
| [0024](0024-buoi-trong-ngay-va-chon-thoi-diem.md) | Buổi trong ngày: chu kỳ 24 giờ thực, chọn 4 mốc (đổi buổi, không đổi đồng hồ mô phỏng), điều kiện môi trường sống theo buổi; thêm vào M1 | Chốt |
| [0025](0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md) | M1 sim bước 7b + 7c (trước bước 8): công cụ Xới đánh dấu từng ô + biển canh tác, sinh vật xới thành ô ruộng; bấm ô ruộng chọn giống (2 loại cây), vụ kế xới lại; bỏ lệnh trồng cây theo vùng; các kho thông nhau, giao về kho gần nhất, nút Kho | Chốt |
| [0026](0026-hang-doi-o-doi-theo-loai.md) | Hàng đợi vụ = ô đợi theo loại cây × số lượng (ô ruộng cấp 1: 2 ô × 3); nút +/− trên từng ô đợi; không gộp; M1 sim bước 7d | Chốt |
