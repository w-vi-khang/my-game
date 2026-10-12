# Công nghệ

> Trạng thái: Nháp
> Liên quan: [Thành phần](components.md) · [Kiến trúc](architecture.md) · [Rủi ro](risks.md) · [Quyết định client](../decisions/0001-client-unity.md)

Công nghệ theo từng thành phần trong [components.md](components.md). Thành phần mới → thêm một mục `##` cùng cấu trúc.
Docs tra ngày 2026-10-12, đúng bản 6000.4 trừ khi ghi khác. Lựa chọn là đề xuất của AI [ý tưởng] cho tới khi user đồng ý.

## Client (Unity)
<!-- Version: 6000.4.8f1 (đã cài). Render pipeline (URP/HDRP/Built-in) — chưa chọn, cần quyết định. Nền tảng build. Package: bảng package | dùng cho | lý do — chỉ thêm khi thiết kế cần (Input System, Cinemachine, Addressables...?). Khi tạo project: Asset Serialization = Force Text; Version Control = Visible Meta Files; tạo trong thư mục client/. -->
- **Version:** Unity 6000.4.8f1 ([0001](../decisions/0001-client-unity.md)).
- **Render pipeline:** [ý tưởng] **URP**, tạo project từ template Universal 3D — xem đề xuất [0005](../decisions/0005-render-pipeline.md).
  - URP chạy "from mobile to high-end consoles and PCs" ([docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/urp-introduction.html)); dành cho dự án cần mở rộng trên mọi nền tảng, hiệu quả trên GPU mobile ([docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/choose-a-render-pipeline.html)).
  - Manual 6000.4 vẫn gọi Built-in là "the default render pipeline" ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/render-pipelines.html)) → phải chủ động chọn template Universal 3D ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/creating-a-new-project-with-urp.html)).
  - Từ Unity 6.5, Built-in bị deprecated ([upgrade guide 6.5](https://docs.unity.com/en-us/engine/6000.5/manual/upgrade-guides/upgrade-guide-unity65)) → chọn Built-in sẽ phải chuyển sau.
- **Nền tảng build:** PC (Windows, macOS), Android, iOS; sau phát hành thêm Linux Dedicated Server cho game server.

**Package** (chỉ những gì thiết kế hiện tại cần):

| Package | Version | Dùng cho | Milestone | Nguồn |
|---|---|---|---|---|
| Input System | 1.19 | Điều khiển PC (bàn phím, chuột) + cảm ứng Android/iOS + tay cầm. Input Manager cũ là "legacy feature and not recommended for new projects" | M1 | [pack-safe 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/pack-safe.html), [thiết bị hỗ trợ](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/SupportedDevices.html), [Input Manager](https://docs.unity3d.com/6000.4/Documentation/Manual/class-InputManager.html) |
| Burst | 1.8 | Biên dịch job dựng mesh chunk voxel ra mã native | M1 | [pack-safe 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/pack-safe.html), [Burst 1.8](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/index.html) |
| Collections, Mathematics | core 6.4 | Native container, toán cho job | M1 | [pack-core 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/pack-core.html) |
| uGUI (Unity UI) | core 2.0 | UI runtime, HUD | M1 | [so sánh UI 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/UI-system-compare.html) — runtime: khuyến nghị uGUI, thay thế UI Toolkit |
| Addressables | 3.1 | Tải nội dung từ xa (map theo mùa) — chỉ khi chọn tải nội dung thay vì cập nhật app | M3+ (chưa quyết) | [Addressables 3.1](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html) |
| Dedicated Server | 2.0 | Build game server headless; ký hiệu `UNITY_SERVER` | Sau phát hành | [build Dedicated Server](https://docs.unity3d.com/6000.4/Documentation/Manual/dedicated-server-build.html), [pack-safe](https://docs.unity3d.com/6000.4/Documentation/Manual/pack-safe.html) |
| Netcode | _(chưa chọn)_ | PvP thời gian thực | M3 (prototype) | Xem bên dưới |

**Voxel world — dựng mesh chunk** [ý tưởng]
- **Bề mặt mịn liền** (user, 2026-10-12): mỗi ô lưu mật độ + loại vật liệu thay vì chỉ "có/không có khối"; dựng bề mặt mịn từ lưới bằng một thuật toán kiểu marching cubes / surface nets / dual contouring (chọn khi prototype M1, cân nhắc chất lượng cạnh, số tam giác, đường nối giữa chunk). Vật liệu trộn theo đỉnh, texture dán 3 hướng (triplanar). Đào/đặt = đổi mật độ các ô. Logic (chiếm chỗ, tìm đường) vẫn theo ô. Ước lượng voxel world tăng +10–15 ngày ([roadmap](../05-roadmap.md)).
- Tự viết meshing, chạy bằng C# Job System + Burst; ghi mesh qua `Mesh.AllocateWritableMeshData` → `Mesh.MeshData` → `Mesh.ApplyAndDisposeWritableMeshData`. API này cho phép truy cập MeshData "from any thread", nên xin nhiều MeshData trong một lần gọi (hợp với gom nhiều chunk bị đổi) ([AllocateWritableMeshData](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.AllocateWritableMeshData.html), [MeshData](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.MeshData.html), [ApplyAndDispose](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.ApplyAndDisposeWritableMeshData.html), [Job System](https://docs.unity3d.com/6000.4/Documentation/Manual/job-system.html)).
- Lưu ý: ApplyAndDispose kiểm tra dữ liệu rất ít — dữ liệu sai có thể lỗi khó tìm → cần test cho meshing.
- Burst trên mobile: Android cần Android NDK; iOS cần Mac có Xcode; thiếu toolchain thì build chạy không có Burst ([docs](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/building-projects.html)).
- Không dùng Entities (ECS) dù đã là core package ở 6.4 ([pack-core](https://docs.unity3d.com/6000.4/Documentation/Manual/pack-core.html)): thêm một mô hình lập trình cho một người học; Jobs + Burst đủ cho meshing. Xem lại nếu M1 cho thấy số sinh vật lớn bị chậm.
- Phương án khác: asset voxel có sẵn — chưa tra (tính năng, giá) [chưa xác minh], xem đề xuất C3 trong [roadmap](../05-roadmap.md#kiểm-tra-phạm-vi).

**Vật thể trên lưới** [ý tưởng] — theo [art style](../03-art-audio/art-style.md): sinh vật, cây, vật phẩm là model mịn riêng (không dựng từ khối); mỗi loại có **khung chiếm chỗ** (danh sách ô voxel) trong bảng dữ liệu. Đặt/xoá vật thể = đánh dấu/bỏ đánh dấu các ô trong lưới chiếm chỗ; tìm đường đọc lưới này. Địa hình hiển thị **bề mặt mịn liền** (user, 2026-10-12 — [art style](../03-art-audio/art-style.md)): xem mục dựng mesh ở trên.

**Tìm đường sinh vật** [ý tưởng]
- Đề xuất: **tự viết A\* trên lưới voxel**, C# thuần.
  - Lưới voxel đã rời rạc; kiểu di chuyển theo loài (đi bộ, bay, bơi — [cơ chế](../02-gameplay/mechanics.md#tìm-đường-sinh-vật--đình-trệ)) chỉ là luật đi qua ô.
  - Chạy được trong mô phỏng tua nhanh và kiểm thử, không cần scene ([kiến trúc](architecture.md#client-unity)).
  - Đổi khối → chỉ tính lại vùng bị đổi.
- Phương án khác: AI Navigation 2.0 — `NavMeshSurface.UpdateNavMesh` dựng lại bất đồng bộ phần NavMesh ở vùng thay đổi; `NavMeshBuilder.UpdateNavMeshDataAsync` cập nhật tăng dần nhưng phải đưa lại toàn bộ hình học đầu vào mỗi lần; bake lúc chạy nên dùng tile nhỏ để giảm bộ nhớ ([NavMeshSurface API](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html), [UpdateNavMeshDataAsync](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AI.NavMeshBuilder.UpdateNavMeshDataAsync.html), [NavMeshSurface manual](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshSurface.html)). Docs không có số liệu thời gian → chi phí trên địa hình voxel [chưa xác minh].
- Kiểm chứng ở M1: profile A\* khi đổi khối liên tục với số sinh vật mục tiêu (rủi ro tìm đường trong [risks](risks.md)).

**Netcode** (quyết định ở M3, khi prototype trận mạng)
- Multiplayer Center của Unity xếp: co-op casual → Netcode for GameObjects; game đối kháng cạnh tranh (FPS, đối kháng, battle royale) → Netcode for Entities ([docs](https://docs.unity.com/en-us/multiplayer/multiplayer-center)).
- Bản hiện có: Netcode for GameObjects 2.12 ([docs](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.12/manual/index.html)); Netcode for Entities 1.13, "server authoritive with client prediction framework" ([docs](https://docs.unity3d.com/Packages/com.unity.netcode@1.13/manual/index.html)).
- Mâu thuẫn cần cân nhắc: PvP thời gian thực, server quyết định → gần nhóm "cạnh tranh" (Netcode for Entities), nhưng client không dùng ECS. [ý tưởng] Logic chiến đấu C# thuần tách khỏi hiển thị giúp đổi lựa chọn sau mà không viết lại luật.

**Thiết lập project khi tạo (M1)**
- Tạo trong `client/` ([0002](../decisions/0002-repo-layout.md)).
- Asset Serialization = Force Text — mặc định ở 6000.4 ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/class-EditorManager.html)); có thêm tuỳ chọn "Reduce version control noise".
- Version Control Mode = Visible Meta Files — mặc định, dùng cho git ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/class-VersionControlSettings.html)).
- Kiểm tra "Active Input Handling" = Input System sau khi tạo — giá trị mặc định ở 6000.4 [chưa xác minh].

## Thành phần khác
<!-- Điền sau khi components.md xác định cần thành phần nào (server, công cụ, dịch vụ...). Mỗi thành phần: ngôn ngữ/framework hoặc dịch vụ, hosting, chi phí ước tính (có nguồn), lý do chọn. -->
- **Backend** (từ M2): dịch vụ có sẵn, chưa chọn — ứng viên trong [0003](../decisions/0003-server.md) (Unity Gaming Services, Nakama). Cần tra giá trước M2.
- **Game server** (sau phát hành): build Dedicated Server từ project client (bảng package ở trên); hosting chưa chọn — Unity đã ngừng Multiplay Game Server Hosting ([risks](risks.md)).

## Hạ tầng & công cụ chung
<!-- Quản lý mã nguồn (git + LFS), CI/build, quản lý task, công cụ art/âm thanh, môi trường (dev/prod). -->
- **Mã nguồn:** git, một repo ([0002](../decisions/0002-repo-layout.md)). Đã có [`.gitignore`](../../.gitignore) (bỏ `client/Library`, `Temp`, `Obj`, `Build(s)`, `Logs`, `UserSettings`...) và [`.gitattributes`](../../.gitattributes) (YAML Unity `eol=lf`; ảnh, model, âm thanh, font qua **Git LFS**).
- **Merge scene/prefab:** [ý tưởng] cấu hình UnityYAMLMerge (Smart Merge) cho git khi bắt đầu M1 ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/SmartMerge.html)) — làm một người nên ít cần, chưa ưu tiên.
- **CI/build, quản lý task, công cụ art/âm thanh, môi trường dev/prod:** _(chưa có)_
