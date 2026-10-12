"""Lệnh kiểm tra lớp 2–3 cho skill `concept-image` của ai-workflow-kit (khai báo ở .ai/project.json > concept_image.checkers).

  <venv>/bin/python tools/asset_gen/concept_check.py detect <ảnh> <nhãn,nhãn> <out.json> [--threshold 0.1] [--prefix "animal "]
      Lớp 2 — đếm bộ phận bằng OWLv2 (google/owlv2-base-patch16-ensemble) qua Space merve/owlv2.
      Ghi [{"label", "box"}] cho skill (count_check.py). Space chạy CPU → không trừ quota ZeroGPU [run 2026-10-12].
  <venv>/bin/python tools/asset_gen/concept_check.py qa <ảnh> <questions.json> <out.json>
      Lớp 3 — hỏi–đáp bằng Qwen3-VL qua Space prithivMLmods/Qwen3-VL-HF-Demo (ZeroGPU → TRỪ quota GPU
      của chủ token, ~9 giây cho 12 câu [run 2026-10-12]). Gộp mọi câu vào MỘT lần gọi để tiết kiệm quota.
      Ghi [{"id", "question", "answer"}] cho skill (qa_check.py).
      Không dùng: Space chính thức Qwen/Qwen2.5-VL-32B-Instruct và Qwen/Qwen3-VL-Demo — lỗi phía máy chủ khi gọi
      qua API [run 2026-10-12].

Cần: gradio_client (venv ngoài repo). Token HF đọc qua huggingface_hub (không in token).
Quyết định: design/decisions/0006-cong-cu-kiem-tra-asset.md (ngoại lệ M0 cho công cụ art).
exit 0 = đã ghi kết quả; 1 = lỗi gọi Space / đầu vào.
"""
import argparse, json, os, sys

from gradio_client import Client, handle_file
from huggingface_hub import get_token

DETECT_SPACE = "merve/owlv2"
VLM_SPACE = "prithivMLmods/Qwen3-VL-HF-Demo"  # bên thứ ba — có thể đổi/tắt bất kỳ lúc nào


def client(space):
    return Client(space, token=get_token(), verbose=False, analytics_enabled=False)


def mask_box(path):
    """Hộp bao của mask (nếu có Pillow); không có thì trả None — count_check.py không cần box."""
    try:
        from PIL import Image
    except ImportError:
        return None
    try:
        im = Image.open(path).convert("L")
        return list(im.point(lambda v: 255 if v > 0 else 0).getbbox() or [])
    except Exception:
        return None


def detect(a):
    labels = [x.strip() for x in a.labels.split(",") if x.strip()]
    if not labels:
        sys.exit("LỖI: không có nhãn nào để đếm")
    # Truy vấn có tiền tố ("animal leg") bắt tốt hơn nhãn trần ("leg") trên bộ ảnh lỗi [run 2026-10-12];
    # nhãn ghi ra là khoá gốc trong spec để count_check.py so khớp.
    queries = {f"{a.prefix}{x}".strip(): x for x in labels}
    res = client(DETECT_SPACE).predict(handle_file(a.image), ",".join(queries), a.threshold, api_name="/predict")
    out = []
    for ann in (res or {}).get("annotations", []):
        q = ann.get("label")
        out.append({"label": queries.get(q, q), "query": q, "box": mask_box(ann.get("image"))})
    return out


def qa(a):
    import base64, re
    qs = json.load(open(a.questions, encoding="utf-8"))
    prompt = ("Look carefully at the image and answer each question on its own line as '<number>. <answer>'.\n"
              + "\n".join(f"{i + 1}. {q['question']}" for i, q in enumerate(qs)))
    b64 = base64.b64encode(open(a.image, "rb").read()).decode()
    raw = client(VLM_SPACE).predict("Query", prompt, b64, 512, a.gpu_timeout, api_name="/run_understanding")
    try:
        text = json.loads(raw).get("text", "") if isinstance(raw, str) and raw.strip().startswith("{") else str(raw)
    except json.JSONDecodeError:
        text = str(raw)
    answers = {int(m.group(1)): m.group(2).strip() for m in re.finditer(r"^\s*(\d+)[.)]\s*(.*)$", text, re.M)}
    out = []
    for i, q in enumerate(qs):
        ans = answers.get(i + 1, "")
        out.append({"id": q.get("id"), "question": q["question"], "answer": ans})
        print(f"{q.get('id')}: {ans[:80]}")
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    d = sub.add_parser("detect"); d.add_argument("image"); d.add_argument("labels"); d.add_argument("out")
    d.add_argument("--threshold", type=float, default=0.1, help="Space lọc sẵn ở 0.1 nên nhỏ hơn không có tác dụng")
    d.add_argument("--prefix", default="animal ", help='tiền tố truy vấn, vd "animal " cho sinh vật, "" cho vật phẩm')
    q = sub.add_parser("qa"); q.add_argument("image"); q.add_argument("questions"); q.add_argument("out")
    q.add_argument("--gpu-timeout", type=int, default=30, help="giây GPU tối đa xin cho một lần gọi (ZeroGPU)")
    a = ap.parse_args()
    if not os.path.isfile(a.image):
        sys.exit(f"LỖI: không thấy ảnh {a.image}")
    if not get_token():
        sys.exit("LỖI: chưa có token HF. Chạy `hf auth login` (trong venv) rồi thử lại.")
    try:
        result = detect(a) if a.cmd == "detect" else qa(a)
    except Exception as e:
        print(f"LỖI gọi Space: {type(e).__name__}: {str(e)[:300]}")
        return 1
    with open(a.out, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=2)
    print(f"Đã ghi {len(result)} mục → {a.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
