# 0010 — MCP for Unity: AI làm việc trực tiếp trong Unity Editor đang mở

> Trạng thái: Đề xuất
> Ngày: 2026-10-13

## Bối cảnh
- Chạy test / build Unity bằng dòng lệnh (`tools/unity_test.py`) cần **tắt Unity** — project chỉ mở được một Editor. Khi thử chơi (M1 bước 4), user phải tắt Unity để AI chạy test rồi mở lại; AI không đọc được Console, không bấm Play được (user, 2026-10-13).
- Lỗi bước 4 (khung chọn ô bị huỷ) chỉ lộ khi user chơi thử — AI cần tự vào Play, đọc Console, chụp màn hình.
- Đây là **công cụ phát triển**, không phải thành phần của game (không thêm vào [components](../04-tech/components.md)).

## Các lựa chọn
- **MCP for Unity** (CoplayDev, MIT, miễn phí) — package Unity + server Python nhỏ; Claude Code là client. v10.3.0 phát hành 2026-10-04, hỗ trợ Unity 2021.3 → 6.x ([repo](https://github.com/CoplayDev/unity-mcp), [PyPI mcpforunityserver 10.3.0](https://pypi.org/project/mcpforunityserver/10.3.0/)). Đã đọc mã nguồn tag v10.3.0 [run 2026-10-13]: có `run_tests` (EditMode/PlayMode), `manage_editor` (play/pause/stop), `read_console`, chụp màn hình (`manage_camera`), `execute_code`. **Telemetry bật mặc định**, gửi tới `api-prod.coplay.dev`; tắt bằng biến môi trường `DISABLE_TELEMETRY=true` (server) hoặc EditorPrefs `MCPForUnity.TelemetryDisabled` (phía Unity; dữ liệu phía Unity gửi qua server). Bên thứ ba → rủi ro ngừng bảo trì.
- **Unity MCP chính thức** (package `com.unity.ai.assistant`, beta) — cần project nối Unity Cloud + trial/gói Unity AI ([blog Unity 2026-05-11](https://unity.com/blog/unity-ai-mcp-how-to-get-started)); docs không liệt kê chạy test / Play ([docs](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/unity-mcp-get-started.html)). Trái với M1 không dùng dịch vụ Unity.
- **Tự viết cầu nối** (script Editor đọc lệnh từ file/socket) — 1–2 ngày-người [ước lượng], tự bảo trì.
- **Không làm gì:** user chạy test bằng Test Runner trong Editor, hoặc tắt Unity khi AI cần chạy.

## Quyết định
User đồng ý cài **MCP for Unity v10.3.0** (2026-10-13). Ghim version (không dùng `#main`); tắt telemetry; chế độ stdio (Claude Code tự chạy server). Chờ user chuyển **Chốt** sau khi dùng thử.

## Hệ quả
- `client/Packages/manifest.json`: `com.coplaydev.unity-mcp` → `…/unity-mcp.git?path=/MCPForUnity#v10.3.0`. Package kéo thêm `com.unity.nuget.newtonsoft-json` và vài module có sẵn (physics2d, animation, uielements, screencapture, unitywebrequest).
- Package có assembly `MCPForUnity.Runtime` (~2.000 dòng, chỉ helper) không giới hạn Editor → vào cả bản build. Chấp nhận khi prototype; **trước khi phát hành** phải loại khỏi bản build hoặc gỡ package.
- `.mcp.json` (gốc repo): server `UnityMCP` = `uvx --from mcpforunityserver==10.3.0 mcp-for-unity`, `DISABLE_TELEMETRY=true`. Cần `uv` (đã có 0.13.0) + Python ≥ 3.10.
- Phía Unity: chọn transport **stdio** trong `Window → MCP for Unity` (mặc định là HTTP); bật tắt telemetry trong cửa sổ đó nếu có.
- `tools/unity_test.py` vẫn giữ — dùng khi Unity tắt (và trong `verify`).
- Gỡ: xoá dòng trong `manifest.json` + `.mcp.json`.
- [tech-stack](../04-tech/tech-stack.md) mục "Hạ tầng & công cụ chung".
