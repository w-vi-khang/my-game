# Thuật ngữ

> Trạng thái: Nháp

<!-- Mỗi thuật ngữ của game (tên tài nguyên, cơ chế, địa danh...) một dòng. Dùng đúng tên này trong mọi tài liệu. -->

| Thuật ngữ | Nghĩa | Định nghĩa ở |
|---|---|---|
| Pillar | Trụ cột trải nghiệm của game | [00-vision.md](00-vision.md) |
| Core loop | Vòng lặp hành động cốt lõi | [02-gameplay/core-loop.md](02-gameplay/core-loop.md) |
| Thành phần | Một phần độc lập của hệ thống game (client, server, công cụ, dịch vụ...) | [04-tech/components.md](04-tech/components.md) |
| Client | Thành phần người chơi chạy, làm bằng Unity | [04-tech/components.md](04-tech/components.md) |
| Sinh vật | Sinh vật phép thuật người chơi sưu tầm; làm việc và chiến đấu | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Loài | Một dạng sinh vật; mỗi loài có nội tại, tộc, nguyên tố, yêu cầu môi trường sống riêng | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Tộc | Nhóm gồm nhiều loài | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Nguyên tố | Thuộc tính của loài, đơn hoặc đôi | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Nội tại | Đặc điểm cố hữu của loài, tác dụng cả khi làm việc lẫn chiến đấu | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Nhập hồn | Người chơi nhập vào sinh vật, dùng bộ kỹ năng của sinh vật đó | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Mana | Nguồn năng lượng chung của người chơi, dùng cả khi nhập hồn | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Môi trường sống | Thế giới được cải tạo theo danh sách yêu cầu của loài để loài trở lại | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Danh sách yêu cầu của loài | Danh sách yêu cầu mỗi loài (user gọi là "list nhiệm vụ theo loài", "bảng nhiệm vụ loài"); quyết định cả cấp độ sinh vật ngoài tự nhiên: đạt phần mở khoá → loài trở lại; đạt thêm → mức đáp ứng tăng. Khác "nhiệm vụ" (cốt truyện) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Rút thưởng gói | Rút phần thưởng ngẫu nhiên từ gói trong cửa hàng (gói sinh vật, gói sách phép, gói trang bị...) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Bảo hiểm | Đảm bảo ra phần thưởng được bảo hiểm khi rút gói đủ nhiều lần (gói sinh vật: cá thể vượt trội; gói sách phép: phép bậc cao hơn; gói trang bị: trang bị bậc cao hơn); công khai; ưu tiên hơn chặn quá may | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Trang bị | Đồ gắn cho sinh vật (mỗi sinh vật 3 trang bị, không chia loại ô), cộng chỉ số chiến đấu, tính trong PvP; không ngẫu nhiên, không vượt trội | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Bậc trang bị | Mức của trang bị; khác "bậc" (tiến hoá) của sinh vật | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Ghép trang bị | Ghép các trang bị cùng loại, cùng bậc trang bị thành một trang bị bậc cao hơn | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Rèn | Làm trang bị ở lò rèn từ nguyên liệu, cần sinh vật phù hợp | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Ma năng | Tài nguyên nhận khi rút gói sách phép ra phép đã sở hữu; đổi phép trong cửa hàng hoặc chế tác một số trang bị. Khác "mana" (năng lượng dùng phép) — tên dễ nhầm, xem [naming](01-story/naming.md) | [02-gameplay/economy.md](02-gameplay/economy.md) |
| Biến thể hiển thị | Khác biệt nhỏ về hình ảnh giữa mọi cá thể/vật thể cùng loại (kích thước, màu, hoa văn), sinh theo seed riêng |
| Biến thể | Dạng khác của một loài: cá thể đặc biệt về màu sắc, hình thái, hiếm; tách riêng với vượt trội | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Thẻ bài | Mỗi cá thể gắn một thẻ bài; thẻ là thứ cất trong kho sinh vật; ném thẻ vào sinh vật để bắt | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Ném thẻ | Thao tác bắt: ném thẻ bài vào sinh vật ngoài tự nhiên. Ném hụt: tỉ lệ nhỏ mất thẻ; trúng nhưng bắt không thành: mất thẻ | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Bậc thẻ bài | Chất lượng thẻ bài (1–4): quyết định tỉ lệ bắt và tỉ lệ vượt trội; bậc 4 chắc chắn bắt, chắc chắn vượt trội, có hiệu ứng. Khác "bậc" của sinh vật và "bậc trang bị" | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Tỉ lệ bắt | Xác suất bắt thành công: theo chênh lệch cấp độ người chơi – sinh vật, bậc sinh vật, bậc thẻ bài | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Hiệu ứng thẻ bài | Hiệu ứng hình ảnh (khi xuất hiện, hào quang, mây...) — chỉ thẻ bậc 4 | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Lò rèn | Công trình để rèn trang bị | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Công cụ kiểm tra asset | Script Blender headless kiểm model 3D theo quy cách trước khi đưa vào client (`tools/asset_check/`) | [decisions/0006](decisions/0006-cong-cu-kiem-tra-asset.md) |
| Khung chiếm chỗ | Tập ô voxel một sinh vật/cây/vật phẩm chiếm trên lưới; chỉ biểu thị không gian, không quyết định hình dạng hiển thị | [03-art-audio/art-style.md](03-art-audio/art-style.md) |
| Mốc bảo hiểm | Số lần rút liên tiếp tối đa trước khi chắc chắn ra phần thưởng hiếm của gói (ý tưởng cách làm) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Lệnh làm việc | Việc người chơi đặt cho sinh vật trên một vùng (vd trồng cây), gồm nhiều công đoạn. Khác với "nhiệm vụ" (cốt truyện) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Công đoạn | Một bước của lệnh làm việc (xới đất, gieo hạt, tưới nước, thu hoạch, vận chuyển...) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Đình trệ | Công đoạn không làm được (không có đường, thiếu vật tư, kho đầy) — báo cho người chơi | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Tua nhanh | Tính sản lượng cho thời gian vắng mặt khi người chơi vào lại, không chạy animation | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Nhiệm vụ | Nhiệm vụ cốt truyện (chính hoặc phụ) trong một map | [01-story/plot.md](01-story/plot.md) |
| Tinh linh | Đồng hành luôn đi theo người chơi; mảnh linh hồn của sinh vật từng thân thiết với pháp sư, ở lại để cảm hoá; hỗ trợ bằng thông tin, nâng cấp bằng mảnh ký ức (tên chưa có) |
| NPC | Nhân vật không do người chơi điều khiển — trong game này không phải người: sinh vật đặc biệt, máy móc, pho tượng | [01-story/characters.md](01-story/characters.md) | [01-story/characters.md](01-story/characters.md) |
| Bản ngã cũ / bản ngã mới | Pháp sư trước khi mất ký ức (coi sinh vật là công cụ) / sau khi mất ký ức (người chơi điều khiển); đối đầu ở trận chiến cuối | [01-story/characters.md](01-story/characters.md) |
| Biến cố | Sự kiện bản ngã cũ rút sinh khí của thế giới làm sinh vật thất lạc, pháp sư mất ký ức (tên chưa có) | [01-story/world.md](01-story/world.md) |
| Sinh khí | Năng lượng sống chảy giữa đất đai và sinh vật — lý do trong truyện của môi trường sống (ý tưởng) | [01-story/world.md](01-story/world.md) |
| Mảnh ký ức | Mảnh quá khứ của nhân vật chính; mạch truyện chính xuyên mọi map | [01-story/plot.md](01-story/plot.md) |
| Map | Vùng có ranh giới trên world của người chơi, có câu chuyện riêng | [01-story/world.md](01-story/world.md) |
| Seed | Giá trị riêng của mỗi người chơi để sinh phần không thuộc cốt truyện của map | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Khối cốt truyện | Khối thuộc cốt truyện hoặc đường đi tới nó; không phá/đặt được cho tới khi phá đảo map | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Phá đảo map | Hoàn thành cốt truyện chính của một map | [02-gameplay/progression.md](02-gameplay/progression.md) |
| Đảo sandbox | Vùng cô lập cho co-op (spin-off, tạm bỏ) | [ideas.md](ideas.md) |
| Phép | Phép thuật; gồm phép của pháp sư và phép sinh vật — hai bộ sưu tầm riêng | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Phép của pháp sư | Phép phục vụ khám phá, phát triển, tấn công đơn giản | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Nhóm phép | Phạm vi được mang phép: phổ biến, tộc, nguyên tố, loài | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Độ hiếm | Mức hiếm của phép, mô tả tỉ lệ xuất hiện | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Cấp độ | Mức phát triển của một sinh vật; phép có yêu cầu cấp độ | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Cấp độ người chơi | Mức phát triển của người chơi; điều kiện mở PvP (chi tiết chưa có) | [02-gameplay/progression.md](02-gameplay/progression.md) |
| Bậc | Mức tiến hoá của sinh vật; phép có yêu cầu bậc | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Tiến hoá | Tăng bậc sinh vật, mở rộng phạm vi phép mang được | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Chuẩn bị chiến đấu | Màn người chơi gán phép cho sinh vật trước khi chiến đấu | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Bắt / Ấp nở / Rút thưởng | Các cách nhận sinh vật mới | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Gói sách phép | Gói rút thưởng ra phép | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Nghiên cứu | Một nguồn phép mới (chi tiết chưa có) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Ghép trận | Ghép người chơi với nhau để chiến đấu (PvP), chạy ở backend | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Chế độ nhiều người | Các mode tương tác với người chơi khác, không ảnh hưởng tiến trình chính | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Backend | Dịch vụ phía server gọi theo yêu cầu: tài khoản, lưu dữ liệu, giờ chuẩn, rút thưởng, ghép trận | [04-tech/components.md](04-tech/components.md) |
| Game server | Server chạy mô phỏng trận đấu thời gian thực | [04-tech/components.md](04-tech/components.md) |
| PvE | Chiến đấu với nội dung của game, trong world riêng | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| PvP | Chiến đấu thời gian thực giữa người chơi, qua ghép trận; game server quyết định kết quả | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Phép sinh vật | Phép sinh vật mang trong ô phép để chiến đấu; pháp sư không mang được | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Ô phép | Chỗ gán phép trên sinh vật: 3 ô ở bậc 1, 4 ô từ bậc 2 | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Ô đặc biệt | Ô phép thứ 4; chứa được phép yêu cầu ô đặc biệt và phép thường | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Cá thể | Một sinh vật cụ thể; cùng loài thì chung đặc điểm loài, khác chỉ số cá thể | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Chỉ số cá thể | Phần chỉ số riêng của từng cá thể: phân bố ngẫu nhiên và phần vượt trội (nếu có) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Chỉ số làm việc | Bảng chỉ số dùng khi làm việc: tốc độ di chuyển, sức khoẻ, độ thuần thục 1–2 | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Chỉ số chiến đấu | Bảng chỉ số dùng khi chiến đấu: máu, giáp, kháng phép, tốc độ đánh, sát thương... + chỉ số đặc trưng loài | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Năng lực làm việc | Loại công đoạn một loài làm được; mỗi loài có 2 (vd vận chuyển, tưới nước) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Độ thuần thục | Chỉ số làm việc ứng với một năng lực làm việc; cao thì làm công đoạn đó hiệu quả hơn | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Sức khoẻ | Chỉ số làm việc; cao thì sinh vật ít bị bệnh hơn | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Bệnh | Trạng thái xấu của sinh vật; ít xảy ra khi mức đáp ứng nhu cầu cao và sức khoẻ cao (tác động chưa có) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Mệt | Trạng thái xấu của sinh vật; ít xảy ra khi mức đáp ứng nhu cầu cao (tác động chưa có) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Nhu cầu sinh vật | Phần danh sách yêu cầu của loài sau khi đã trở lại; đáp ứng càng nhiều sinh vật càng khoẻ, làm tốt, đôi khi tặng quà (tên là ý tưởng) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Mức đáp ứng | Mức nhu cầu sinh vật đã được đáp ứng (tên là ý tưởng) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Giới hạn sinh vật | Số sinh vật hoạt động (làm việc + chiến đấu) tối đa; tăng theo cấp độ người chơi và công trình | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Kho sinh vật | Nơi tạm cất sinh vật (dưới dạng thẻ bài); sinh vật trong kho không tính vào giới hạn sinh vật, không làm việc/chiến đấu. Khác "kho" chứa sản lượng | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Vật phẩm phân bổ lại | Vật phẩm dùng để phân bổ lại điểm chỉ số (tên là ý tưởng) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Túi đồ | Nơi chứa vật phẩm của người chơi; tiền nạp tăng 10% sức chứa | [02-gameplay/economy.md](02-gameplay/economy.md) |
| Hạng | Bậc xếp hạng PvP theo điểm xếp hạng; ghép trận theo hạng | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Cấp độ trung bình | Trung bình cấp độ các sinh vật mang vào trận PvP; tiêu chí ghép trận thứ hai trong cùng hạng | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Điểm chỉ số | Điểm nhận khi tăng cấp độ/bậc, người chơi phân bổ tự do; điểm làm việc và chiến đấu là hai quỹ riêng; phân bổ lại cần vật phẩm | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Base | Giá trị chỉ số ban đầu theo loài | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Phân bố loài | Phần điểm chỉ số cố định, đặc trưng cho loài; tạo khác biệt chính giữa các loài | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Phân bố ngẫu nhiên | Phần điểm chỉ số chia ngẫu nhiên khi cá thể sinh ra; cố định, tạo sự riêng biệt của cá thể | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Vượt trội | Phân hạng cá thể cao nhất: điểm cộng cá thể ≥ 20% base của loài; xác suất theo bậc thẻ bài (bậc 4 chắc chắn); bị bỏ trong PvP | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Điểm cộng cá thể | Điểm cộng trên base của loài (user gọi là "điểm vượt trội"), khác nhau giữa các cá thể; quyết định phân hạng cá thể; bị bỏ trong PvP | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Phân hạng cá thể | Hạng chất lượng của cá thể theo điểm cộng cá thể: 4 hạng (3 hạng thường + vượt trội); tên các hạng chưa có | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Hệ số quy đổi | Tỉ lệ đổi điểm chỉ số thành giá trị thật; giống nhau ở mọi loài | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Trần | Giá trị tối đa của một chỉ số (vd tốc độ đánh tối đa 6) |
| Chặn quá may | Cơ chế hạ tạm tỉ lệ vượt trội khi người chơi ra vượt trội nhiều bất thường; không tác động may mắn bình thường | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Chỉ số đặc trưng loài | Chỉ số chiến đấu cố định theo loài (vd tầm đánh, tỉ lệ chí mạng); điểm chỉ số không đổi được | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Tiền trong game | Tiền người chơi kiếm được khi chơi | [02-gameplay/economy.md](02-gameplay/economy.md) |
| Tiền nạp | Tiền mua bằng tiền thật; ảnh hưởng tốc độ tiến trình nhưng không vượt được mức tối đa người không nạp cũng đạt được | [02-gameplay/economy.md](02-gameplay/economy.md) |
| Thể thức | Kiểu trận PvP, quy định số sinh vật mỗi bên (3–5) | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Điểm xếp hạng | Điểm nhận khi thắng PvP, dùng để leo hạng | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
| Ngưỡng PvP | Mức người chơi phải đạt để chơi PvP. Trong PvP, điểm cấp độ/bậc vẫn tính, chỉ bỏ phần vượt trội | [02-gameplay/mechanics.md](02-gameplay/mechanics.md) |
