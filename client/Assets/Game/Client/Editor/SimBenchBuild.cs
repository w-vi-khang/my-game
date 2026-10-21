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
    /// Build APK đo sim bước 7 (brief 0028): scene SimBench = bản sao SimView với SimView.stressBench = true (đồng bằng giữa Map 1, 60 sinh vật, log SIMBENCH).
    /// Menu "Game/Tạo scene SimBench", "Game/Build APK SimBench"; dòng lệnh -executeMethod Game.Client.Editor.SimBenchBuild.BuildAndroid.
    /// </summary>
    public static class SimBenchBuild
    {
        const string SourceScene = "Assets/Game/Client/Scenes/SimView.unity";
        const string ScenePath = "Assets/Game/Client/Scenes/SimBench.unity";
        const string OutputPath = "Builds/simbench.apk";
        const string TempAppId = "com.viventa.simbench";

        [MenuItem("Game/Tạo scene SimBench")]
        public static void EnsureScene()
        {
            // Luôn tạo lại từ SimView để SimBench dùng đúng ánh sáng/biển/trời hiện tại.
            var active = EditorSceneManager.GetActiveScene().path;
            if (File.Exists(ScenePath))
                AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
                throw new Exception($"Không chép được {SourceScene} → {ScenePath}");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var views = UnityEngine.Object.FindObjectsByType<SimView>(FindObjectsSortMode.None);
            if (views.Length == 0)
                throw new Exception("Scene SimBench không có SimView");
            foreach (var sv in views)
            {
                sv.stressBench = true;
                EditorUtility.SetDirty(sv);
            }
            // Giữ DebugFlyCamera để bay quan sát trên PC; tắt đào/đặt.
            foreach (var input in UnityEngine.Object.FindObjectsByType<TerrainEditInput>(FindObjectsSortMode.None))
                input.enabled = false;
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(active) && active != ScenePath)
                EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        }

        [MenuItem("Game/Build APK SimBench")]
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
                Debug.Log($"SimBench build: {report.summary.result}, {report.summary.totalSize} byte, {report.summary.totalTime} → {OutputPath}");
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
