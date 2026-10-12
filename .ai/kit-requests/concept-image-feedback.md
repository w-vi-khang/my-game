# Góp ý cho skill `concept-image` (kit 0.7.0) — từ my-game

> Trạng thái: góp ý, chưa gửi (viết 2026-10-12). Bằng chứng: chạy lớp 2–3 trên 3 ảnh đã biết lỗi của my-game [run 2026-10-12].

1. **`make_questions.py` sai số nhiều của chủ thể:** `"How many foxs are in the image?"` — chỉ thêm "s". Đề xuất: đọc `plural` cho cả `subject` (vd `"plural": {"fox": "foxes"}`), hoặc hỏi "How many animals/subjects…".
2. **Lớp 2 nên cho tiền tố truy vấn:** OWLv2 với nhãn trần ("leg") không phát hiện chân nào; "animal leg" phát hiện được. Đề xuất ghi vào SKILL.md: lệnh detector của project nên nhận tiền tố theo loại chủ thể và trả về nhãn gốc của spec.
3. **Lớp 2 hay báo sai (3/12 phép đếm):** SKILL.md đã nói "mâu thuẫn → xem lại lớp 1"; nên nói rõ thêm: kết quả lớp 2 lệch là **cảnh báo**, không tự kết luận lỗi.
4. **Gộp câu hỏi lớp 3 vào một lần gọi:** với VLM trên ZeroGPU, 12 câu một lần gọi tốn ~9 giây GPU thay vì 12 lần gọi. Đề xuất gợi ý trong SKILL.md (định dạng "<số>. <trả lời>").
5. **Bộ kiểm phong cách chung:** để nhiều ảnh đồng bộ, nên có chỗ trong cấu hình project (vd `concept_image.style_checks: {forbid: [...], require: [...]}`) để `make_questions.py` tự thêm vào mọi spec, thay vì chép tay.
