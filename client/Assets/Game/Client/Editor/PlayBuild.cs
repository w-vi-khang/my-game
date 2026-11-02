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
