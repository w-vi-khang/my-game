using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Client.Editor
{
    /// <summary>
    /// Build APK đo hiệu năng (world-tech bước 6): scene Bench = bản sao TerrainView + TerrainBench chạy tự động.
    /// Theo mẫu HashCheckBuild: IL2CPP + ARM64 (Burst FloatMode.Deterministic chỉ hỗ trợ 64-bit), tên gói Android tạm rồi trả lại.
    /// Chạy từ menu "Game/Build APK Bench" (Editor đang mở) hoặc dòng lệnh -executeMethod Game.Client.Editor.BenchBuild.BuildAndroid.
    /// </summary>
    public static class BenchBuild
    {
        const string SourceScene = "Assets/Game/Client/Scenes/TerrainView.unity";
        const string ScenePath = "Assets/Game/Client/Scenes/Bench.unity";
        const string OutputPath = "Builds/bench.apk";
        const string TempAppId = "com.viventa.bench";

        [MenuItem("Game/Tạo scene Bench")]
        public static void EnsureScene()
        {
            // Luôn tạo lại từ TerrainView để Bench dùng đúng ánh sáng/biển/trời hiện tại.
            var active = EditorSceneManager.GetActiveScene().path;
            if (File.Exists(ScenePath))
                AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
                throw new Exception($"Không chép được {SourceScene} → {ScenePath}");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var view = UnityEngine.Object.FindFirstObjectByType<TerrainChunkView>();
            if (view == null)
                throw new Exception("Scene Bench không có TerrainChunkView");
            // Bench tự chạy: tắt điều khiển chuột/phím và camera bay.
            foreach (var input in UnityEngine.Object.FindObjectsByType<TerrainEditInput>(FindObjectsSortMode.None))
                input.enabled = false;
            foreach (var fly in UnityEngine.Object.FindObjectsByType<DebugFlyCamera>(FindObjectsSortMode.None))
                fly.enabled = false;
            if (view.GetComponent<TerrainBench>() == null)
                view.gameObject.AddComponent<TerrainBench>();
            // Brief 0023: đo FPS khi bầu trời xoay — cứ 5 giây tự chọn mốc; không có bàn phím.
            foreach (var day in UnityEngine.Object.FindObjectsByType<DayCycleView>(FindObjectsSortMode.None))
            {
                day.benchCycleSeconds = 5f;
                day.debugKeys = false;
            }
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(active) && active != ScenePath)
                EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        }

        [MenuItem("Game/Build APK Bench")]
        public static void BuildAndroid()
        {
            EnsureScene();
            var android = NamedBuildTarget.Android;
            string oldId = PlayerSettings.GetApplicationIdentifier(android);
            try
            {
                PlayerSettings.SetApplicationIdentifier(android, TempAppId);
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
                Debug.Log($"Bench build: {report.summary.result}, {report.summary.totalSize} byte, {report.summary.totalTime} → {OutputPath}");
                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception($"Build thất bại: {report.summary.result}");
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(android, oldId);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
