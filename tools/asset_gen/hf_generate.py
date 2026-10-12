"""Tạo model 3D thô bằng AI qua Hugging Face Spaces: prompt -> ảnh concept (FLUX.2 klein 4B) -> GLB (TRELLIS.2).

  <venv>/bin/python tools/asset_gen/hf_generate.py --name <tên> --out <thư mục> (--prompt "..." | --image ảnh.png)
      [--seed 0] [--resolution 1024] [--decimation 20000] [--texture-size 1024] [--image-only]

-> exit 0 nếu ra được GLB (hoặc ảnh, với --image-only), exit 1 nếu lỗi.
Cần: gradio_client + huggingface_hub (venv ngoài repo), token HF đã lưu bằng `hf auth login` (script không in token).
Mỗi lần chạy trừ vào quota ZeroGPU của chủ token (https://huggingface.co/docs/hub/spaces-agents).
Ghi kèm <tên>.provenance.json: model, giấy phép, tham số, seed, thời gian từng bước — để kiểm giấy phép và đo công.
Quyết định: design/decisions/0006-cong-cu-kiem-tra-asset.md; quy trình: design/03-art-audio/art-style.md.
"""
import argparse, json, os, shutil, sys, time

from gradio_client import Client, handle_file
from huggingface_hub import get_token

IMAGE_SPACE = "black-forest-labs/FLUX.2-klein-4B"   # model Apache-2.0
MESH_SPACE = "microsoft/TRELLIS.2"                  # code + model MIT
LICENSES = {IMAGE_SPACE: "apache-2.0 (FLUX.2-klein-4B)", MESH_SPACE: "MIT (TRELLIS.2)"}


def client(space, token):
    return Client(space, token=token, verbose=False, analytics_enabled=False)


def as_path(result):
    """gradio_client trả đường dẫn file đã tải về, hoặc dict FileData."""
    if isinstance(result, (list, tuple)):
        result = result[0]
    if isinstance(result, dict):
        result = result.get("path") or result.get("value") or result.get("url")
    return result


def image_ext(path):
    """Đuôi theo nội dung file: Space FLUX trả WebP dù tên file là gì."""
    with open(path, "rb") as f:
        head = f.read(12)
    if head[:4] == b"RIFF" and head[8:12] == b"WEBP":
        return ".webp"
    if head[:8] == b"\x89PNG\r\n\x1a\n":
        return ".png"
    if head[:2] == b"\xff\xd8":
        return ".jpg"
    return os.path.splitext(path)[1] or ".img"


def gen_image(token, prompt, seed):
    c = client(IMAGE_SPACE, token)
    res = c.predict(prompt=prompt, input_images=[], mode_choice="Distilled (4 steps)",
                    seed=seed, randomize_seed=False, width=1024, height=1024,
                    num_inference_steps=4, guidance_scale=1.0, prompt_upsampling=False,
                    api_name="/infer")
    return as_path(res[0]), res[1]


def gen_mesh(token, image, seed, resolution, decimation, texture_size):
    c = client(MESH_SPACE, token)   # một Client = một session; image_to_3d và extract_glb dùng chung state
    c.predict(api_name="/start_session")
    pre = as_path(c.predict(input=handle_file(image), api_name="/preprocess_image"))
    c.predict(image=handle_file(pre), seed=seed, resolution=str(resolution), api_name="/image_to_3d")
    out = c.predict(decimation_target=decimation, texture_size=texture_size, api_name="/extract_glb")
    return pre, as_path(out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--name", required=True)
    ap.add_argument("--out", required=True)
    src = ap.add_mutually_exclusive_group(required=True)
    src.add_argument("--prompt")
    src.add_argument("--image")
    ap.add_argument("--seed", type=int, default=0)
    ap.add_argument("--resolution", type=int, choices=(512, 1024, 1536), default=1024)
    ap.add_argument("--decimation", type=int, default=20000)
    ap.add_argument("--texture-size", type=int, default=1024)
    ap.add_argument("--image-only", action="store_true")
    a = ap.parse_args()

    token = get_token()
    if not token:
        print("LỖI: chưa có token HF. Chạy `hf auth login` (trong venv) rồi thử lại.")
        sys.exit(1)
    os.makedirs(a.out, exist_ok=True)
    prov = {"name": a.name, "date": time.strftime("%Y-%m-%d %H:%M:%S"), "steps": {}, "licenses": {}}
    try:
        if a.prompt:
            t = time.time()
            img, seed = gen_image(token, a.prompt, a.seed)
            image = os.path.join(a.out, a.name + "_concept" + image_ext(img))
            shutil.copy(img, image)
            prov["steps"]["image"] = {"space": IMAGE_SPACE, "prompt": a.prompt, "seed": seed,
                                      "seconds": round(time.time() - t, 1), "file": image}
            prov["licenses"][IMAGE_SPACE] = LICENSES[IMAGE_SPACE]
            print("ảnh concept:", image)
        else:
            image = os.path.abspath(a.image)
            prov["steps"]["image"] = {"source": "user", "file": image}
        if not a.image_only:
            t = time.time()
            pre, glb = gen_mesh(token, image, a.seed, a.resolution, a.decimation, a.texture_size)
            dst = os.path.join(a.out, a.name + ".glb")
            shutil.copy(glb, dst)
            shutil.copy(pre, os.path.join(a.out, a.name + "_preprocessed" + os.path.splitext(pre)[1]))
            prov["steps"]["mesh"] = {"space": MESH_SPACE, "seed": a.seed, "resolution": a.resolution,
                                     "decimation_target": a.decimation, "texture_size": a.texture_size,
                                     "seconds": round(time.time() - t, 1), "file": dst,
                                     "bytes": os.path.getsize(dst)}
            prov["licenses"][MESH_SPACE] = LICENSES[MESH_SPACE]
            print("GLB:", dst)
    except Exception as e:  # lỗi mạng, hết quota, Space đổi API...
        prov["error"] = "%s: %s" % (type(e).__name__, e)
        print("LỖI:", prov["error"])
    with open(os.path.join(a.out, a.name + ".provenance.json"), "w", encoding="utf-8") as f:
        json.dump(prov, f, ensure_ascii=False, indent=2)
    sys.exit(1 if "error" in prov else 0)


if __name__ == "__main__":
    main()
