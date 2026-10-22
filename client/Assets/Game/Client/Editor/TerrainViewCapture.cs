using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Client.Editor
{
    /// <summary>
    /// Tạo scene TerrainView và chụp ảnh từ dòng lệnh (world-tech bước 2):
    /// Unity -batchmode -quit -projectPath client -executeMethod Game.Client.Editor.TerrainViewCapture.Capture -logFile -
    /// Ảnh ghi ra client/Logs/terrain_view.png (thư mục đã ignore).
    /// </summary>
    public static class TerrainViewCapture
    {
        const string ScenePath = "Assets/Game/Client/Scenes/TerrainView.unity";
        const string OceanMatPath = "Assets/Game/Client/Materials/Ocean.mat";
        const string WaterMatPath = "Assets/Game/Client/Materials/Water.mat";
        const string OutputPath = "Logs/terrain_view.png";

        [MenuItem("Game/Create Scene TerrainView")]
        public static void EnsureScene()
        {
            if (File.Exists(ScenePath))
                return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            cam.transform.position = new Vector3(4f, 34f, 4f);
            cam.transform.LookAt(new Vector3(24f, 12f, 24f));
            cam.backgroundColor = new Color(0.62f, 0.8f, 0.95f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            new GameObject("Terrain").AddComponent<TerrainChunkView>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        /// <summary>
        /// Bước 4: thêm camera bay (DebugFlyCamera) và đào/đặt bằng chuột (TerrainEditInput) vào scene TerrainView:
        /// Unity -batchmode -quit -projectPath client -executeMethod Game.Client.Editor.TerrainViewCapture.SetupEditing
        /// </summary>
        [MenuItem("Game/Add Dig-Place to TerrainView")]
        public static void SetupEditing()
        {
            EnsureScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam.GetComponent<DebugFlyCamera>() == null)
                cam.gameObject.AddComponent<DebugFlyCamera>();
            var view = Object.FindAnyObjectByType<TerrainChunkView>();
            if (view.GetComponent<TerrainEditInput>() == null)
                view.gameObject.AddComponent<TerrainEditInput>();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TERRAINVIEW added DebugFlyCamera + TerrainEditInput");
        }

        /// <summary>Bước 7b (brief 0010): scene TerrainView dùng world v3 + nạp dần quanh camera, sương 64–120, far clip 128.</summary>
        [MenuItem("Game/Convert TerrainView to World v3")]
        public static void SetupStreamingV3()
        {
            EnsureScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var view = Object.FindAnyObjectByType<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = Camera.main.transform;
            view.loadRadius = 128;
            view.unloadRadius = 160;
            view.maxColumnsPerFrame = 2;
            EditorUtility.SetDirty(view);
            foreach (var ocean in Object.FindObjectsByType<OceanBackdrop>(FindObjectsSortMode.None))
            {
                ocean.source = WorldSource.MapsV3;
                EditorUtility.SetDirty(ocean);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 64f;
            RenderSettings.fogEndDistance = 120f;
            var cam = Camera.main;
            cam.farClipPlane = 128f;
            cam.transform.position = new Vector3(192f, 40f, 150f);
            cam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            EditorUtility.SetDirty(cam);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TERRAINVIEW converted to world v3 + streaming");
        }

        /// <summary>Bước 7b2 (brief 0011): mặt nước theo ô dùng Water.mat (trong suốt), camera bay va chạm + hiệu ứng dưới nước, near clip 0,1.</summary>
        [MenuItem("Game/Clear Water + Camera Collision for TerrainView")]
        public static void SetupWaterAndCamera()
        {
            EnsureScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var water = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);
            if (water == null)
            {
                water = new Material(AssetDatabase.LoadAssetAtPath<Material>(OceanMatPath)) { name = "Water" };
                AssetDatabase.CreateAsset(water, WaterMatPath);
            }
            water.SetFloat("_Alpha", 0.45f);
            water.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            water.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            water.SetFloat("_ZWrite", 0f);
            water.SetFloat("_Cull", (float)CullMode.Off);
            water.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(water);
            AssetDatabase.SaveAssets();

            var view = Object.FindAnyObjectByType<TerrainChunkView>();
            view.waterMaterial = water;
            EditorUtility.SetDirty(view);
            var cam = Camera.main;
            cam.nearClipPlane = 0.1f;
            EditorUtility.SetDirty(cam);
            var fly = cam.GetComponent<DebugFlyCamera>();
            fly.terrain = view;
            fly.collisionRadius = 0.25f;
            EditorUtility.SetDirty(fly);
            var under = cam.GetComponent<UnderwaterView>();
            if (under == null)
                under = cam.gameObject.AddComponent<UnderwaterView>();
            under.terrain = view;
            under.fogColor = new Color(0.14f, 0.44f, 0.56f);
            under.backgroundColor = new Color(0.42f, 0.70f, 0.82f);
            under.fogStart = 0f;
            under.fogEnd = 40f;
            EditorUtility.SetDirty(under);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TERRAINVIEW enabled clear water + camera collision + underwater effect");
        }

        /// <summary>Bước 7b3 (brief 0012): sương biên world trên camera của TerrainView.</summary>
        [MenuItem("Game/World Border for TerrainView")]
        public static void SetupWorldEdge()
        {
            EnsureScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var view = Object.FindAnyObjectByType<TerrainChunkView>();
            var cam = Camera.main;
            var edge = cam.GetComponent<WorldEdgeFog>();
            if (edge == null)
                edge = cam.gameObject.AddComponent<WorldEdgeFog>();
            edge.terrain = view;
            edge.underwater = cam.GetComponent<UnderwaterView>();
            EditorUtility.SetDirty(edge);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TERRAINVIEW added world border fog");
        }

        /// <summary>Bước 7c (brief 0016): bản đồ (MinimapView) trên TerrainChunkView của TerrainView.</summary>
        [MenuItem("Game/Minimap for TerrainView")]
        public static void SetupMinimap()
        {
            EnsureScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var view = Object.FindAnyObjectByType<TerrainChunkView>();
            if (view.GetComponent<MinimapView>() == null)
                view.gameObject.AddComponent<MinimapView>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TERRAINVIEW added minimap");
        }

        [MenuItem("Game/Capture TerrainView")]
        public static void Capture()
        {
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindAnyObjectByType<TerrainChunkView>().Build();

            var cam = Camera.main;
            cam.transform.position = new Vector3(-14f, 46f, -14f);
            cam.transform.LookAt(new Vector3(32f, 8f, 32f));
            cam.backgroundColor = new Color(0.62f, 0.8f, 0.95f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(OutputPath, tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            Debug.Log($"TERRAINVIEW image → {Path.GetFullPath(OutputPath)}");
        }
    }
}
