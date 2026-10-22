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
    /// Build APK kiểm tất định (world-tech bước 1). Chạy dòng lệnh:
    /// Unity -batchmode -quit -projectPath client -buildTarget Android -executeMethod Game.Client.Editor.HashCheckBuild.BuildAndroid -logFile -
    /// Tên gói Android dùng tạm rồi trả lại như cũ (tên chính thức do user đặt).
    /// </summary>
    public static class HashCheckBuild
    {
        const string ScenePath = "Assets/Game/Client/Scenes/HashCheck.unity";
        const string OutputPath = "Builds/hashcheck.apk";
        const string TempAppId = "com.viventa.hashcheck";

        [MenuItem("Game/Create Scene HashCheck")]
        public static void EnsureScene()
        {
            if (File.Exists(ScenePath))
                return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("HashCheck").AddComponent<HashCheckView>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        [MenuItem("Game/Build APK HashCheck")]
        public static void BuildAndroid()
        {
            EnsureScene();
            var android = NamedBuildTarget.Android;
            string oldId = PlayerSettings.GetApplicationIdentifier(android);
            try
            {
                PlayerSettings.SetApplicationIdentifier(android, TempAppId);
                // ARM64 + IL2CPP: Burst FloatMode.Deterministic chỉ hỗ trợ 64-bit (architecture.md).
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
                Debug.Log($"HashCheck build: {report.summary.result}, {report.summary.totalSize} byte → {OutputPath}");
                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception($"Build failed: {report.summary.result}");
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(android, oldId);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
