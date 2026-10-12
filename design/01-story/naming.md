# Quy ước đặt tên

> Trạng thái: Nháp
> Liên quan: [Thuật ngữ](../glossary.md) · [Thế giới](world.md) · [Nhân vật](characters.md) · [Cơ chế](../02-gameplay/mechanics.md)

Tên riêng trong game (tên game, vùng đất, loài, tộc, phép, nhân vật, người chơi...). Thuật ngữ cơ chế (lệnh làm việc, nhập hồn...) nằm ở [glossary](../glossary.md), không ở đây. Viết bằng skill `naming` (kit); kiểm bằng `check_names.py` của skill (đường dẫn: `<Plugin root>/skills/naming/scripts/check_names.py`, Plugin root in ở đầu mỗi phiên).

## Cần đặt tên
**User tự đặt tên; AI chỉ duyệt** (user, 2026-10-12): kiểm trùng (web + `check_names.py`), cách đọc tiếng Việt/tiếng Anh, khớp quy ước. Bảng dưới là **danh sách nhắc** các mục chưa có tên — tổng hợp 2026-10-12 từ toàn bộ `design/`. Chưa có tên nào được chốt.

**Tên thiết kế ≠ tên trong game** [ý tưởng]: thuật ngữ trong [glossary](../glossary.md) (vd "lệnh làm việc", "vượt trội") là tên **nội bộ** để viết thiết kế. Tên người chơi thấy trong game có thể khác và đặt ở file này. Khi đã có tên trong game, glossary ghi thêm cột/ghi chú tên hiển thị.

Ưu tiên: **Trước M1** = cần cho B8 (roadmap) / prototype dùng tên tạm được; **M2** = vertical slice; **M3+** = sau.

| Nhóm | Mục cần tên | Số lượng | Ưu tiên | Ghi chú |
|---|---|---|---|---|
| Thương hiệu | Tên game | 1 | Trước M1 (tên tạm được) | Tra trùng store, nhãn hiệu trước khi dùng chính thức |
| Thế giới | Tên thế giới | 1 | M2 | [world](world.md) chưa có tông giọng |
| Thế giới | Tên map / vùng đất | 3 lúc đầu, thêm theo mùa | M2 (map của vertical slice trước) | Mỗi map có câu chuyện riêng |
| Thế giới | Tên vùng trong map, địa danh, khối cốt truyện | Nhiều | M2 | Theo [levels](../02-gameplay/levels.md) |
| Nhân vật | Danh xưng / tên mặc định của pháp sư (người chơi) | 1 | M2 | Người chơi có tự đặt tên không? |
| Nhân vật | NPC, nhân vật cốt truyện | _(chưa có danh sách)_ | M2 | [characters](characters.md) đang trống |
| Sinh vật | Tên gọi chung cho "sinh vật" trong thế giới | 1 | M2 | Tên người chơi dùng để gọi cả nhóm sinh vật |
| Sinh vật | Tộc | _(chưa định số)_ | M2 | Tên cùng tộc nên nhận ra là cùng nhóm |
| Sinh vật | Nguyên tố | _(bộ nguyên tố chưa định)_ | M2 | |
| Sinh vật | Loài | 3 (M1, tên tạm) → ~10 (M2) → nhiều hơn | M1 tạm, M2 chính thức | |
| Sinh vật | Biến thể | Vài biến thể mỗi loài | M3 | |
| Phép | Tên hiển thị của "phép sinh vật", "phép của pháp sư", "nhập hồn" | 3 | M2 | |
| Phép | Từng phép | ~20 phép sinh vật + ~8 phép pháp sư (slice) | M2 | |
| Phép | Các mức độ hiếm, nhóm phép | _(chưa định số mức)_ | M2 | |
| Trang bị | Tên chung, các bậc trang bị, từng trang bị | _(chưa định)_ | M3 | |
| Tài nguyên | Tiền trong game, tiền nạp | 2 | M3 | |
| Tài nguyên | **"Ma năng"** — cần đổi tên | 1 | M2–M3 | Dễ nhầm với "mana" (user đồng ý đổi, 2026-10-12) |
| Tài nguyên | Mana | 1 | M2 | |
| Tài nguyên | Nguyên liệu rèn, vật phẩm phân bổ lại, vật phẩm hỗ trợ, quà từ sinh vật | 4+ | M2–M3 | |
| Cửa hàng | Gói sinh vật, gói sách phép, gói trang bị | 3 | M3 | |
| Thẻ bài | Tên các loại thẻ bài, tên các bậc thẻ bài | _(chưa định số loại)_ | M2 | |
| Công trình | Kho, lò rèn, công trình khác | _(chưa có danh sách)_ | M1 (kho, tên tạm) → M3 | |
| Cơ chế hiển thị | Lệnh làm việc, năng lực làm việc, độ thuần thục, danh sách yêu cầu của loài, nhu cầu sinh vật / mức đáp ứng, vượt trội, biến thể, bảo hiểm, mảnh ký ức, kho sinh vật, túi đồ, tiến hoá / bậc | ~14 | M2 | Có thể giữ tên glossary nếu đủ dễ hiểu |
| PvP | Tên các hạng | _(chưa định số hạng)_ | Sau phát hành | |

## Tên game
<!-- Tên chính thức, các phương án, kết quả kiểm tra trùng (store, nhãn hiệu, tên miền) kèm link. -->
Chưa chốt. Đã thử 8 phương án do AI đề xuất (2026-10-12), kiểm bằng tìm kiếm web + `check_names.py` (denylist gồm tên game tham khảo và tên na ná tìm thấy):
- **Viventa** — *vivere* (sống) + *-enta*: "sự sống trở lại" → gắn P1 (hồi sinh). **Phương án duy nhất qua cả hai lần kiểm** [ý tưởng]. Đọc: tiếng Việt "vi-ven-ta", tiếng Anh /vɪˈvɛntə/; chưa thấy nghĩa xấu trong tiếng Việt, tiếng Anh; ngôn ngữ khác [chưa xác minh].
- **Aetharyn** — user đề xuất (2026-10-12). AI duyệt: chưa thấy trùng chính xác; nhưng giống 93% "Aethryn" (loài homebrew trên D&D Beyond), 88% "Aetharon" (nhân vật World of Warcraft), 86% "Myth of Ethary"; gốc "aether" rất phổ biến trong fantasy → khó nổi bật khi tìm kiếm. Đọc: "Ae" mơ hồ (tiếng Anh /iː/, /aɪ/ hay /eɪ/; tiếng Việt đọc "a-e"), "th" khác nhau giữa hai ngôn ngữ — lệch nguyên tắc "tránh cách viết dễ đọc sai". Nghĩa gợi phép thuật (aether), không gợi hồi sinh/sinh vật. Chờ user quyết.
- Bị loại (giống tên đã có ≥ 75% hoặc trùng): Verdavia, Faunora, Revira, Animora, Sylvara, Thalora, Anivera, Renascia — lý do ở "Danh sách tên".

Trước khi dùng chính thức: tra trực tiếp Steam, App Store, Google Play, nhãn hiệu, tên miền.

## Nguyên tắc chung
<!-- Ngôn ngữ gốc (tiếng Việt, tiếng Anh, ngôn ngữ tự tạo), thị trường mục tiêu, độ dài, dễ đọc trên cả PC và mobile, không trùng game/thương hiệu có sẵn. -->
Quyết định của user (2026-10-12):
- **Tên riêng là từ tự tạo** kiểu fantasy, **dễ đọc ở cả tiếng Việt lẫn tiếng Anh**.
- **Thị trường mục tiêu: toàn cầu.**

Hệ quả [ý tưởng]:
- Tên riêng giữ nguyên qua mọi bản dịch; giao diện và tên cơ chế dịch theo ngôn ngữ người chơi.
- Chỉ dùng chữ Latin không dấu (a–z), không ký tự đặc biệt → gõ, tìm kiếm, hiển thị được ở mọi ngôn ngữ.
- 2–3 âm tiết; tránh cụm phụ âm khó với người Việt (vd "str", "-nds") và cách viết dễ đọc sai với người Anh (vd "nh", "ngh", "gi").
- Không trùng từ có nghĩa xấu/nhạy cảm trong tiếng Việt, tiếng Anh (kiểm thêm các ngôn ngữ lớn khi chốt tên game).
- Độ dài: loài, phép ≤ 12 ký tự; map, vùng đất ≤ 16 ký tự (để vừa UI mobile).
- Tên game: tra trùng toàn cầu (Steam, App Store, Google Play, nhãn hiệu) trước khi dùng chính thức.

## Quy ước theo nhóm
<!-- Mỗi nhóm: cách ghép tên (gốc từ, âm tiết, hậu tố theo nguyên tố/tộc...), độ dài tối đa, ví dụ. -->
Gợi ý của AI [ý tưởng] (2026-10-12) để user tham khảo khi tự đặt tên. Hai loại tên:
- **Tự tạo** (giữ nguyên ở mọi ngôn ngữ): thứ cần nhận diện như thương hiệu — game, thế giới, map, tộc, loài, nhân vật, tiền tệ.
- **Mô tả, dịch theo ngôn ngữ người chơi**: thứ cần hiểu ngay — nguyên tố, phép thường, vật phẩm, trang bị, thẻ bài, công trình, tên cơ chế.

| Nhóm | Loại | Quy ước | Độ dài tối đa | Ví dụ (minh hoạ, không phải tên chốt) |
|---|---|---|---|---|
| Tên game | Tự tạo | 3 âm tiết, gợi hồi sinh / sinh vật / thiên nhiên | 12 | xem mục Tên game |
| Thế giới, vùng đất / map | Tự tạo | 2–3 âm tiết, kết thúc nguyên âm mở (-a, -ia, -o); hiển thị kèm mô tả dịch được | 16 | "Lunvera — Thung lũng sương" |
| Tộc | Tự tạo | 2 âm tiết; mỗi tộc có **một âm đuôi riêng** dùng lại trong tên loài của tộc | 10 | tộc đuôi "-vix" |
| Loài | Tự tạo | Gốc gợi đặc điểm/nguyên tố (1–2 âm) + âm đuôi của tộc → nhìn tên biết cùng tộc | 12 | "Mossvix", "Emberix" (cùng tộc "-vix/-ix") |
| Biến thể | Mô tả | Tên loài + tính từ dịch được | 12 + tính từ | "Mossvix Bạc" / "Silver Mossvix" |
| Nguyên tố | Mô tả | Từ thông dụng, dịch theo ngôn ngữ | — | Lửa / Fire |
| Phép | Mô tả (phép loài: có thể tự tạo) | Động từ/danh từ hình ảnh, dịch được | 16 | "Mưa lá" / "Leaf Rain" |
| Nhân vật | Tự tạo | 2–3 âm tiết | 12 | |
| Người chơi (pháp sư) | Người chơi tự đặt | Danh xưng chung dịch được ("Pháp sư"); tên mặc định tự tạo | 12 | |
| Tiền tệ, "ma năng" (đổi tên) | Tự tạo | 1–2 âm tiết, dễ nhớ | 10 | |
| Vật phẩm, trang bị, thẻ bài, công trình | Mô tả | Danh từ dịch được; bậc dùng số hoặc tiền tố chung | 16 | "Thẻ bài bậc 3", "Lò rèn" |

## Danh sách tên
<!-- Mọi tên riêng đã đặt. Script check_names.py đọc bảng này: cột 1 = tên, cột 2 = nhóm. Trạng thái: đề xuất / dùng / bỏ. -->
| Tên | Nhóm | Ý nghĩa / gốc | Trạng thái | Dùng ở |
|---|---|---|---|---|
| Aetharyn | Tên game | User đề xuất: aether (khí trời, phép thuật) + -yn | đề xuất | Chờ user quyết — giống 93% "Aethryn", 88% "Aetharon" |
| Viventa | Tên game | [ý tưởng] vivere (sống) + -enta: sự sống trở lại | đề xuất | — |
| Verdavia | Tên game | [ý tưởng] verde + -via/-ia: vùng đất xanh trở lại | bỏ | Giống 75% "Verdoria's Kingdom Quest" |
| Faunora | Tên game | [ý tưởng] fauna + flora | bỏ | Giống 86% "Faunori" |
| Revira | Tên game | [ý tưởng] re- + vir- (sống): hồi sinh | bỏ | Giống 83% "Revita" |
| Animora | Tên game | [ý tưởng] anima (linh hồn) + -ora | bỏ | Trùng map Fortnite Creative "Animora" (thu phục, tiến hoá bạn đồng hành) |
| Sylvara | Tên game | [ý tưởng] sylva (rừng) + -ara | bỏ | Trùng nhiều dự án (game người lớn, prototype indie) |
| Thalora | Tên game | [ý tưởng] thallos (chồi non) + -ora | bỏ | Giống 86% "Thalara" (itch.io) |
| Anivera | Tên game | [ý tưởng] anima + vera | bỏ | Giống 80% "Aniverse Arena" (Roblox) |
| Renascia | Tên game | [ý tưởng] renascence (tái sinh) | bỏ | Giống 93% "Renasci"; nhiều game "Renascence" |

## Tra trùng bên ngoài
<!-- Tên game / tên có thể thành thương hiệu: nơi đã tra (store, web, nhãn hiệu), ngày, link. Không phải tư vấn pháp lý. -->
Chỉ tra bằng tìm kiếm web (2026-10-12). **Chưa tra trực tiếp** Steam, App Store, Google Play, nhãn hiệu, tên miền → [chưa xác minh].

| Tên | Đã tra ở | Kết quả | Ngày |
|---|---|---|---|
| Verdavia | Tìm kiếm web | Không thấy game trùng; gần: "Verdoria's Kingdom Quest" ([Nintendo](https://nintendo.com/store/products/verdorias-kingdom-quest-magic-battles-and-sorcery-switch)), "Veredilia" ([Deku Deals](https://www.dekudeals.com/items/veredilia-the-sacred-forest)) | 2026-10-12 |
| Faunora | Tìm kiếm web | Không thấy trùng chính xác; gần: "Faunori" — game giải đố mobile về linh thú ([APKPure](https://apkpure.net/faunori/com.faunori.game)) | 2026-10-12 |
| Revira | Tìm kiếm web | Không thấy trùng chính xác; gần: "Revita" (2022, nhân vật mất ký ức — [Wikipedia](https://en.wikipedia.org/wiki/Revita)), "Reviva" ([itch.io](https://evilisa.itch.io/reviva)) | 2026-10-12 |
| Aetharyn | Tìm kiếm web | Không thấy trùng chính xác; gần: "Aethryn" ([D&D Beyond](https://www.dndbeyond.com/species/2107603-aethryn)), "Aetharon" ([Warcraft wiki](https://warcraft.wiki.gg/wiki/Debilitated_Aetharon)), "Myth of Ethary" ([RAWG](https://rawg.io/games/myth-of-ethary)), "Aethyr" ([Steam](https://store.steampowered.com/app/1271900/Aethyr/)) | 2026-10-12 |
| Viventa | Tìm kiếm web | Không thấy game trùng; gần: "Viviette" ([Bloody Disgusting](https://bloody-disgusting.com/video-games/3537343/16-bit-style-survival-horror-viviette-hits-nintendo-switch-december-20th/)), "Vivaldia" (game trong trình duyệt Vivaldi — [help.vivaldi.com](https://help.vivaldi.com/article/android-vivaldia-game/)) | 2026-10-12 |
| Thalora | Tìm kiếm web | Gần: "Thalara" ([itch.io](https://mystical-bento-box.itch.io/thalara)), "Althora" ([eipix](https://eipix.com/?p=8017)) | 2026-10-12 |
| Anivera | Tìm kiếm web | Gần: "Aniverse Arena" ([rowatcher](https://rowatcher.com/games/5607362259/aniverse-arena)), "Aniversus" ([prelaunch](https://www.prelaunch.com/projects/aniversus-strategic-deck-buil-aniversus-strategic-deck-building-adventure/discussions)) | 2026-10-12 |
| Renascia | Tìm kiếm web | Gần: "Renasci" ([itch.io](https://jozaru.itch.io)), "The Light of the Darkness: Renascence" ([GOG](https://static02.gog.com/dreamlist/game/the-light-of-the-darkness-renascence)) | 2026-10-12 |
| Animora | Tìm kiếm web | **Trùng**: map Fortnite Creative ([fortnite.com](https://www.fortnite.com/@tayama/6253-5864-4856?lang=de)); gần "Animoria" ([itch.io](https://fraamaury.itch.io/animoria)) | 2026-10-12 |
| Sylvara | Tìm kiếm web | **Trùng**: prototype indie ([itch.io](https://aliyigityilmaz.itch.io/sylvara)), game người lớn trên itch.io; gần "Sylvari" của Guild Wars 2 ([wiki](https://wiki.guildwars2.com/wiki/Sylvari)) | 2026-10-12 |
