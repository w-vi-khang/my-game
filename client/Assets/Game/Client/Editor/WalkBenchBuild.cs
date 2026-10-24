using System;
using System.Collections.Generic;
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
        // World 7k (brief 0070): GPU Resident Drawer / occlusion culling variants A (off), B (GRD), C (GRD + occlusion).
        // The URP assets are edited through SerializedObject (Game.Client.Editor does not reference the URP assembly) and restored in finally.
        const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        const string PcRpAssetPath = "Assets/Settings/PC_RPAsset.asset";
        const string MobileRpAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
        const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
        static int GrdInstancedDrawing => EnumValue("GPUResidentDrawerMode", "InstancedDrawing");
        static int BrgKeepAll => EnumValue("BatchRendererGroupStrippingMode", "KeepAll");
        const int RenderingModeForwardPlus = 2;    // UniversalRendererData RenderingMode.ForwardPlus (PC_Renderer uses 2)
        const string SourceScene = "Assets/Game/Client/Scenes/SimView.unity";
        const string ScenePath = "Assets/Game/Client/Scenes/WalkBench.unity";
        const string OutputPath = "Builds/walkbench.apk";
        const string OutputPath2 = "Builds/walkbench-2vp.apk";
        const string OutputPath3 = "Builds/walkbench-plots.apk";
        const string TempAppId = "com.viventa.walkbench";

        [MenuItem("Game/Create Scene WalkBench")]
        public static void EnsureScene() => EnsureScene(false);

        /// <summary>Recreates the WalkBench scene from SimView and sets secondViewpoint on every WalkBench.</summary>
        public static void EnsureScene(bool secondViewpoint, int targetPlots = 0)
        {
            // Luôn tạo lại từ SimView để WalkBench dùng đúng ánh sáng/biển/trời hiện tại.
            var active = EditorSceneManager.GetActiveScene().path;
            if (File.Exists(ScenePath))
                AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
                throw new Exception($"Could not copy {SourceScene} → {ScenePath}");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var views = UnityEngine.Object.FindObjectsByType<SimView>(FindObjectsSortMode.None);
            if (views.Length == 0)
                throw new Exception("Scene WalkBench has no SimView");
            foreach (var sv in views)
            {
                var wb = sv.GetComponent<WalkBench>();
                if (wb == null) wb = sv.gameObject.AddComponent<WalkBench>();
                wb.secondViewpoint = secondViewpoint;
                wb.targetPlots = targetPlots;
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
        public static void BuildAndroid() => Build(false, OutputPath);

        /// <summary>Same APK with a fixed second viewpoint at the Map 2 centroid (task 7i, brief 0052); the saved scene goes back to one viewpoint afterwards.</summary>
        [MenuItem("Game/Build APK WalkBench 2 Viewpoints")]
        public static void BuildAndroidTwoViewpoints()
        {
            try { Build(true, OutputPath2); }
            finally { EnsureScene(false); }
        }

        /// <summary>Same APK with 20 farm plots around the camp (world 7l e2, brief 0065) to measure plot icons; the saved scene goes back to defaults afterwards.</summary>
        [MenuItem("Game/Build APK WalkBench 20 Plots")]
        public static void BuildAndroidPlots()
        {
            try { Build(false, OutputPath3, 20); }
            finally { EnsureScene(false); }
        }

        static void Build(bool secondViewpoint, string outputPath, int targetPlots = 0)
        {
            EnsureScene(secondViewpoint, targetPlots);
            var android = NamedBuildTarget.Android;
            string oldId = PlayerSettings.GetApplicationIdentifier(android);
            try
            {
                PlayerSettings.SetApplicationIdentifier(android, TempAppId);
                PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                });
                Debug.Log($"WalkBench build: {report.summary.result}, {report.summary.totalSize} byte, {report.summary.totalTime} → {outputPath}");
                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception($"Build failed: {report.summary.result}");
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(android, oldId);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>Integer value of an enum member found by type name in the loaded assemblies (the enum assemblies are not referenced by Game.Client.Editor).</summary>
        static int EnumValue(string typeName, string member)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var t in asm.GetTypes())
                    if (t.IsEnum && t.Name == typeName)
                        return Convert.ToInt32(Enum.Parse(t, member));
            throw new Exception($"Enum {typeName} not found");
        }

        /// <summary>Sets one int property on a serialized asset, returning an action that restores the old value.</summary>
        static Action SetInt(SerializedObject so, string prop, int value)
        {
            var p = so.FindProperty(prop);
            if (p == null) throw new Exception($"Property {prop} not found on {so.targetObject}");
            int old = p.intValue;
            p.intValue = value;
            so.ApplyModifiedProperties();
            return () =>
            {
                so.Update();
                so.FindProperty(prop).intValue = old;
                so.ApplyModifiedProperties();
            };
        }

        static SerializedObject Load(string path)
        {
            var obj = path.EndsWith("GraphicsSettings.asset")
                ? AssetDatabase.LoadAllAssetsAtPath(path)[0]
                : AssetDatabase.LoadMainAssetAtPath(path);
            if (obj == null) throw new Exception("Asset not found: " + path);
            return new SerializedObject(obj);
        }

        /// <summary>Applies variant A/B/C only for the duration of <paramref name="build"/>; every changed value is restored in finally.</summary>
        static void WithGpuVariant(char variant, bool android, Action build)
        {
            var restore = new List<Action>();
            bool oldStats = PlayerSettings.enableFrameTimingStats;
            try
            {
                PlayerSettings.enableFrameTimingStats = true;
                if (variant == 'B' || variant == 'C')
                {
                    restore.Add(SetInt(Load(GraphicsSettingsPath), "m_BrgStripping", BrgKeepAll));
                    var rp = Load(android ? MobileRpAssetPath : PcRpAssetPath);
                    restore.Add(SetInt(rp, "m_GPUResidentDrawerMode", GrdInstancedDrawing));
                    if (variant == 'C')
                        restore.Add(SetInt(rp, "m_GPUResidentDrawerEnableOcclusionCullingInCameras", 1));
                    if (android)
                        restore.Add(SetInt(Load(MobileRendererPath), "m_RenderingMode", RenderingModeForwardPlus));
                }
                AssetDatabase.SaveAssets();
                build();
            }
            finally
            {
                PlayerSettings.enableFrameTimingStats = oldStats;
                for (int k = restore.Count - 1; k >= 0; k--) restore[k]();
                AssetDatabase.SaveAssets();
            }
        }

        static void BuildMac(string outputPath)
        {
            EnsureScene(false);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            });
            Debug.Log($"WalkBench build: {report.summary.result}, {report.summary.totalSize} byte, {report.summary.totalTime} → {outputPath}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Build failed: {report.summary.result}");
        }

        [MenuItem("Game/Build Mac WalkBench GPU A-B-C")]
        public static void BuildMacGpuVariants()
        {
            foreach (char v in new[] { 'A', 'B', 'C' })
                WithGpuVariant(v, false, () => BuildMac($"Builds/walkbench-mac-{char.ToLower(v)}.app"));
        }

        [MenuItem("Game/Build APK WalkBench GPU A-B-C")]
        public static void BuildAndroidGpuVariants()
        {
            foreach (char v in new[] { 'A', 'B', 'C' })
                WithGpuVariant(v, true, () => Build(false, $"Builds/walkbench-gpu-{char.ToLower(v)}.apk"));
        }
    }
}
