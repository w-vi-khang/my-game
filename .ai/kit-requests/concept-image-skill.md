# Yêu cầu: skill `concept-image` cho ai-workflow-kit

> Trạng thái: **đã phát hành** — skill `concept-image` có trong ai-workflow-kit 0.7.0 (2026-10-12). Bản yêu cầu gốc viết 2026-10-12 từ project my-game; giữ lại để tra cứu.
> Đã thay skill project `local-concept-image` (my-game). Việc còn lại ở my-game: mục 14 (4–5), theo dõi trong `design/05-roadmap.md` mục C.

## 1. Vấn đề
Ảnh do AI tạo (concept nhân vật, sinh vật, vật phẩm...) hay có **lỗi cấu trúc mà prompt không yêu cầu**, và AI kiểm tra bằng mắt dễ bỏ sót. Lỗi thật đã gặp ở my-game (FLUX.2-klein-4B, 2026-10-12):
- Cáo 4 chân → ảnh có **5 chân** (chân thừa mọc từ giữa thân).
- Cáo phép thuật 1 đuôi → **3 đuôi** ở cả 2 ảnh, 1 đuôi mọc sau cổ (model kéo theo hình mẫu kitsune).
- **Nhân hoá ngoài ý muốn**: lông mày, miệng cười kiểu người — vẫn cười dù prompt ghi "no human-like smile".
- Vật mọc trên thân thành **khay/yên rời**.
- Ảnh trả về là WebP dù đuôi file đặt `.png`.

Skill này biến việc tạo + kiểm tra ảnh thành quy trình có cổng kiểm, có báo cáo, kiểm được bằng công cụ chứ không chỉ bằng mắt AI.

## 2. Phạm vi
- **Trong kit (chung):** quy trình, nguyên tắc, checklist chuẩn, mẫu spec/báo cáo, công cụ cắt vùng, công cụ so số đếm với spec, sinh câu hỏi kiểm tra, cấu hình theo project.
- **Ngoài kit (project khai báo):** model/lệnh tạo ảnh, model kiểm tra, tài liệu phong cách, nơi lưu file, yêu cầu của bước sau (vd đầu vào image-to-3D), danh sách "hình mẫu quen" riêng của thể loại.
- Không thuộc skill: tạo 3D, rig, kiểm tra mesh (việc của project).

## 3. Định danh
- Tên: `concept-image` (gợi ý; hoặc `ai-image-qa`).
- `description`: "Tạo và kiểm tra ảnh concept bằng AI (nhân vật, sinh vật, vật phẩm, môi trường) theo quy trình có cổng: viết bảng đặc điểm kỳ vọng từ mô tả, tạo ảnh (có thể kèm ảnh tham chiếu cấu trúc), soi theo vùng + đếm bằng công cụ phát hiện + hỏi–đáp bằng VLM độc lập, báo cáo đạt/lỗi kèm vị trí, chỉ chuyển bước sau khi user duyệt. Dùng khi user muốn tạo, làm lại, kiểm tra hoặc duyệt ảnh concept/ảnh AI."
- Từ khoá (không dấu): `anh concept`, `tao anh`, `concept sinh vat`, `concept nhan vat`, `kiem tra anh`, `giai phau`, `bat thuong`, `thua chan`, `anh ai`; yếu: `~concept`, `~anh`.
  - Lưu ý trùng: skill ý tưởng (ideation) của project có thể có từ khoá `concept` mạnh → `concept` ở đây phải là từ yếu. Đã thử ở my-game: router chọn đúng 4/4 (3 prompt ảnh → skill này, 1 prompt brainstorm → ideation) [run 2026-10-12].
- `related`: skill viết tài liệu thiết kế của project (nếu có).

## 4. Cấu hình project (`.ai/project.json`)
```json
"concept_image": {
  "style_doc": "design/03-art-audio/art-style.md",
  "out_dir": "~/Documents/User/khang/Game/art-test/gen",
  "allow_in_repo": false,
  "generator": {
    "command": "{venv}/bin/python tools/asset_gen/hf_generate.py --name {name} --out {out} --image-only --seed {seed} --prompt {prompt}",
    "reference_arg": "--ref {ref}",
    "license": "apache-2.0 (FLUX.2-klein-4B)",
    "max_per_turn": 3,
    "quota_note": "ZeroGPU free ~3,5 phút/ngày [chưa xác minh]; chừa quota cho bước sau"
  },
  "checkers": [
    {"name": "detector", "kind": "count", "command": "...", "output": "json: [{label, score, box}]"},
    {"name": "vlm", "kind": "qa", "command": "...", "output": "json: [{question, answer}]"}
  ],
  "downstream": {
    "name": "TRELLIS.2 image-to-3D",
    "requirements": ["1 chủ thể", "toàn thân, không cắt mép", "nền trơn", "góc ba phần tư", "tư thế trung tính"]
  },
  "tropes": ["cáo + magical → nhiều đuôi (kitsune)", "vật mọc trên lưng → khay/yên rời"],
  "license_denylist": ["flux-non-commercial-license"]
}
```
- Thiếu `concept_image` → skill hỏi user tuần tự (generator, nơi lưu, style doc) và đề xuất ghi vào project.json.
- `allow_in_repo: false` → skill không được ghi ảnh/spec/báo cáo vào repo.
- `init` của kit nên hỏi phần này khi project có làm asset bằng AI.

## 5. Nguyên tắc gốc (đưa nguyên vào SKILL.md)
1. **Bất thường so với tự nhiên chỉ hợp lệ khi mô tả nói rõ.** "3 mắt" có trong mô tả → đúng; không có → lỗi. Áp dụng cho số bộ phận, vị trí, chất liệu, hành vi kiểu người.
2. Mô tả mơ hồ → nêu các cách hiểu; hỏi user, hoặc (nếu rẻ) tạo mỗi cách hiểu 1 ảnh và ghi rõ.
3. Không kết luận "đạt" khi chưa đếm từng bộ phận trong spec và chưa xem mọi vùng.
4. Mắt AI là một lớp kiểm tra, không phải lớp duy nhất (mục 7).
5. Mọi ảnh ghi nguồn gốc (model, giấy phép, prompt, seed, ảnh tham chiếu, thời gian).

## 6. Quy trình
1. **Đọc** `style_doc`, `downstream.requirements`, `tropes`.
2. **Spec** — `<name>.spec.md` (mẫu ở mục 9): bảng đặc điểm đếm được (mặc định theo loài/vật thật gần nhất), đặc điểm phép thuật/khác thường mô tả yêu cầu, danh sách "không được có", phong cách, yêu cầu downstream, ngân sách độ phức tạp.
3. **Ảnh tham chiếu cấu trúc (khuyên dùng khi đã lỗi giải phẫu ≥ 1 lần):** dựng khối thô (blockout) đúng số bộ phận/tư thế (vd trong Blender) → render → truyền làm ảnh tham chiếu cho model hỗ trợ image-to-image/multi-reference (FLUX.2 klein: "multi-reference editing" — [model card](https://huggingface.co/black-forest-labs/FLUX.2-klein-4B)). Ghi ảnh tham chiếu vào spec.
4. **Prompt** từ spec: số lượng **kèm vị trí** ("a single tail attached at the rear, behind the hips"); mô tả mặt bằng biểu cảm động vật thay vì câu cấm ("calm neutral animal expression, closed muzzle"); vật mọc trên thân: "growing directly from its fur, no base or tray". Lưu prompt vào spec.
5. **Tạo** bằng `generator.command`; không vượt `max_per_turn`; kiểm định dạng ảnh theo nội dung file (magic bytes), không theo đuôi.
6. **Kiểm tra 3 lớp** (mục 7) → `<name>.check.md`.
7. **Lặp:** lỗi → sửa prompt/seed/ảnh tham chiếu → tạo lại; ghi số lần tạo và lý do.
8. **Cổng user:** gửi ảnh + tóm tắt báo cáo; chỉ user duyệt mới chuyển downstream. Ghi trạng thái duyệt vào check.md.
9. **Học từ lỗi bị bỏ sót:** user chỉ ra lỗi AI không bắt được → thêm mục checklist cụ thể (project: `tropes`; chung: đề xuất PR vào kit).

## 7. Ba lớp kiểm tra
| Lớp | Cách | Bắt gì | Bắt buộc |
|---|---|---|---|
| 1. Soi theo vùng | `crops.py` → AI xem full + mọi ô, đi theo checklist (mục 8) | Toàn diện, chủ quan | Có |
| 2. Đếm bằng phát hiện vật thể | `checkers[kind=count]` (vd Grounding DINO — Apache-2.0, OWLv2 — Apache-2.0, Florence-2 — MIT; đều có Space trên HF) → `count_check.py` so với bảng đếm trong spec | Thừa/thiếu bộ phận, khách quan | Khi project khai báo |
| 3. Hỏi–đáp bằng VLM độc lập (kiểu [TIFA](https://ar5iv.labs.arxiv.org/html/2303.11897)) | `make_questions.py` sinh câu hỏi từ spec ("How many tails does the animal have?", "Is the animal smiling like a human?") → `checkers[kind=qa]` (vd Qwen2.5-VL — Apache-2.0) → so đáp án kỳ vọng | Lỗi đếm, nhân hoá, lệch mô tả — không phụ thuộc mắt AI đang làm việc | Khi project khai báo |
- Kết quả lớp 2–3 **mâu thuẫn** với lớp 1 → coi là lỗi cho tới khi AI xem lại vùng đó và giải thích; không tự gạt bỏ.
- **Kiểm chứng công cụ kiểm tra trước khi tin:** chạy lớp 2–3 trên bộ ảnh đã biết lỗi (mục 11). Công cụ không bắt được lỗi đã biết → ghi giới hạn, không dùng cho loại lỗi đó.
- Để sau (không bắt buộc): [VQAScore / t2v_metrics](https://github.com/linzhiqiu/t2v_metrics) (độ khớp prompt); [RichHF](https://research.google/blog/rich-human-feedback-for-text-to-image-generation/) (heatmap vùng lỗi — có công bố model hay chỉ dataset: [chưa xác minh]).

## 8. Checklist chuẩn (lớp 1)
- **Đếm**: mọi mục trong bảng spec; chân đếm theo **điểm chạm đất**; bộ phận bị che phải truy được tới thân.
- **Vị trí gốc**: đuôi ở sau hông, tai trên đầu, chân dưới thân — không mọc từ giữa thân/sau cổ.
- **Khớp**: gập đúng chiều; không dính/nhập hai bộ phận.
- **Thừa/thiếu/lơ lửng**: mảnh rời không nối vào thân.
- **Đối xứng**: mắt/tai cùng kiểu, cỡ hợp phối cảnh.
- **Nhân hoá ngoài ý muốn**: lông mày, môi, răng, biểu cảm, tay kiểu người.
- **Hình mẫu quen kéo theo**: đối chiếu `tropes` của project.
- **Bề mặt**: chữ/watermark, vệt lỗi, texture lặp, nền dính vào chủ thể.
- **Ánh sáng/tiếp đất**: bóng hợp hướng, chân chạm đất.
- **Mô tả + phong cách**: từng ý trong mô tả có mặt; đúng `style_doc`; giống loài/vật thật quá mức hay không (nếu style yêu cầu khác thường); không giống thiết kế có bản quyền.
- **Downstream**: từng mục `downstream.requirements`.

## 9. Mẫu file (đưa vào `templates/`)
**spec.md**: Mô tả gốc của user · Cách hiểu (nếu mơ hồ) · Bảng đếm `| Bộ phận | Số lượng | Vị trí |` · Đặc điểm khác thường được phép · Không được có · Phong cách · Downstream · Ảnh tham chiếu · Prompt · Lựa chọn của user.
**check.md**: Ảnh + model + seed + lần tạo thứ mấy · Bảng `| Mục | Kỳ vọng | Thấy | Đạt/Lỗi | Vùng | Lớp |` · Kết quả lớp 2 (bảng đếm) · Kết quả lớp 3 (câu hỏi/đáp) · Kết luận · Sửa cho lần sau · Trạng thái duyệt của user.
**counts (máy đọc)**: spec nên kèm khối JSON `{"tail": 1, "leg": 4, "eye": 2, "ear": 2}` để `count_check.py` và `make_questions.py` đọc.

## 10. Công cụ (`scripts/`, chỉ thư viện chuẩn)
| Script | Việc | Ghi chú |
|---|---|---|
| `crops.py <ảnh> <thư mục> [--grid 3] [--overlap 0.2]` | Đổi sang PNG, cắt lưới vùng chồng lấn, **kiểm kích thước từng ô** | Bản đang dùng: `my-game/.claude/skills/local-concept-image/scripts/crops.py`. macOS dùng `sips`; **lỗi đã gặp**: `sips --cropOffset` bị bỏ qua khi X = 0 mà Y > 0 (macOS 15.7.3) → lệch tối thiểu 1 px. Linux: cần nhánh `magick` (ImageMagick) — chưa viết |
| `count_check.py <spec.md> <detector.json> [--min-score 0.3]` | Đếm nhãn theo ngưỡng, so với khối counts → bảng + exit 1 nếu lệch | Chưa viết |
| `make_questions.py <spec.md>` | Sinh câu hỏi + đáp án kỳ vọng từ counts + "không được có" | Chưa viết |
| `image_kind.py <file>` | Định dạng thật theo magic bytes (PNG/JPEG/WebP) | Logic có ở `my-game/tools/asset_gen/hf_generate.py:image_ext` |
- Generator và checker là **lệnh của project** (cần mạng/GPU/token) — kit không gọi API trực tiếp, chỉ chạy lệnh trong cấu hình và đọc JSON trả về.
- Token: chỉ đọc qua thư viện/biến môi trường; skill **cấm** in, ghi hoặc yêu cầu user dán token vào chat.

## 11. Kiểm chứng skill (trước khi `status: active`)
- `validate_skills.py` pass.
- Router: ≥ 3 prompt ảnh chọn skill này; prompt brainstorm/ideation không chọn nhầm.
- **Bộ ảnh đã biết lỗi** (fixture, không đưa ảnh vào kit nếu giấy phép không cho — có thể chỉ mô tả + đường dẫn local): my-game có `creature_a` (5 chân), `creature_b1`, `creature_b2` (3 đuôi, cười) ở `~/Documents/User/khang/Game/art-test/gen/` kèm `*.check.md`. Tiêu chí: lớp 1 bắt đủ; lớp 2/3 (nếu cấu hình) bắt được ít nhất lỗi đếm.
- `crops.py`: mọi ô đúng kích thước, kể cả ô cột 0 hàng > 0.
- Script mới có test: spec mẫu + JSON mẫu → exit code đúng.

## 12. Cấm
- Tự coi là đạt khi chưa đếm và chưa xem mọi vùng.
- Dùng model có giấy phép trong `license_denylist` (vd FLUX.2-klein-9B: FLUX Non-Commercial — [model card](https://huggingface.co/black-forest-labs/FLUX.2-klein-9B)).
- Prompt theo tên/thiết kế có bản quyền.
- Chuyển downstream khi user chưa duyệt.
- Vượt `max_per_turn` mà không báo user; ghi file vào repo khi `allow_in_repo: false`.

## 13. Tiêu chí hoàn thành (của mỗi lần dùng skill)
- [ ] Có spec (bảng đếm + counts JSON + prompt) trước khi tạo.
- [ ] `crops.py` exit 0; đã xem full + mọi ô.
- [ ] Lớp 2/3 đã chạy nếu project khai báo; mâu thuẫn đã giải thích.
- [ ] Có check.md đủ mục + số lần tạo; ảnh đã gửi user; trạng thái duyệt đã ghi.

## 14. Việc ở my-game sau khi kit phát hành
1. ✅ Thêm khối `concept_image` vào `.ai/project.json` (mục 4) — commit 357c74d.
2. ✅ Xoá `.claude/skills/local-concept-image/` + mục trong `.ai/skill-map.local.json`.
3. ✅ `design/03-art-audio/art-style.md` (quy trình asset bước 2): đổi tên skill → `concept-image` của kit.
4. ✅ (2026-10-12: `tools/asset_gen/concept_check.py` — OWLv2 + Qwen3-VL; xem art-style) Viết lệnh checker lớp 2–3 (gọi Space Grounding DINO / Qwen2.5-VL bằng `gradio_client`) và chạy kiểm chứng trên bộ ảnh đã biết lỗi.
5. ⏳ Thêm `--ref` cho `tools/asset_gen/hf_generate.py` (truyền `input_images` cho Space FLUX.2-klein-4B — tham số có trong API [run 2026-10-12]).

## 15. Chưa xác minh
- Quota ZeroGPU free (3,5 hay 5 phút/ngày, có giới hạn số lượt không); Space phát hiện/VLM có trừ quota không.
- Grounding DINO/OWLv2 phát hiện "tail"/"leg" của sinh vật stylized có đáng tin không — phải đo bằng bộ ảnh đã biết lỗi.
- Ảnh tham chiếu blockout có khoá được số bộ phận với FLUX.2 klein không — phải thử.
- Ảnh concept kiểu tranh vẽ tay cho kết quả image-to-3D kém hơn ảnh kiểu render — lý do không chọn LoRA vẽ tay/anime cho mục đích này.
