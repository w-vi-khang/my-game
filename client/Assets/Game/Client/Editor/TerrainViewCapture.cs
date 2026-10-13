using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        const string OutputPath = "Logs/terrain_view.png";

        [MenuItem("Game/Tạo scene TerrainView")]
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
        [MenuItem("Game/Thêm đào-đặt vào TerrainView")]
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
            Debug.Log("TERRAINVIEW đã thêm DebugFlyCamera + TerrainEditInput");
        }

        [MenuItem("Game/Chụp ảnh TerrainView")]
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
            Debug.Log($"TERRAINVIEW ảnh → {Path.GetFullPath(OutputPath)}");
        }
    }
}
