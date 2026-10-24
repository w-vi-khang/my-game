# Brief 0070 — World 7k: thử GPU Resident Drawer + GPU occlusion culling (đo trên Mac; APK sẵn cho điện thoại)

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24 (Planner trả lời vướng mắc)

<!-- Không có task contract riêng: hạng mục đo/thử nhỏ, quyết định gốc 0029 mục 4 (Chốt). Dòng 7k ở design/04-tech/world-tech.md. -->

## Mục tiêu
Đo world có / không có GPU Resident Drawer (GRD) và GPU occlusion culling. Đo bằng scene WalkBench (nhân vật tự đi vòng quanh trại, log một dòng `WALKBENCH`):
- **trên Mac:** làm trong brief này;
- **trên điện thoại:** brief này build sẵn 3 APK; phần đo **chờ user cắm máy**, vì lúc viết brief `adb devices` không thấy máy.

Brief này **không đổi cấu hình phát hành** của project. GRD/occlusion chỉ bật tạm trong lúc build bản đo. Có bật hẳn hay không thì Planner/user quyết sau khi có số đo điện thoại (0029 mục 4).

## Bối cảnh cần đọc
- Quyết định: `design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md:40-42`. Bật GRD + Forward+, đo trên Mac và điện thoại khi có và không có culling. Không có lợi, hoặc không chạy được trên điện thoại → tắt và ghi kết quả.
- Dòng 7k: `design/04-tech/world-tech.md`. Tìm dòng `| 7k |`; cột "Xong khi": "Ghi số FPS/ms GPU hai trường hợp; không lợi hoặc không chạy trên điện thoại → tắt, ghi kết quả".
- Docs Unity 6000.4 / URP 17.4 (Planner đã tra):
  - Bật GRD ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-resident-drawer.html)):
    - Graphics settings → Shader Stripping → **BatchRendererGroup Variants = Keep All**;
    - URP Asset bật SRP Batcher, **GPU Resident Drawer = Instanced Drawing**;
    - Universal Renderer **Rendering Path = Forward+**.
  - Yêu cầu của GRD: API có compute shader (không có OpenGL ES); renderer là Mesh Renderer, không có MaterialPropertyBlock, không có MonoBehaviour có `OnBecameVisible` / `OnWillRenderObject`. Vật không thoả yêu cầu thì vẽ theo cách thường. [repo] `grep` không thấy `MaterialPropertyBlock`, `OnBecameVisible` hay `OnWillRenderObject` trong `client/Assets/Game`.
  - GPU occlusion culling ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-culling.html)): cần GRD; vật được xấp xỉ bằng hình cầu bao. Nếu không giảm được nhiều việc thì thời gian render có thể **tăng**.
  - API dùng trong code:
    - `UniversalRenderPipelineAsset.gpuResidentDrawerMode` (`GPUResidentDrawerMode`, get/set) và `gpuResidentDrawerEnableOcclusionCullingInCameras` (bool, get/set) ([docs](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.4/api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html)).
    - `UniversalRendererData.renderingMode` (`RenderingMode`, get/set) ([docs](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.4/api/UnityEngine.Rendering.Universal.UniversalRendererData.html)).
    - Enum `BatchRendererGroupStrippingMode` có `KeepAll` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rendering.BatchRendererGroupStrippingMode.html)).
    - `FrameTimingManager.CaptureFrameTimings()`, `FrameTimingManager.GetLatestTimings(uint, FrameTiming[])`, `FrameTimingManager.IsFeatureEnabled()`; `FrameTiming.gpuFrameTime` (ms; 0 nếu nền tảng không có) ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/FrameTimingManager.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/FrameTiming.html)).
    - Bản build release cần Player setting **Frame Timing Stats**: `PlayerSettings.enableFrameTimingStats`. Trên macOS Metal, GPU time có thể lớn hơn tổng thời gian khung.
- Cấu hình hiện tại [repo]:
  - Quality "PC" dùng `client/Assets/Settings/PC_RPAsset.asset` (Standalone). Renderer `PC_Renderer.asset:56` `m_RenderingMode: 2` = Forward+.
  - Quality "Mobile" dùng `Mobile_RPAsset.asset` (Android). Renderer `Mobile_Renderer.asset:48` `m_RenderingMode: 0` = Forward.
  - Cả hai RP asset: `m_GPUResidentDrawerMode: 0`, `m_GPUResidentDrawerEnableOcclusionCullingInCameras: 0` (dòng 86, 88).
  - `ProjectSettings/GraphicsSettings.asset:49` `m_BrgStripping: 0`.
  - `ProjectSettings/ProjectSettings.asset:154` `enableFrameTimingStats: 0`.
  - Android graphics API: Vulkan trước, GLES3 sau (`ProjectSettings.asset:537`).
- Bench có sẵn:
  - `client/Assets/Game/Client/WalkBench.cs`: khởi động `warmupSeconds = 3`, đo `measureSeconds = 40`; `Finish()` (dòng 228-252) dựng dòng `WALKBENCH …` với `avgFps`, `p50Ms`, `p99Ms`, `maxMs`, `holeFrames`…
  - `client/Assets/Game/Client/Editor/WalkBenchBuild.cs`: `EnsureScene(bool secondViewpoint, int targetPlots = 0)`, `Build(...)` cho Android (dòng 75-101: lưu/khôi phục ID ứng dụng trong `try/finally`).
  - SimView giữ nguyên `vSyncCount` / `targetFrameRate` mặc định, trừ khi bấm F1/F2 (`SimView.cs:214-219`).
- Mac có module `MacStandaloneSupport` (Mono). Tên sản phẩm `Viventa`.

## Quyết định đã chốt (không được đổi)
1. **Ba biến thể đo:**
   - **A** = như hiện tại (GRD tắt);
   - **B** = GRD Instanced Drawing, occlusion tắt;
   - **C** = GRD Instanced Drawing + occlusion bật.
   - Cả ba dùng cùng scene WalkBench, 1 điểm nhìn, không có ô ruộng thêm.
2. **Không đổi cấu hình lưu trong repo:**
   - Các thay đổi `gpuResidentDrawerMode`, occlusion, `m_BrgStripping`, `enableFrameTimingStats`, `renderingMode` của Mobile renderer chỉ đặt trong lúc build, rồi khôi phục giá trị cũ trong `finally` (như cách `Build` khôi phục ID ứng dụng).
   - Sau brief, `git diff` của `client/Assets/Settings/` và `client/ProjectSettings/` phải rỗng.
3. **Mac:**
   - Build StandaloneOSX (Mono, `BuildOptions.None`), mỗi biến thể một bản `client/Builds/walkbench-mac-{a,b,c}.app`. Thư mục `client/Builds/` đã có, các file APK cũ ở đó không được commit; kiểm `git status` để chắc `.app` không bị track.
   - Chạy mỗi bản bằng dòng lệnh: `client/Builds/walkbench-mac-X.app/Contents/MacOS/Viventa -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile <scratch>/mac-X.log`, chờ 60 s rồi tắt (`kill`).
   - Chạy theo thứ tự A, B, C, rồi A lần nữa để kiểm độ ổn định số đo.
4. **Android:**
   - Build `client/Builds/walkbench-gpu-{a,b,c}.apk` (cùng ID ứng dụng tạm `com.viventa.walkbench`, IL2CPP ARM64 như `Build` hiện có).
   - B và C đặt tạm Mobile renderer `renderingMode = RenderingMode.ForwardPlus`, A giữ Forward.
   - **Không cài, không đo** trong brief này, vì `adb devices` trống lúc viết brief. Nếu khi làm brief `adb devices` thấy máy thì vẫn **không** đo; ghi "có máy lúc HH:MM" để Planner giao bước đo riêng.
5. **Số đo thêm trong `WALKBENCH`** (code mới, chung cho mọi bản):
   - `gpuMsAvg` và `gpuMsP99` từ `FrameTimingManager`: mỗi khung đo gọi `CaptureFrameTimings()` + `GetLatestTimings(1, buf)`, lấy `gpuFrameTime` > 0.
   - `gpuSamples` = số mẫu > 0.
   - `grd=` (giá trị `gpuResidentDrawerMode` của RP asset đang chạy) và `occ=` (bool occlusion), đọc qua `GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset`.
   - `IsFeatureEnabled()` false → `gpuMsAvg=-1`.
   - Không đổi các trường cũ, không đổi thứ tự của chúng; chỉ chèn thêm một đoạn `| gpuMsAvg=… gpuMsP99=… gpuSamples=… grd=… occ=… |` ngay trước đoạn `pass30fps`.
6. **Không kết luận bật/tắt** trong brief này. Executor chỉ ghi số đo. Planner ghi kết luận Mac vào world-tech khi review; quyết định cuối chờ số đo điện thoại.
7. **(Trả lời vướng mắc, Planner 2026-10-24) Chấp nhận lệch brief của vòng 1:** sửa asset qua `SerializedObject` + reflection thay vì API URP (không thêm tham chiếu asmdef).
8. **Bản Mac không ra `WALKBENCH` — nguyên nhân nghi:** máy đặt `displaysleep 10` [run `pmset -g` lúc review], user AFK → màn hình tắt / khoá, player Metal không vẽ khung nên `Update` không chạy tới `Finish()`. Cách đo lại:
   - chạy mỗi bản kèm `caffeinate -u -d -t 90 &` ngay trước lệnh mở app (`-u` báo có người dùng → bật màn hình; `-d` giữ màn hình sáng) và mở bằng `open -n -W --args …` hoặc chạy trực tiếp file trong `Contents/MacOS/`;
   - trong `WithGpuVariant`, khi build Mac đặt tạm `PlayerSettings.runInBackground = true` (khôi phục trong `finally` như các giá trị khác);
   - nếu A **vẫn** không ra `WALKBENCH` sau 120 s: kiểm `ioreg -n Root -d1 | grep -i CGSSessionScreenIsLocked` và ghi kết quả; **bỏ phần đo Mac** (ghi "đo Mac chờ user ở máy"), không thử cách khác, làm tiếp bước 4–6.
9. **B crash (`GPUInstanceDataBufferUploader+GPUResources.LoadShaders` NullReference):** `GPUResidentDrawerResources` có trong `UniversalRenderPipelineGlobalSettings.asset` [repo], nên nghi tài nguyên GRD bị loại khỏi bản build B (C build sau B thì tạo được GRD). Kiểm một lần: build lại **riêng B sau A và C** (thêm hàm public `BuildMacGpuVariant(char)` để gọi từng biến thể qua MCP `execute_code` hoặc menu riêng), rồi chạy B. Vẫn crash → ghi "B crash trên Mac" + 3 dòng lỗi đầu, **không** sửa Global Settings, đo tiếp C và A.
10. **File bị build làm bẩn** (`PC_RPAsset.asset` m_Prefilter*, `UniversalRenderPipelineGlobalSettings.asset` +rid, `ProjectSettings.asset` batching `Standalone`, `UnityConnectSettings.asset`, `WalkBench.unity.meta`): sau mỗi lượt build `git checkout --` các file này là đúng; ghi danh sách file đã khôi phục vào Kết quả. Không sửa `EnsureScene`.
11. **APK (bước 4) vẫn build** dù Mac B crash: 3 APK là thứ cần cho đo điện thoại sau này. Cùng cách khôi phục file như QĐ 10.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Client/WalkBench.cs` (QĐ 5);
  - `client/Assets/Game/Client/Editor/WalkBenchBuild.cs` (thêm menu/hàm build biến thể, QĐ 3–4);
  - brief này (Kết quả trở xuống).
- Được tạo không commit: `client/Builds/walkbench-mac-*.app`, `client/Builds/walkbench-gpu-*.apk`, log trong scratch.
- Không được sửa:
  - `client/Assets/Settings/*`, `client/ProjectSettings/*` (chỉ đổi tạm trong lúc build, khôi phục xong; sau brief diff rỗng);
  - code game khác, `design/`.
  - Working tree có thay đổi chưa commit của phiên khác (`design/…`, `tools/terrain_catalog*`, `.claude/skills/local-terrain-gen/`, `.ai/skill-map.local.json`): không sửa, không stage, không khôi phục.

## Các bước
1. **WalkBench.cs** (QĐ 5):
   - Thêm `FrameTiming[] ftBuf = new FrameTiming[1]` và `List<double> gpuMs`. Trong vòng đo (chỗ đang thêm vào `frameMs`) gọi `FrameTimingManager.CaptureFrameTimings()`; nếu `GetLatestTimings(1, ftBuf) == 1 && ftBuf[0].gpuFrameTime > 0` thì thêm vào `gpuMs`.
   - Trong `Finish()` tính trung bình + p99 (dùng `SimBenchMeter.Percentile` như `p99Ms`), rồi chèn đoạn mới như QĐ 5.
   - Xong khi: biên dịch được; chạy scene WalkBench trong Editor một lần (Play, chờ ~45 s, đọc console) có dòng `WALKBENCH` chứa `gpuMsAvg=` (trong Editor có thể là -1 hoặc số; ghi lại).
2. **WalkBenchBuild.cs** (QĐ 2–4):
   - Thêm hàm `static void WithGpuVariant(char variant, bool android, Action build)`:
     - lưu giá trị cũ;
     - đặt `PlayerSettings.enableFrameTimingStats = true`;
     - B/C: `m_BrgStripping = (int)BatchRendererGroupStrippingMode.KeepAll` qua `SerializedObject` của `AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]`; RP asset của nền tảng (`PC_RPAsset` cho Mac, `Mobile_RPAsset` cho Android, nạp bằng `AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/…")`) `gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing`; C thêm `gpuResidentDrawerEnableOcclusionCullingInCameras = true`; Android B/C: `Mobile_Renderer` `renderingMode = RenderingMode.ForwardPlus`;
     - `build()`;
     - `finally` khôi phục mọi giá trị + `AssetDatabase.SaveAssets()`.
   - Thêm `BuildMac(string outputPath)`: `BuildTarget.StandaloneOSX`, `scenes = { ScenePath }`, gọi `EnsureScene(false)` trước.
   - Thêm 2 menu: `[MenuItem("Game/Build Mac WalkBench GPU A-B-C")]` build 3 `.app`; `[MenuItem("Game/Build APK WalkBench GPU A-B-C")]` build 3 APK. Mỗi menu gọi `WithGpuVariant` cho A, B, C.
   - Xong khi: chạy menu Mac qua MCP `execute_menu_item` → 3 `.app`. Sau đó `git diff --stat client/Assets/Settings client/ProjectSettings` rỗng.
3. **Đo Mac** (QĐ 3, sửa theo QĐ 8–9): build lại 3 bản Mac (đã có `runInBackground` tạm), chạy A, B, C, A theo QĐ 3 kèm `caffeinate` (QĐ 8); B crash thì build lại riêng B một lần (QĐ 9). Mỗi log: `grep WALKBENCH <log>` lấy dòng tổng kết. Nếu B/C có lỗi đỏ trong log (shader, BRG) → ghi nguyên văn 3 dòng lỗi đầu. Không cần ảnh chụp.
4. **Build APK** (QĐ 4): chạy menu APK → 3 file APK. Kiểm lại `git diff --stat client/Assets/Settings client/ProjectSettings` rỗng.
5. Ghi Kết quả:
   - bảng Mac 4 lần chạy × (`avgFps`, `p50Ms`, `p99Ms`, `gpuMsAvg`, `gpuMsP99`, `gpuSamples`, `grd`, `occ`, `holeFrames`);
   - thời gian build mỗi biến thể;
   - kích thước 3 APK;
   - dòng `adb devices` lúc kết thúc.

## Tiêu chí xong
- Có 4 dòng `WALKBENCH` Mac (A, B, C, A), dòng nào cũng có `gpuMsAvg=` và `grd=`/`occ=` đúng biến thể (A: grd tắt; B: grd bật, occ false; C: grd bật, occ true). Bản nào không ra dòng vì lý do ở QĐ 8–9 → ghi lý do + log thay cho dòng đó.
- Có 3 APK trong `client/Builds/`.
- `git diff --stat client/Assets/Settings client/ProjectSettings` rỗng. Sau khi chạy test PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- EditMode + PlayMode toàn bộ qua (582+14 = 596 / 267 trước brief), console 0 Error.
- `git status`: không có `.app` / `.apk` bị track.

## Test sau khi xong

### Phần cần test
- Số đo GPU mới trong `WALKBENCH` (bước 1): Play scene WalkBench trong Editor → dòng có `gpuMsAvg=`, các trường cũ còn đủ và đúng thứ tự (so với dòng mẫu trong `.ai/briefs/0065-*.md` Kết quả).
- Build biến thể + khôi phục cấu hình (bước 2, 4): sau mỗi menu → `git diff --stat client/Assets/Settings client/ProjectSettings` rỗng; `grd`/`occ` trong log Mac đúng biến thể.
- Đo Mac (bước 3): 4 dòng `WALKBENCH`. Hai lần A lệch `avgFps` bao nhiêu % thì ghi.
- Regression bench cũ: `WalkBenchBuild.BuildAndroid` không bị đổi. Đọc diff: hàm cũ giữ nguyên; scene `WalkBench.unity` sau `EnsureScene(false)` không đổi so với HEAD (`git diff --stat client/Assets/Game/Client/Scenes/WalkBench.unity` rỗng, hoặc chỉ đổi do EnsureScene tạo lại — ghi rõ).
- Regression chung: EditMode + PlayMode toàn bộ qua MCP.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` Error = 0.
2. Bước 1: mở `Assets/Game/Client/Scenes/WalkBench.unity` qua MCP `manage_scene`, Play ~45 s (Editor không focus làm khung chậm, xem ghi chú: nếu không ra dòng `WALKBENCH` sau 90 s thì dừng Play, ghi "Editor không ra kết quả" và dựa vào bản build Mac ở bước 3), `read_console` lọc `WALKBENCH`.
3. Bước 2: `execute_menu_item` "Game/Build Mac WalkBench GPU A-B-C"; `git diff --stat client/Assets/Settings client/ProjectSettings`.
4. Bước 3: chạy 4 lần như QĐ 3 (mỗi lần ≤ 60 s, chạy nền, đợi xong rồi `kill`).
5. Bước 4: `execute_menu_item` "Game/Build APK WalkBench GPU A-B-C" (chạy nền nếu MCP hết giờ chờ; theo dõi `read_console` "WalkBench build:"); `git diff --stat …` lần nữa.
6. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
7. Nếu build APK làm bẩn file: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`. `git status --short` cuối cùng.

Ghi giờ bắt đầu/kết thúc vào Kết quả.

### User tự test
Không (phần đo điện thoại là bước riêng khi user cắm máy — Planner giao sau).

## Dừng và chuyển lại Opus khi
- (Vòng 2) Các trường hợp ở QĐ 8–9 **không** còn là điều kiện dừng: ghi kết quả rồi làm tiếp.
- Một API ở mục Bối cảnh không tồn tại hoặc khác chữ ký (vd không có `RenderingMode.ForwardPlus`, `m_BrgStripping` không có trong SerializedObject).
- Bản Mac B/C không chạy được (crash, không ra dòng `WALKBENCH` sau 60 s) → ghi log, dừng ở đó; không thử cách khác.
- Không khôi phục được cấu hình (diff `Settings/` / `ProjectSettings/` không rỗng sau `finally`).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt"; cùng một lỗi sửa 2 lần vẫn chưa qua; phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit (vòng 2): cb506a8 (code vòng 1: 050d905)
Chạy bằng: claude-sonnet-5-5 · medium
Giờ vòng 2: bắt đầu 13:22, kết thúc 13:43.
Code: `WalkBench.cs` (vòng 1) + `WalkBenchBuild.cs`: thêm tạm `runInBackground=true` cho Mac trong `WithGpuVariant` (khôi phục ở finally), `BuildMacGpuVariant(char)` + `BuildMacGpuA/B/C()` (cho `-executeMethod`). Commit vòng 2: xem `git log` ("brief 0070: parameterless Mac GPU variant…"). Vẫn dùng SerializedObject/reflection (QĐ 7).

Lưu ý quy trình: lúc bắt đầu Editor đang TẮT (MCP không kết nối), nên build Mac chạy bằng Unity `-batchmode -executeMethod` (thứ tự A, C, B → QĐ 9). Giữa chừng Editor được mở lại (phiên khác/user), nên build APK + test chạy qua MCP.

### Đo Mac (M1 MacBookPro17,1, Metal, 1920x1080 cửa sổ, caffeinate, runInBackground=true; vsync nên trần ~60 FPS)
| Lần | avgFps | p50Ms | p99Ms | maxMs | gpuMsAvg | gpuMsP99 | gpuSamples | grd | occ | holeFrames |
|---|---|---|---|---|---|---|---|---|---|---|
| A | 59.4 | 16.67 | 18.55 | 42.35 | 9.80 | 12.10 | 2378 | Disabled | False | 0 |
| B | 58.4 | 16.67 | 19.81 | 515.57 | 9.90 | 12.07 | 2336 | InstancedDrawing | False | 0 |
| C | 59.6 | 16.67 | 17.69 | 26.09 | 10.55 | 12.79 | 2384 | InstancedDrawing | True | 0 |
| A (lại) | 56.7 | 16.67 | 20.46 | 941.41 | 10.27 | 15.93 | 2269 | Disabled | False | 0 |
- Hai lần A lệch avgFps 4,6% (59.4 vs 56.7), gpuMsAvg lệch 4,8%; maxMs 942 ms ở A lần 2 và 516 ms ở B là hitch đơn lẻ (có thể do máy nền). Chênh A/B/C nằm trong nhiễu đo; không thấy lợi rõ của GRD/occlusion trên Mac ở cảnh này. Không kết luận (QĐ 6).
- **B không crash** lần này (QĐ 9: build B sau A, C). Không có lỗi đỏ nào ở dòng WALKBENCH. Vòng 1 B crash là do build B trước khi Global Settings/GRD resources được import (nghi; không kiểm chứng thêm). A ra WALKBENCH bình thường (QĐ 8: caffeinate + runInBackground hiệu quả; không cần kiểm CGSSessionScreenIsLocked).
- Log: scratch `mac-{a,b,c,a2}.log` (không commit).
- Thời gian build Mac (batchmode): A 126 s, C 108 s, B 20 s (tổng gồm khởi động Unity).

### APK (build qua MCP menu, không cài, không đo — QĐ 4)
- `client/Builds/walkbench-gpu-a.apk` 35 313 769 B; `-b.apk` 36 179 973 B; `-c.apk` 36 355 323 B. Không track (git ls-files rỗng).
- `adb devices` lúc 13:42: không thấy máy.

### File bị build làm bẩn, đã `git checkout --` (QĐ 10)
`PC_RPAsset.asset`, `Mobile_RPAsset.asset` (m_Prefilter*), `Mobile_Renderer.asset` (reserialize AssetVersion 2→3, xrSystemData…), `UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings.asset`, `UnityConnectSettings.asset`, `WalkBench.unity.meta`, `EditorSettings.asset` (sau PlayMode). Sau cùng: `git diff --stat client/Assets/Settings client/ProjectSettings` rỗng; `WalkBench.unity` không đổi.
Ghi cho Planner: Mobile_Renderer bị reserialize (AssetVersion 3) mỗi lần build APK — hành vi của Unity, không phải giá trị GRD.

## Kết quả test
- Bước 1 (gpuMsAvg trong WALKBENCH): đạt — Editor vòng 1 (gpuMsAvg=-1 do frames=0) và bản Mac có số thật (xem bảng), thứ tự trường đúng.
- Build biến thể + khôi phục cấu hình: đạt — grd/occ đúng biến thể trong log Mac; diff Settings/ProjectSettings rỗng sau khôi phục.
- Đo Mac: đạt — 4 dòng WALKBENCH; A lệch 4,6%.
- Regression: BuildAndroid cũ không đổi; scene không đổi (diff rỗng).
- EditMode 596/596 đạt; PlayMode 267/267 đạt (MCP); console 0 Error.
- `git status`: không có .app/.apk bị track.

---
<!-- PLANNER ghi khi review. -->

## Review
- Đối chiếu Model thực thi: Sonnet · medium = `Chạy bằng` claude-sonnet-5-5 · medium (cả 2 vòng). Đạt.
- Diff code (`050d905` + `f166e41`): `WalkBench.cs` chèn đoạn `gpuMsAvg … occ` trước `pass30fps`, trường cũ giữ nguyên thứ tự; `WalkBenchBuild.cs` thêm `WithGpuVariant` (khôi phục mọi giá trị trong `finally`, gồm `runInBackground` mới ở vòng 2), `BuildMac`, `BuildMacGpuVariant` + 3 hàm cho `-executeMethod`, 2 menu; `BuildAndroid` cũ không đổi. Trong Phạm vi. QĐ 7 (SerializedObject + reflection) đã chấp nhận.
- Tiêu chí xong: 4 dòng `WALKBENCH` Mac có `gpuMsAvg`, `grd`/`occ` đúng biến thể — đạt; 3 APK có, không track — đạt; `git diff --stat 69696d0 HEAD -- client/Assets/Settings client/ProjectSettings` rỗng [run] — đạt; EditMode 596 + PlayMode 267 — đạt (theo Kết quả, không chạy lại).
- Lệch quy trình chấp nhận được: build Mac bằng `-batchmode -executeMethod` khi Editor tắt (cùng code, cùng `WithGpuVariant`).
- Kết luận Mac (ghi ở world-tech dòng 7k): GPU 9,8–10,6 ms ở cả A/B/C, FPS bị vsync chặn ~60; chênh nằm trong nhiễu → không thấy lợi trên Mac. Quyết định cuối (0029 mục 4) chờ đo điện thoại 3 APK `walkbench-gpu-{a,b,c}.apk`.
- Ghi nhận: build APK reserialize `Mobile_Renderer.asset` (AssetVersion 2→3) — hành vi Unity, đã checkout; không cần sửa.
- Giờ: Executor ~41 phút (2 vòng); world-tech + roadmap đã ghi.
- Kết luận: đã duyệt
