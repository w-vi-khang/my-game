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
    /// Build APK đo FPS đi bộ (world 7h b, brief 0039): scene WalkBench = bản sao SimView có thêm WalkBench (nhân vật tự đi vòng quanh trại, log WALKBENCH).
    /// Menu "Game/Tạo scene WalkBench", "Game/Build APK WalkBench"; dòng lệnh -executeMethod Game.Client.Editor.WalkBenchBuild.BuildAndroid.
    /// </summary>
    public static class WalkBenchBuild
    {
        const string SourceScene = "Assets/Game/Client/Scenes/SimView.unity";
        const string ScenePath = "Assets/Game/Client/Scenes/WalkBench.unity";
        const string OutputPath = "Builds/walkbench.apk";
        const string TempAppId = "com.viventa.walkbench";

        [MenuItem("Game/Tạo scene WalkBench")]
        public static void EnsureScene()
        {
            // Luôn tạo lại từ SimView để WalkBench dùng đúng ánh sáng/biển/trời hiện tại.
            var active = EditorSceneManager.GetActiveScene().path;
            if (File.Exists(ScenePath))
                AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
                throw new Exception($"Không chép được {SourceScene} → {ScenePath}");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var views = UnityEngine.Object.FindObjectsByType<SimView>(FindObjectsSortMode.None);
            if (views.Length == 0)
                throw new Exception("Scene WalkBench không có SimView");
            foreach (var sv in views)
            {
                if (sv.GetComponent<WalkBench>() == null)
                    sv.gameObject.AddComponent<WalkBench>();
                EditorUtility.SetDirty(sv.gameObject);
            }
            // Tắt đào/đặt (nhân vật tự đi).
            foreach (var input in UnityEngine.Object.FindObjectsByType<TerrainEditInput>(FindObjectsSortMode.None))
                input.enabled = false;
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(active) && active != ScenePath)
                EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        }

        [MenuItem("Game/Build APK WalkBench")]
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
                Debug.Log($"WalkBench build: {report.summary.result}, {report.summary.totalSize} byte, {report.summary.totalTime} → {OutputPath}");
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
