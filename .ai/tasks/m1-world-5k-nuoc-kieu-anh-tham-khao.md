# Task: M1 world 5k — nước kiểu ảnh tham khảo (màu theo độ sâu, bọt bờ, lấp lánh, vệt chảy)

> Model thực thi: Opus · high (Planner thiết kế cách làm + viết brief). Các brief Executor ghi model riêng theo AGENTS.md §10.2.
> Trạng thái: Đã xác nhận (user 2026-11-01). Phần 5 cắt sang M2.
> - Nguồn: user 2026-11-01, "tôi muốn nước giống như ảnh 3". Ảnh lưu ngoài repo: `~/Documents/User/khang/Game/art-test/shots/ref_water_style_5k.webp`. Ảnh là game của người khác, chưa có tên/nguồn → không ghi tên game.
> - Khớp mục "Hiển thị dòng chảy" [ý tưởng] ở [world-tech](../../design/04-tech/world-tech.md) (`:141`, dòng 5k `:200`).
> - **Pillar:** P1 "Hồi sinh để sưu tầm" (người chơi cải tạo thế giới, nước là thứ người chơi đào/dẫn; `design/00-vision.md:15`). **Thành phần:** client Unity (`BlockMesher` + `Ocean.shader` + biển nền).

## Mục tiêu
Nước trong game giống ảnh tham khảo 3: bãi cát nhìn từ trên, nước trong và mềm mại.
1. **Màu theo độ sâu + thấy đáy + nét mềm** (user thêm "nét mềm mại hơn", 2026-11-01).
   - Xanh ngọc nhạt sát bờ, đậm dần ra xa.
   - Chỗ nông trong, thấy đáy.
   - Viền chỗ nước chạm bờ mềm, không cắt gắt. Chuyển màu êm, không có đường cứng.
2. **Bọt bờ:** nhiều vệt bọt trắng song song với bờ, trôi vào/ra theo thời gian.
3. **Lấp lánh mạnh hơn:** đốm sáng mặt trời rõ hơn, thêm chấm lấp lánh nhỏ trên mặt nước.
4. **Vệt chảy:** vệt sáng trôi theo hướng chảy (ô lan, ô rơi). Mặt ao chỉ gợn tại chỗ.
5. ~~**Bọt chân thác + mép tràn.**~~ → **M2** (user cắt 2026-11-01), ghi ở [ideas](../../design/ideas.md) `:71`.

Áp cho **cả biển nền quanh đảo lẫn nước trong map** (ao, rãnh, thác), dùng chung một kiểu (user 2026-11-01).

## Ngoài phạm vi
- Sinh vật/cây/san hô dưới nước như trong ảnh: đó là nội dung map, không phải shader nước.
- Sóng làm biến dạng hình học mặt nước, phản chiếu thật (reflection probe/planar), khúc xạ màn hình.
- Âm thanh nước.
- Đổi luật mô phỏng nước (mức, lan, chảy): 5k chỉ đổi hiển thị.
- Mesh cột thác/mép tràn (đã xong ở 5h–5j), trừ phần ghi thêm dữ liệu vào đỉnh cho shader.
- Bọt chân thác + mép tràn (phần 5 cũ): M2.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Mỗi phần 1–4 có brief đã duyệt. Phần có dữ liệu mới ghi vào đỉnh (khoảng cách tới bờ, hướng chảy) có test EditMode: giá trị đúng ở ô sát bờ / xa bờ / ô rơi / ô lan / ao — kiểm bằng: MCP `run_tests` EditMode.
- [ ] Toàn bộ EditMode + PlayMode đạt sau mỗi brief — kiểm bằng: `run_tests`.
- [ ] Tham số chỉnh được (màu nông/sâu, độ trong, độ rộng + tốc độ bọt bờ, độ mạnh lấp lánh, tốc độ vệt chảy) nằm trong JSON `design/data/`, ghi `[số tạm]`, không hard-code — kiểm bằng: đọc diff + test nạp JSON.
- [ ] Điện thoại (Android, cắm USB) ≥ 30 FPS ở cảnh nhiều nước (bờ biển + ao + thác demo) — kiểm bằng: AI tự cài APK + đọc logcat/FPS overlay.
- [ ] User xem cạnh ảnh tham khảo, chấp nhận từng phần (gộp cuối 5k): màu, bọt bờ, lấp lánh, vệt chảy, độ mềm.
- [ ] Ghi giờ thực tế so với ước lượng vào roadmap.

## Ràng buộc
- **Thứ tự** (user xác nhận 2026-11-01): 1 → 2 → 3 → 4. Phần 1 là nền cho bọt bờ (cùng dữ liệu "gần bờ").
- **Ước lượng** [ý tưởng]:
  - Phần 1: 1–2. Phần 2: 1,5–3. Phần 3: 0,5–1. Phần 4: 2–3.
  - **Tổng 5–9 ngày-người** (roadmap đã sửa 4–8 → 5–9, M1 +1, 2026-11-01). M1 vẫn vượt quỹ; nếu cần cắt tiếp: phần 4 (vệt chảy).
- Biển nền và nước trong map phải liền màu ở chỗ giáp nhau (như [0012](../../design/decisions/0012-bien-tran-va-luat-dat-khoi-m1.md) và brief 0084).
- Nước trong suốt nhiều lớp đã từng gây lỗi vẽ chồng (brief 0082). Kiểu mới không được làm lại lỗi đó.
- API Unity / URP 17.4 dùng trong brief phải tra docs đúng 6000.4, ghi link (AGENTS.local).
- Mỗi phần một hoặc vài brief. Unity Editor dùng chung qua MCP, một phiên điều khiển tại một thời điểm.

## Quyết định đã chốt (ai chốt, khi nào)
- Làm phần 1–4, cộng "nét mềm mại hơn"; cắt phần 5 (bọt chân thác + mép tràn) sang M2 (user, 2026-11-01).
- "Nét mềm mại" = chuyển màu êm, viền bờ mờ dần, bọt có mép mềm, không có đường cắt cứng; chỉ ở shader, không bo lại hình khối nước (user, 2026-11-01).
- Áp cho biển nền + nước trong map, dùng chung kiểu; điện thoại ≥ 30 FPS (user, 2026-11-01).
- Làm sau khi 5j xong (world-tech `:141`).
- Không lưu ảnh tham khảo vào repo (user, 2026-11-01).
- Gác đo FPS điện thoại: hoàn thiện cả 4 phần trên máy tính trước, đo điện thoại sau (user, 2026-11-01).

## Câu hỏi còn mở
- ~~Thiết kế kỹ thuật phần 1~~ → chốt trong brief [0095](../briefs/0095-world-5k-mau-theo-do-sau-vien-bo.md) (2026-11-01). Tóm tắt: khoảng cách tới bờ ghi vào kênh R màu đỉnh (tối đa 4 ô); biển nền = xa bờ + sâu (không cần đáy, đảo cách mép map ≥ 30 ô); không dùng depth texture URP. Câu gốc:
  - "khoảng cách tới bờ" cho nước trong map (mesher ghi vào đỉnh, như độ dày 0084) và cho biển nền (theo mép đảo);
  - "thấy đáy" ở biển nền: hiện là mặt phẳng, chưa có đáy.
  - Có cần depth texture của URP không, và chi phí trên điện thoại.

## Lỗi chơi thử (user, 2026-11-02)
User: "nước bị lỗi rồi, mà nó quá bóng không cần thiết. các vệt bóng quá rõ thiếu tự nhiên" (2 ảnh Game view: ban ngày + ban đêm, biển phía đông trại + ao đào).
Planner khoanh vùng 2026-11-02 bằng MCP: đổi tham số material lúc Play, rồi chụp lại [run]. Material tạo lúc chạy, không có asset, nên không lưu thay đổi nào.
- **Lỗi 5k-1. Bọt bờ thành vạch trắng gấp khúc theo bậc ô.** Có ở biển trong map và ở ao đào, thấy cả ban đêm. Đặt `_FoamStrength` = 0 thì vạch mất hết.
  - Nguyên nhân: khoảng cách tới bờ chỉ ghi ở đỉnh (`BlockMesher.cs:1750`), rồi nội suy tuyến tính trên quad từng ô, nên đường đồng mức gãy theo góc ô.
  - Dải bọt (`Ocean.shader:190–191`) là vạch mảnh (3 dải trong 2 ô, `foamLine` 0,3, `foamSoft` 0,15), nên chỗ gãy lộ rõ.
- **Lỗi 5k-2. Quá bóng: mảng trắng loang khắp mặt nước.** Tắt `_Glint`, `_GlintHalo`, `_SparkleStrength` thì mặt nước hết loang.
  - Nguyên nhân chính: spec trên pháp tuyến gợn, `glint` 1,0 với `glintPower` 160 (`Ocean.shader:286`). Phần này phủ cả mặt, kể cả ban đêm.
- Vệt chảy (0099): tắt đi thì biển gần như không đổi, nên không phải nguồn chính.

## Kiểm chứng
- 2026-11-01 (Planner, bản nháp chờ user xác nhận; đọc lại file, đối chiếu repo):
  - Pillar: đạt — P1 `design/00-vision.md:15`.
  - Thành phần: đạt — client Unity, không thêm thành phần mới.
  - Glossary: chưa đạt — "bọt bờ", "vệt chảy", "biển nền", "mép tràn" chưa có trong `design/glossary.md`. Ghi ở Câu hỏi còn mở, thêm khi cập nhật world-tech.
  - Rà phần liên quan: đạt — roadmap dòng 5k `design/05-roadmap.md:36` (4–8), world-tech `:141`/`:200`. Lệch +2–3 ghi ở Ràng buộc; chưa sửa roadmap, chờ user chọn cắt.
  - [ý tưởng]: đạt — ước lượng và thứ tự ghi [ý tưởng]/đề xuất. "Quyết định đã chốt" chỉ là lựa chọn của user trong phiên, chưa có file decisions.
  - Nguồn: đạt — ảnh tham khảo do user cung cấp, không ghi tên game.
  - Phạm vi một người: đạt — có ước lượng từng phần, thuộc M1, cảnh báo vượt quỹ.
  - Không áp dụng (chưa có code): Game.Core, test "Xong khi", giờ thực tế.
  - Chỉ M1: đạt — 5k nằm trong M1 ở roadmap; đề xuất cắt sang M2 chờ user.
- 2026-11-01 (Planner, sau khi user trả lời a/b/c):
  - Glossary: đạt — thêm "Mép tràn", "Biển nền", "Khoảng cách tới bờ (nước)", "Bọt bờ", "Vệt chảy" (`design/glossary.md:61–65`).
  - Rà phần liên quan: đạt — roadmap 5k 4–8 → 5–9, M1 135,5–222,5 → 136,5–223,5 (`design/05-roadmap.md:24,31,36`); world-tech `:141` (gạch bọt thác) + `:200`; ideas `:71` (bọt thác → M2).
  - Chỉ M1: đạt — phần 5 chuyển sang M2 qua ideas.
- 2026-11-02 (Planner, ghi lỗi chơi thử 5k-1, 5k-2; đọc lại file và đối chiếu repo):
  - Nguồn: đạt. Nguyên nhân dẫn `BlockMesher.cs:1750` và `Ocean.shader:190–191,286` [repo]. Kết quả tắt từng hiệu ứng có ảnh chụp lúc Play [run].
  - Glossary: đạt. Chỉ dùng "bọt bờ", "vệt chảy", "khoảng cách tới bờ" đã có ở `design/glossary.md:61–65`.
  - [ý tưởng]: đạt. Hướng sửa chưa ghi vào file, chờ user chọn.
  - Không áp dụng (chỉ ghi lỗi, không đổi thiết kế/code): pillar, thành phần, rà phần liên quan, phạm vi/ước lượng, Game.Core, test "Xong khi", giờ thực tế, chỉ M1.
