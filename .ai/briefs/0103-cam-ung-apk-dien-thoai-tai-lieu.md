# Brief 0103 — Cảm ứng (c): build APK chơi thử, chạy trên điện thoại, cập nhật tài liệu

> Trạng thái: cần Opus
> Cấp: Lớn
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-03

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc). -->

## Mục tiêu
Phần (c), phần cuối, của task [m1-dieu-khien-cam-ung](../tasks/m1-dieu-khien-cam-ung.md). Brief 0101 (a) và 0102 (b) đã làm xong code cảm ứng. Brief này làm:
1. Thêm script build APK **chơi thử** từ scene SimView gốc, không phải bản bench. App tên "Viventa Play", chỉ màn hình ngang.
2. Thêm một dòng log `TOUCHUI` ghi toạ độ các nút cảm ứng, để bấm nút qua `adb shell input tap` cho đúng chỗ.
3. Build, cài lên điện thoại, chạy, đọc logcat. Chụp màn hình lúc chưa mở bảng và lúc đang mở túi. Kiểm xem bảng IMGUI (Cài đặt) có nhận chạm không.
4. Cập nhật tài liệu: controls-ui (cột Cảm ứng), glossary, ideas (phần ngoài phạm vi), world-tech dòng 7m, roadmap (giờ thực tế).

## Bối cảnh cần đọc
- Task contract: `.ai/tasks/m1-dieu-khien-cam-ung.md`, các mục "Tiêu chí chấp nhận" (dòng "Chạy trên điện thoại", "Tài liệu") và "Ngoài phạm vi".
- **Script build cũ để chép theo:** `client/Assets/Game/Client/Editor/SimBenchBuild.cs:48-77` (`BuildAndroid`). Script này đổi `ApplicationIdentifier` tạm thời, đặt IL2CPP + ARM64, gọi `BuildPipeline.BuildPlayer` và khôi phục trong `finally`.
  - Đường dẫn trong `BuildPlayerOptions.locationPathName` tính từ thư mục `client/`. `client/Builds/` đã bị git bỏ qua.
- **TouchControls** (`client/Assets/Game/Client/TouchControls.cs`):
  - `public void Feed(IReadOnlyList<TouchSample> input, float screenWidth, float dpi)` ở `:153`. Dòng `LayoutButtons(dpi);` ở `:159`.
  - `void ClearOutputs()` ở `:97-107`, kết thúc bằng `wasActive = false;`.
  - `void AddButton(RectTransform root, int id, string name, string key)` ở `:311`. Tên GameObject = `name`. Thứ tự gọi ở `:298-306`: "Jump Button", "Action Button", "Collect Button", "Mode Default Button", "Mode Dig Button", "Mode Till Button", "Mode Rain Button", "Bag Button", "Back Button".
  - `Btn.Rect` là hình chữ nhật nút theo px màn hình Unity, gốc ở góc dưới-trái (`Place` ở `:224-235`).
  - Dòng `using` ở đầu file `:1-8`, chưa có `System.Text`.
  - `WantsTouch => Application.isMobilePlatform || ForceOn` ở `:43`: trên điện thoại giao diện cảm ứng tự bật.
- **Test PlayMode cảm ứng:** `client/Assets/Game/Tests/Client/TouchControlsPlayTests.cs`. `SetUp` ở `:25-62` (gồm `touch.readTouches = false; TouchControls.SetForceOn(true);`), `Feed(params TouchSample[] s)` ở `:78`, `Dpi = 160f`. Test cuối là `JumpButtonLayout` ở `:211-225`.
- **Bảng Cài đặt là IMGUI** (`client/Assets/Game/Client/SettingsView.cs`): `GUI.Toolbar` các tab Chung / Hướng dẫn / Điều khiển ở `:192`, nút Đóng ở `:202`.
  - Project đặt Active Input Handling = Input System Package (`client/ProjectSettings/ProjectSettings.asset` `activeInputHandler: 1`). Theo docs, Input System không cấp input cho IMGUI [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html#immediate-mode-gui). Vì vậy chưa biết IMGUI có nhận chạm trên điện thoại không → bước 6 kiểm.
  - Nút Huỷ (0102) mở Cài đặt khi không còn gì để đóng, và đóng Cài đặt khi đang mở.
- **Điện thoại:** Xiaomi 24095PCADG. `ADB` = `adb` trên PATH, hoặc `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`.
  - Có thể nối qua USB, serial `PFQCRWVWR46HU4GQ`.
  - Có thể nối qua Wi‑Fi, địa chỉ đổi theo mạng. Lần đo gần nhất [run 2026-11-03, Planner]: `adb mdns services` ra `adb-PFQCRWVWR46HU4GQ _adb._tcp 192.168.1.27:5555` và `_adb-tls-connect._tcp 192.168.1.27:35435`; `adb devices` lúc đó rỗng.
  - Qua Wi‑Fi có thể bị chặn cài (`INSTALL_FAILED_USER_RESTRICTED`).
  - **Không dùng `192.168.1.5:6060`**: đó là Android TV.
- Thư mục ảnh ngoài repo: `/Users/macvn/Documents/User/khang/Game/art-test/shots/` (đã có, dùng cho ảnh các brief trước).
- **Tài liệu cần sửa** (dòng đã đọc ngày 2026-11-03):
  - `design/02-gameplay/controls-ui.md:36-60`: bảng "Hành động | Bàn phím-chuột | Cảm ứng | Tay cầm | Milestone".
  - `design/glossary.md:107` (Tâm nhắm), `:109` (Hành động chính).
  - `design/ideas.md`: mục cuối là `## Nước — vật nổi trôi theo hướng chảy (user, 2026-11-02, M2)`.
  - `design/04-tech/world-tech.md:221`: dòng `| 7m | …`, cột cuối kết thúc bằng `… Executor ~18 phút) |`.
  - `design/05-roadmap.md`: bảng giờ thực tế (tiêu đề `| Bước | Ước lượng (ngày-người) | Thực tế | Ghi chú |` ở `:61`). Dòng cuối bảng bắt đầu bằng `| Sửa lỗi mép cỏ hở ở góc lõm (user báo 2026-10-24, thuộc world 7b4)` (`:138`).
- Giờ Executor của các brief trước: 0101 ~18 phút (`.ai/briefs/0101-cam-ung-joystick-camera-nhay.md`, Review), 0102 ~30 phút (`.ai/briefs/0102-cam-ung-tam-nham-nut-bang.md:388`).
- Docs Unity 6000.4:
  - [PlayerSettings.defaultInterfaceOrientation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/PlayerSettings-defaultInterfaceOrientation.html) (`public static UIOrientation defaultInterfaceOrientation`) và [UIOrientation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/UIOrientation.html) (`UIOrientation.LandscapeLeft`).
  - [PlayerSettings.productName](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/PlayerSettings-productName.html).
  - [BuildPipeline.BuildPlayer](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html).
  - [Application.logMessageReceived](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application-logMessageReceived.html).
  - [Screen.safeArea](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Screen-safeArea.html), [Screen.dpi](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Screen-dpi.html).
  - [Dòng lệnh Editor](https://docs.unity3d.com/6000.4/Documentation/Manual/EditorCommandLineArguments.html) (`-batchmode -quit -projectPath -executeMethod -buildTarget -logFile`).
  - adb: [docs](https://developer.android.com/tools/adb).

## Quyết định đã chốt (không được đổi)
1. **APK chơi thử build từ `Assets/Game/Client/Scenes/SimView.unity`**, không sao chép scene, không sửa scene.
   - Gói `com.viventa.play`, tên app "Viventa Play", file `Builds/play.apk`.
   - Đổi tạm `ApplicationIdentifier`, `productName` và `defaultInterfaceOrientation`, rồi khôi phục trong `finally` như `SimBenchBuild`.
   - Lý do: không đè lên các app bench đã cài, và dễ phân biệt trên màn hình chính.
2. **Chỉ màn hình ngang (`UIOrientation.LandscapeLeft`) cho APK này.** Bố cục cảm ứng giả định màn hình ngang (joystick nửa trái, camera nửa phải). Không đổi `ProjectSettings` cố định. [ý tưởng, Planner; ghi trong controls-ui]
3. **Dòng `TOUCHUI` chỉ log khi bố cục đổi**, tức khi `Screen.width`, `Screen.height`, dpi hoặc `Screen.safeArea` khác lần trước. `ClearOutputs` đặt lại để lần bật sau log lại. Không log mỗi khung.
4. **Toạ độ bấm qua adb:** `x_adb = x`, `y_adb = H − y`, với `H` = chiều cao `screen=` trong `TOUCHUI`.
   - Nếu ảnh `screencap` có kích thước khác `screen=` thì nhân theo tỉ lệ (`x_adb = x · Wảnh / W`, `y_adb = (H − y) · Hảnh / H`).
5. **Không sửa hiệu năng, không đo FPS** (user gác ngày 2026-11-01). Không sửa lỗi phát hiện trên điện thoại trong brief này: chỉ ghi lại vào Kết quả, Planner quyết định sau. Kể cả khi IMGUI không nhận chạm.
6. **Để app trên máy sau khi xong** (không gỡ `com.viventa.play`) để user chơi thử. Không gỡ, không đụng các app khác trên máy.
7. **Không dùng `svc power stayon` hay đổi cài đặt máy.** Màn hình khoá có mật khẩu → dừng, không nhập mật khẩu.
8. **Chữ trong tài liệu** chép nguyên văn từ bước 8–12. Chỗ `<…>` thay bằng số / kết quả thật.

## Phạm vi
- **Được sửa/tạo:**
  - `client/Assets/Game/Client/Editor/PlayBuild.cs` (mới) + `.meta`.
  - `client/Assets/Game/Client/TouchControls.cs`: chỉ thêm `LogLayout` và các trường của nó, một dòng gọi trong `Feed`, một dòng trong `ClearOutputs`, `using System.Text;`.
  - `client/Assets/Game/Tests/Client/TouchControlsPlayTests.cs`: chỉ thêm một test method.
  - `client/Assets/Game/Tests/Client/MinimapCornerPlayTests.cs`: **Planner đã sửa sẵn** trong working tree (trả lời Vướng mắc ngày 2026-11-03). Executor không sửa thêm, chỉ commit cùng code.
  - `design/02-gameplay/controls-ui.md`, `design/glossary.md`, `design/ideas.md`, `design/04-tech/world-tech.md`, `design/05-roadmap.md`: chỉ các chỗ ở bước 8–12.
  - Brief này: mục Kết quả trở xuống.
- **Không được sửa:**
  - Mọi file code khác, `GameControls.inputactions`, `SimView.unity`.
  - `client/ProjectSettings/`, `client/Assets/Settings/`. Nếu build làm bẩn các file này thì `git checkout --` lại (bước 4).
  - Test hiện có. Test cũ fail → dừng, ghi "cần Opus".
  - Trạng thái tài liệu (`> Trạng thái:`), `design/README.md`, task contract.

## Các bước
1. **Ghi giờ bắt đầu** (`date +%H:%M`) vào Kết quả.

2. **`TouchControls.cs`: log `TOUCHUI`** (QĐ 3).
   - Thêm `using System.Text;` vào nhóm `using` đầu file.
   - Thêm các trường sau ngay dưới dòng `FollowCamera follow;`:
     ```csharp
     int layoutLogW = -1, layoutLogH = -1;
     float layoutLogDpi = -1f;
     Rect layoutLogSafe;
     ```
   - Trong `Feed`, ngay sau dòng `LayoutButtons(dpi);`, thêm `LogLayout(dpi);`.
   - Trong `ClearOutputs`, ngay trước `wasActive = false;`, thêm `layoutLogW = -1;`.
   - Thêm method sau, đặt ngay dưới `void LayoutButtons(float dpi) { … }`:
     ```csharp
     /// <summary>One TOUCHUI line per layout change (brief 0103): button centers in Unity screen px, origin bottom-left; adb tap y = screen height − y.</summary>
     void LogLayout(float dpi)
     {
         Rect s = Screen.safeArea;
         if (Screen.width == layoutLogW && Screen.height == layoutLogH && dpi == layoutLogDpi && s == layoutLogSafe) return;
         layoutLogW = Screen.width; layoutLogH = Screen.height; layoutLogDpi = dpi; layoutLogSafe = s;
         var sb = new StringBuilder("TOUCHUI screen=");
         sb.Append(Screen.width).Append('x').Append(Screen.height).Append(" dpi=").Append(Mathf.RoundToInt(dpi))
           .Append(" safe=").Append(Mathf.RoundToInt(s.xMin)).Append(',').Append(Mathf.RoundToInt(s.yMin))
           .Append(',').Append(Mathf.RoundToInt(s.xMax)).Append(',').Append(Mathf.RoundToInt(s.yMax));
         foreach (var b in buttons)
             sb.Append(' ').Append(b.Rt.name.Replace(" Button", "").Replace(' ', '_').ToLowerInvariant())
               .Append('=').Append(Mathf.RoundToInt(b.Rect.center.x)).Append(',').Append(Mathf.RoundToInt(b.Rect.center.y));
         Debug.Log(sb.ToString());
     }
     ```
   - Xong khi: compile không lỗi (`read_console` không có error).

3. **`PlayBuild.cs` (mới)**, đặt cạnh `SimBenchBuild.cs`, nội dung:
   ```csharp
   using System;
   using System.IO;
   using UnityEditor;
   using UnityEditor.Build;
   using UnityEditor.Build.Reporting;
   using UnityEngine;

   namespace Game.Client.Editor
   {
       /// <summary>
       /// APK chơi thử trên điện thoại (task cảm ứng (c), brief 0103): scene SimView gốc, gói com.viventa.play, tên "Viventa Play", chỉ màn hình ngang.
       /// Menu "Game/Build APK Play"; dòng lệnh -executeMethod Game.Client.Editor.PlayBuild.BuildAndroid.
       /// </summary>
       public static class PlayBuild
       {
           const string ScenePath = "Assets/Game/Client/Scenes/SimView.unity";
           const string OutputPath = "Builds/play.apk";
           const string TempAppId = "com.viventa.play";
           const string TempProductName = "Viventa Play";

           [MenuItem("Game/Build APK Play")]
           public static void BuildAndroid()
           {
               var android = NamedBuildTarget.Android;
               string oldId = PlayerSettings.GetApplicationIdentifier(android);
               string oldName = PlayerSettings.productName;
               var oldOrientation = PlayerSettings.defaultInterfaceOrientation;
               try
               {
                   PlayerSettings.SetApplicationIdentifier(android, TempAppId);
                   PlayerSettings.productName = TempProductName;
                   // Touch layout assumes landscape: joystick on the left half, camera swipe on the right half.
                   PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                   PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
                   PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                   Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                   var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                   {
                       scenes = new[] { ScenePath },
                       locationPathName = OutputPath,
                       target = BuildTarget.Android,
                       options = BuildOptions.None,
                   });
                   Debug.Log($"Play build: {report.summary.result}, {report.summary.totalSize} byte, {report.summary.totalTime} → {OutputPath}");
                   if (report.summary.result != BuildResult.Succeeded)
                       throw new Exception($"Build failed: {report.summary.result}");
               }
               finally
               {
                   PlayerSettings.SetApplicationIdentifier(android, oldId);
                   PlayerSettings.productName = oldName;
                   PlayerSettings.defaultInterfaceOrientation = oldOrientation;
                   AssetDatabase.SaveAssets();
               }
           }
       }
   }
   ```
   - Xong khi: compile không lỗi; menu "Game/Build APK Play" có trong Editor.

4. **Test + build.**
   - Viết test ở "Phần cần test" dòng 1.
   - Chạy EditMode rồi PlayMode toàn bộ (mục "Lệnh executor chạy").
   - Build APK:
     - Editor đang mở và MCP kết nối: `execute_menu_item` "Game/Build APK Play". MCP hết giờ chờ thì theo dõi `read_console` tới dòng `Play build:`.
     - Editor tắt: `/Applications/Unity/Hub/Editor/6000.4.8f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath client -buildTarget Android -executeMethod Game.Client.Editor.PlayBuild.BuildAndroid -logFile <file log ngoài repo>`, chạy nền. Lấy dòng `Play build:` trong log.
   - Xong khi: có `client/Builds/play.apk` và dòng `Play build: Succeeded`.
   - Rồi `git status --short client/ProjectSettings client/Assets/Settings`. Mọi file hiện ra → `git checkout --` file đó (thường là `UnityConnectSettings.asset`, `ProjectSettings.asset`, `PC_RPAsset.asset`, `UniversalRenderPipelineGlobalSettings.asset`). Ghi danh sách đã khôi phục vào Kết quả.

5. **Cài và chạy trên điện thoại.** Ký hiệu `ADB` = lệnh adb ở Bối cảnh, `<máy>` = serial đã chọn.
   1. Chọn máy:
      - `ADB devices -l` có `PFQCRWVWR46HU4GQ` (USB) → dùng.
      - Không có: `ADB mdns services`, lấy địa chỉ dòng `_adb._tcp` của `adb-PFQCRWVWR46HU4GQ` rồi `ADB connect <ip>:<cổng>`. Không được thì thử địa chỉ dòng `_adb-tls-connect._tcp`.
      - Kiểm `ADB -s <máy> shell getprop ro.product.model` = `24095PCADG`.
      - Không có máy nào → Vướng mắc, `cần Opus`, dừng.
   2. `ADB -s <máy> install -r client/Builds/play.apk`. Lỗi `INSTALL_FAILED_USER_RESTRICTED` qua Wi‑Fi → Vướng mắc, `cần Opus`, dừng.
   3. `ADB -s <máy> shell input keyevent KEYCODE_WAKEUP`, `ADB -s <máy> logcat -c`, rồi `ADB -s <máy> shell monkey -p com.viventa.play -c android.intent.category.LAUNCHER 1`.
   4. Chờ 40 giây. Kiểm `ADB -s <máy> shell dumpsys power | grep -m1 mWakefulness=` ra `Awake`, và `ADB -s <máy> shell dumpsys window | grep -E "mDreamingLockscreen=true|mShowingLockscreen=true"` không ra dòng nào. Khoá màn hình → Vướng mắc, `cần Opus`, dừng (QĐ 7).
   5. `ADB -s <máy> logcat -d -s Unity > <file ngoài repo>`. Ghi vào Kết quả:
      - nguyên văn dòng `TOUCHUI …`;
      - `grep -cE "Exception|Error" <file>`; có dòng thì chép 5 dòng đầu.
      - `ADB -s <máy> logcat -d | grep -E "FATAL|AndroidRuntime" | head -5`.
      - Không có dòng `TOUCHUI` sau 40 giây → chờ thêm 30 giây rồi đọc lại. Vẫn không có → ghi vào Kết quả, làm tiếp bước 6 bằng toạ độ đọc từ ảnh.
   6. Ảnh 1 (chưa mở bảng): `ADB -s <máy> exec-out screencap -p > /Users/macvn/Documents/User/khang/Game/art-test/shots/b0103_hud.png`.
      - Lấy kích thước ảnh (`sips -g pixelWidth -g pixelHeight <ảnh>`), ghi vào Kết quả, so với `screen=`.
      - Xem ảnh, ghi thấy / không thấy từng thứ: nút Làm, Nhảy, Thu, 4 nút chế độ bên phải, Túi + Huỷ trên-trái, chấm tâm nhắm giữa màn hình. Joystick không hiện là đúng (chỉ hiện khi chạm).

6. **Bấm nút qua adb** (toạ độ theo QĐ 4, lấy từ `bag=` / `back=` trong `TOUCHUI`). Chờ 2 giây sau mỗi lần bấm.
   1. `ADB -s <máy> shell input tap <bag_x> <bag_y>` → ảnh 2 `b0103_bag.png`: túi phải mở. Không mở → ghi vào Kết quả, thử lại một lần theo tỉ lệ ảnh (QĐ 4). Vẫn không mở → ghi, bỏ qua tới 6.3.
   2. `ADB logcat -d -s Unity | grep -cE "Exception|Error"`: phải không tăng so với bước 5.5.
   3. Bấm `back` một lần → ảnh `b0103_back1.png`: túi đóng. Bấm `back` lần nữa → ảnh `b0103_settings.png`: bảng Cài đặt mở.
   4. **Kiểm IMGUI nhận chạm:** trên ảnh `b0103_settings.png` tìm tab "Hướng dẫn" (thanh tab trên cùng của bảng). Tính toạ độ trên ảnh gốc (đơn vị px của `screencap`), rồi `input tap`. Chụp `b0103_imgui.png`.
      - Nội dung đổi sang tab Hướng dẫn → ghi **"IMGUI nhận chạm: có"**.
      - Không đổi → bấm thêm lần nữa, lệch 10 px xuống dưới, rồi chụp lại. Vẫn không đổi → ghi **"IMGUI nhận chạm: không"**.
   5. Bấm `back` → Cài đặt đóng (ảnh `b0103_back2.png`).
   6. Để app chạy. Ghi `ADB logcat -d -s Unity | grep -cE "Exception|Error"` lần cuối.

7. **Đọc ảnh và ghi Kết quả** cho bước 5–6, với mỗi ảnh một dòng `tên file: thấy gì`. Không kết luận "đạt cảm giác": phần đó user xem.

8. **controls-ui** `design/02-gameplay/controls-ui.md`.
   - Thay cột "Cảm ứng" của các dòng sau. Nhận dòng theo ô đầu, giữ nguyên các cột khác:
     | Ô đầu dòng | Cột Cảm ứng mới |
     |---|---|
     | Di chuyển | Joystick ảo nửa trái (hiện tại chỗ chạm) (brief 0101) |
     | Xoay camera | Vuốt nửa phải màn hình (brief 0101) |
     | Camera về mặc định (…) | _(để sau — [ideas](../ideas.md))_ |
     | Góc nhìn thứ nhất / thứ ba | _(để sau — [ideas](../ideas.md))_ |
     | Thu phóng camera | Chụm 2 ngón (brief 0101) |
     | Nhảy | Nút Nhảy, góc dưới-phải (brief 0101) |
     | Lướt | _(để sau — [ideas](../ideas.md))_ |
     | Thao tác chọn (…) | Nút hành động chính "Làm" (góc dưới-phải, to nhất) lên đích dưới tâm nhắm; Mặc định + không cầm + ô ruộng chưa chín → bảng ô ruộng (brief 0102) |
     | Chế độ Mặc định / Đào / Xới / Tưới | Thanh 4 nút dọc mép phải (bấm lại = tắt) (brief 0102) |
     | Dùng ô túi phụ (cầm vật) | Chạm ô túi phụ (brief 0102) |
     | Thu (hút vật rơi trong 2 ô) | Nút Thu, trên nút hành động chính (brief 0102) |
     | Túi đồ (+ túi phụ) | Nút Túi, góc trên-trái cạnh máu/mana (brief 0102) |
     | Ẩn/hiện túi phụ | _(để sau — [ideas](../ideas.md))_ |
     | Chuyển đồ (…) | Kéo thả bằng ngón tay (brief 0102); chọn ô → Tab → 1 / 2 / 3: _(để sau — [ideas](../ideas.md))_ |
     | Bảng kho | Nút hành động chính lên khối kho (brief 0102) |
     | Menu phép (…) | Chạm nút Phép dưới minimap |
     | Xem camera quan sát / quay về | _(để sau — [ideas](../ideas.md))_ |
     | Cài đặt (…) | Nút Huỷ khi không còn gì để đóng (brief 0102) |
     | Menu, tạm dừng / huỷ | Nút Huỷ, cạnh nút Túi (giống Esc) (brief 0102) |
   - Dòng "Bản đồ" giữ nguyên ("Chạm ô minimap"). Các dòng M2 giữ nguyên.
   - Chèn đoạn sau thành một dòng riêng, ngay trước dòng tiêu đề bảng `| Hành động | Bàn phím-chuột | Cảm ứng | …`. Chừa một dòng trống giữa đoạn và bảng:
     `- **Cảm ứng M1 đã làm** (task m1-dieu-khien-cam-ung, brief 0101–0103, user 2026-11-02): tự bật trên điện thoại (\`Application.isMobilePlatform\`), F4 bật/tắt để thử trên máy tính; joystick + tâm nhắm giữa màn hình + nút hành động chính; nút ≥ 48 dp trong vùng an toàn; số tạm \`touch*\` trong \`sim_m1.json\` (chưa chốt). [ý tưởng] APK chơi thử chỉ màn hình ngang. Bảng IMGUI (ô ruộng, Cài đặt, Môi trường sống) trên điện thoại: <IMGUI_KẾT_QUẢ>.`
     - `<IMGUI_KẾT_QUẢ>` = `nhận chạm (kiểm 2026-11-03 bằng tab Cài đặt)` nếu bước 6.4 ra "có". Nếu ra "không": `chưa nhận chạm (kiểm 2026-11-03 bằng tab Cài đặt) — cần sửa sau`. Nếu bước 6.4 không làm được: `chưa kiểm được`.
     - Ngày lấy theo `date +%Y-%m-%d` khi làm.

9. **glossary** `design/glossary.md`: thay 2 dòng, đúng nguyên văn.
   - Dòng `| Tâm nhắm | …` → `| Tâm nhắm | Điểm giữa màn hình trên cảm ứng (và tay cầm); ô / sinh vật dưới tâm nhắm là đích của hành động. PC dùng con trỏ chuột (0034) | [02-gameplay/controls-ui.md](02-gameplay/controls-ui.md) |`
   - Dòng `| Hành động chính | …` → `| Hành động chính | Thao tác chọn lên đích: hành động của chế độ, dùng vật đang cầm, hoặc hành động riêng của chỗ đó (0034). PC: chuột phải; cảm ứng: nút hành động chính lên đích dưới tâm nhắm | [02-gameplay/controls-ui.md](02-gameplay/controls-ui.md) |`

10. **ideas** `design/ideas.md`: thêm ở cuối file một dòng trống rồi mục sau, nguyên văn:
    ```markdown
    ## Điều khiển cảm ứng — để sau (task m1-dieu-khien-cam-ung, user 2026-11-02)
    - Nút cảm ứng cho: lướt (Shift), góc nhìn thứ nhất (X), camera về mặc định (Space), xem / đặt camera quan sát (V), ẩn / hiện túi phụ (P), bảng Môi trường sống (H), chia đồ 1/2 – 1/3 (Tab + 1/2/3). Chưa ước lượng; làm khi chơi thử trên điện thoại thấy cần ([controls-ui](02-gameplay/controls-ui.md)).
    - Đổi vị trí / cỡ nút cảm ứng (M3, controls-ui). Tay cầm: sau phát hành (dòng "Hỗ trợ tay cầm" ở bảng trên).
    - Khoá mục tiêu, nút ngữ cảnh khi nhập hồn, nhắm phép trên cảm ứng: M2.
    - Đo và đạt ngưỡng FPS trên điện thoại khi chơi bằng cảm ứng: user gác 2026-11-01. iOS: chưa thử (M1 chỉ thử Android).
    ```

11. **world-tech** `design/04-tech/world-tech.md`, dòng `| 7m | …`: ở cột cuối, thay đoạn cuối `… Executor ~18 phút) |` thành:
    `… Executor ~18 phút). **(b) xong 2026-11-03** [run] (brief 0102: tâm nhắm, nút Làm / 4 chế độ / Thu / Túi / Huỷ, chạm túi phụ + túi / kho; EditMode 745, PlayMode 318; Executor ~30 phút). **(c) xong <ngày>** [run] (brief 0103: APK \`play.apk\` chạy trên 24095PCADG, <số dòng Exception/Error trong logcat> dòng Exception/Error; IMGUI nhận chạm: <có/không/chưa kiểm>; EditMode <n>, PlayMode <n>; Executor ~<phút> phút). Chờ user chơi thử trên điện thoại |`

12. **roadmap** `design/05-roadmap.md`: chèn dòng sau ngay dưới dòng `| Sửa lỗi mép cỏ hở ở góc lõm (user báo 2026-10-24, thuộc world 7b4) …` (dòng cuối bảng giờ thực tế):
    `| World 7m — điều khiển cảm ứng cơ bản: joystick + vuốt / chụm camera + Nhảy (0101), tâm nhắm + nút Làm / chế độ / Thu / Túi / Huỷ + chạm trong túi (0102), APK chơi thử trên điện thoại + tài liệu (0103) (task m1-dieu-khien-cam-ung) | 3–4,5 [ý tưởng] (trong 3–5 của "Điều khiển"; còn 2–4,5 sau tách đào/đặt) | Executor 0101 ~18 phút (2026-11-02), 0102 ~30 phút (2026-11-03), 0103 ~<phút> phút (<ngày>); review đầy đủ 0101–0102 đạt; chờ user chơi thử trên điện thoại | <n> EditMode + <n> PlayMode (0103, MCP); IMGUI nhận chạm: <có/không/chưa kiểm> |`
    - `<phút>` = từ giờ bắt đầu (bước 1) tới lúc xong bước 12.

13. **Commit.**
    - Commit code + tài liệu: `brief 0103: play APK build, TOUCHUI layout log, touch docs`.
    - Rồi ghi Kết quả + Kết quả test, đặt trạng thái `chờ review`, commit `brief 0103: result`.
    - Kèm dòng `Co-Authored-By` theo AGENTS.md §11. Không push.

## Tiêu chí xong
- `client/Builds/play.apk` có, log `Play build: Succeeded`.
- `git status --short client/ProjectSettings client/Assets/Settings` rỗng sau khi build và chạy test.
- Kết quả có:
  - nguyên văn dòng `TOUCHUI` (hoặc ghi rõ không có);
  - số dòng Exception/Error trong logcat;
  - 6 ảnh: `b0103_hud`, `_bag`, `_back1`, `_settings`, `_imgui`, `_back2`, mỗi ảnh một dòng mô tả;
  - một trong ba kết luận IMGUI: có / không / chưa kiểm.
- Test mới đạt. EditMode 745/745 và PlayMode 319/319 (318 + 1).
- 5 file `design/` đổi đúng bước 8–12. `python3 tools/check_design.py` exit 0.
- `git diff --stat` của commit code chỉ gồm file trong Phạm vi.

## Test sau khi xong

### Phần cần test
1. **Log `TOUCHUI` (mới):** test PlayMode `LayoutLogsOnceWithButtonCenters` trong `TouchControlsPlayTests.cs`, đặt sau `JumpButtonLayout`:
   ```csharp
   [UnityTest]
   public IEnumerator LayoutLogsOnceWithButtonCenters()
   {
       yield return null;
       var lines = new List<string>();
       Application.LogCallback cb = (msg, st, type) => { if (msg.StartsWith("TOUCHUI ")) lines.Add(msg); };
       Application.logMessageReceived += cb;
       try
       {
           TouchControls.SetForceOn(false);
           TouchControls.SetForceOn(true);
           Feed();
           Feed();
       }
       finally { Application.logMessageReceived -= cb; }
       Assert.AreEqual(1, lines.Count, string.Join("\n", lines));
       var m = System.Text.RegularExpressions.Regex.Match(lines[0],
           @"^TOUCHUI screen=\d+x\d+ dpi=160 safe=-?\d+,-?\d+,-?\d+,-?\d+ jump=(\d+),(\d+) action=\d+,\d+ collect=\d+,\d+ mode_default=\d+,\d+ mode_dig=\d+,\d+ mode_till=\d+,\d+ mode_rain=\d+,\d+ bag=\d+,\d+ back=\d+,\d+$");
       Assert.IsTrue(m.Success, lines[0]);
       Assert.AreEqual(touch.JumpButton.anchoredPosition.x, float.Parse(m.Groups[1].Value), 1f);
       Assert.AreEqual(touch.JumpButton.anchoredPosition.y, float.Parse(m.Groups[2].Value), 1f);
   }
   ```
   - Mong đợi: đạt. Hai lần `Feed` liền nhau chỉ ra 1 dòng; tâm `jump=` khớp vị trí nút Nhảy.
   - Nếu `anchoredPosition` không phải tâm nút (anchor khác góc dưới-trái) và test fail ở 2 assert cuối → dừng, `cần Opus`. Không tự đổi test.
2. **Cảm ứng cũ (regression):** `TouchControlsPlayTests`, `TouchActionPlayTests`, `MinimapCornerPlayTests` (gồm `TouchOnCornerOpensMapNotCamera`) đều đạt.
3. **PC không đổi (regression):** toàn bộ PlayMode đạt, đặc biệt `ControlModePlayTests`, `InventoryPlayTests`, `TerrainEditPlayTests`, `SettingsPlayTests`.
4. **Script build:** `PlayBuild.BuildAndroid` ra `play.apk`. Sau build, `ApplicationIdentifier`, `productName` và `defaultInterfaceOrientation` đã khôi phục, kiểm bằng `git status --short client/ProjectSettings` rỗng sau bước khôi phục.
   - Ghi riêng: trước khi `checkout`, `ProjectSettings.asset` có đổi không, và đổi ở dòng nào (`git diff client/ProjectSettings/ProjectSettings.asset | head -20`).
5. **Trên điện thoại:**
   - App mở được, giao diện cảm ứng hiện (ảnh 1).
   - Nút Túi mở túi (ảnh 2), Huỷ đóng túi rồi mở Cài đặt (ảnh 3–4), Huỷ đóng Cài đặt (ảnh 6).
   - Logcat không có Exception (bước 5.5, 6.2, 6.6).
6. **IMGUI trên điện thoại:** bước 6.4, ghi một trong ba kết luận.
7. **Tài liệu:** `python3 tools/check_design.py` exit 0. `git diff design/` chỉ có các thay đổi ở bước 8–12.

### Lệnh executor chạy
1. MCP `read_console` (lọc Error) sau mỗi lần sửa code: không có lỗi compile.
2. MCP `run_tests` EditMode toàn bộ → 745/745.
3. MCP `run_tests` PlayMode toàn bộ → 319/319. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py EditMode` và `python3 tools/unity_test.py PlayMode`, lấy dòng tổng kết (`| tail -n 5`).
4. Build APK (bước 4). Sau đó `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` và các file khác bị bẩn.
5. adb (bước 5–6).
6. `sh "<Plugin root>/scripts/run.sh" verify_run design`. Plugin root lấy từ "Trạng thái phiên".
7. `git status --short` cuối cùng.

### User tự test
Trên điện thoại Xiaomi, mở app **"Viventa Play"**. App đã được cài, không cần cài lại. Nếu app đang chạy thì vuốt tắt rồi mở lại.
- (a) **Joystick:** chạm-giữ nửa trái màn hình rồi kéo. Nhân vật đi theo hướng kéo, thả tay thì dừng. Cần nói: joystick nhạy quá / chậm quá / vừa.
- (b) **Camera:** vuốt nửa phải để xoay, chụm 2 ngón để phóng to / thu nhỏ. Cần nói: tốc độ xoay vừa chưa; góc mặc định có thấy rõ mặt đất trước nhân vật không.
- (c) **Tâm nhắm:** xoay camera để chấm trắng giữa màn hình rơi lên một ô gần. Bấm nút "Đào" bên phải: ô dưới chấm có lớp đỏ. Bấm "Làm": nhân vật đào ô đó (xa hơn 2 ô thì tự đi tới). Cần nói: chọn đúng ô có dễ không.
- (d) **Đủ một vòng:** đào → cầm khối từ túi phụ và đặt → Xới → gieo (chọn túi hạt trong Túi) → Tưới (phép Mưa) → chờ chín → Làm để thu hoạch → Thu để nhặt → Làm lên khối kho rồi kéo đồ vào kho. Ghi lại bước nào không làm được.
- (e) **Bảng ô ruộng:** ở chế độ Mặc định, nhắm vào ô ruộng chưa chín rồi bấm Làm, bảng ô ruộng mở. Chạm chọn giống. Brief này đã kiểm IMGUI trên bảng Cài đặt (xem Kết quả). Nếu ở đó ghi "không", bảng ô ruộng cũng sẽ không chạm được.
- (f) **Nút có che tầm nhìn không;** nút nào khó bấm, quá to hoặc quá nhỏ.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (dòng trích dẫn đã dời quá xa, chữ ký khác).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua (gồm build APK lỗi 2 lần).
- Phải sửa file ngoài "Phạm vi", hoặc test cũ fail.
- Không nối được điện thoại, cài bị chặn, hoặc màn hình khoá (bước 5).
- App crash ngay khi mở (`FATAL` trong logcat, không có ảnh 1): chép 5 dòng lỗi rồi dừng. Không sửa code.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Giờ bắt đầu: 04:01 (2026-11-03)
- Base commit: e95889c
- Chạy bằng: claude-sonnet-5-5 · medium
- Lần 2 (sau trả lời Planner, từ 04:20): code commit `%s` (TouchControls.cs, PlayBuild.cs(+.meta), TouchControlsPlayTests.cs, MinimapCornerPlayTests.cs). Build APK: `Play build: Succeeded, 872898006 byte, 00:03:59` → `client/Builds/play.apk` (35,8 MB). `ProjectSettings.asset` không đổi; `UnityConnectSettings.asset` bẩn → đã `git checkout`.
- Bước 5: **không có điện thoại**: `adb devices -l` rỗng, `adb mdns services` rỗng (thử 2 lần cách nhau 5 giây). Dừng theo brief. Chưa làm bước 5–13 (cài, ảnh, tài liệu 8–12, giờ thực tế).

## Kết quả test
- Lần 2: PlayMode 319/319 đạt (MCP, gồm `TouchOnCornerOpensMapNotCamera` và `LayoutLogsOnceWithButtonCenters`). EditMode 745/745 (lần 1, code không đổi ở assembly EditMode từ đó; không chạy lại).
- EditMode (MCP): 745/745 đạt.
- PlayMode (MCP): 318/319; 1 fail: `MinimapCornerPlayTests.TouchOnCornerOpensMapNotCamera` (`MinimapCornerPlayTests.cs:201`, `Assert.IsTrue(map.IsOpen)` → false). Test mới `LayoutLogsOnceWithButtonCenters` đạt.
- Chưa làm bước 4 (build) trở đi: dừng theo "test cũ fail".

## Vướng mắc
Đã làm bước 1–3 (code `LogLayout`, `PlayBuild.cs`, test mới), CHƯA commit code. Working tree còn: `TouchControls.cs`, `TouchControlsPlayTests.cs`, `PlayBuild.cs(+.meta)`.

**Câu hỏi:** test cũ `TouchOnCornerOpensMapNotCamera` fail, brief cấm sửa test cũ. Xử lý thế nào?
- Chạy riêng test đó: vẫn fail (lặp 2 lần). Tắt tạm dòng `LogLayout(dpi);` trong `Feed`, chạy riêng lại: vẫn fail → **không phải do thay đổi của brief này** (đã khôi phục dòng đó).
- Nghi nguyên nhân (chưa xác minh): kích thước Game view của Editor lúc này khác lúc 0102 chạy (log test: `TOUCHUI screen=1716x876`, minimap `frame 1378x616`), nên `map.ButtonScreenCenter` có thể rơi vào một nút cảm ứng hoặc ngoài vùng; hoặc lỗi flaky phụ thuộc cửa sổ Editor.
- Phương án: (a) Planner xem/sửa test hoặc đặt kích thước Game view cố định trong test; (b) cho phép Executor chạy tiếp build APK, ghi test này là lỗi môi trường đã biết. Đề xuất: (a) trước, vì brief yêu cầu PlayMode 319/319.

**Trả lời của Planner (2026-11-03):**
- **Nguyên nhân** [run]: Editor lúc chạy test không focus (`isApplicationActive=False`). `InputSystem.settings.editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` (mặc định) đẩy input của thiết bị Pointer, gồm cả `Touchscreen`, sang Editor khi Game view không focus (`client/Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/InputSettings.cs:902-911`). Do đó chạm giả lập không tới `MinimapView`. Không phải lỗi của brief này, và cũng là lý do test rớt "1 lần" ở 0102.
- **Sửa** (Planner đã làm trong working tree, `MinimapCornerPlayTests.cs` `TouchOnCornerOpensMapNotCamera`):
  - Trong test, đặt tạm `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView` và `backgroundBehavior = IgnoreFocus`, rồi khôi phục trong `finally`.
  - Assert giữ nguyên. Chỉ bỏ phụ thuộc vào focus cửa sổ (AGENTS.md §11.16).
  - Chạy riêng test này khi Editor không focus: đạt 1/1 [run, MCP].
- **Executor làm tiếp từ bước 4:**
  - Chạy lại toàn bộ EditMode + PlayMode, mong đợi 745/745 và 319/319.
  - Commit `MinimapCornerPlayTests.cs` cùng commit code ở bước 13.
  - Mục Kết quả đã ghi giữ nguyên; ghi tiếp bên dưới.

**Vướng mắc 2 (bước 5):** không nối được điện thoại (không USB, không mDNS). Cần user cắm USB (hoặc bật gỡ lỗi không dây) rồi cho Executor chạy lại từ bước 5; APK đã build sẵn ở `client/Builds/play.apk`, không cần build lại. Đề xuất: user cắm máy, Planner đặt brief về `chờ làm` ghi "bắt đầu từ bước 5".

---
<!-- PLANNER ghi khi review. -->

## Review
