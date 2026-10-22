using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Đồng bằng demo (task m1-demo-dong-bang): san phẳng 48 × 48 cột giữa Map 1 (SimStress.FlattenPlain) trước khi dựng mesh, bỏ dấu mốc trong đồng bằng,
    /// đặt camera nhìn vào tâm. Gắn vào GameObject có TerrainChunkView (scene TerrainView). SimView dùng các hàm static, không gắn component này.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class DemoPlain : MonoBehaviour
    {
        /// <summary>Góc nghiêng camera demo (độ).</summary>
        public const float Pitch = 35f;

        public StressPlain? Plain { get; private set; }

        void Awake() => GetComponent<TerrainChunkView>().afterGenerate = OnAfterGenerate;

        IReadOnlyList<Landmark> OnAfterGenerate(WorldData world, MapTable maps, WorldMapParams p, IReadOnlyList<Landmark> lms)
        {
            var kept = Flatten(world, maps, p, lms, out var plain);
            Plain = plain;
            var pos = CameraPos(plain);
            PlaceCamera(pos, Pitch);
            Debug.Log($"DEMOPLAIN {plain}, bỏ {lms.Count - kept.Count} dấu mốc, camera ({pos.x:F0}, {pos.y:F0}, {pos.z:F0})");
            return kept;
        }

        /// <summary>San đồng bằng giữa Map 1 (ghi thẳng vào world) và trả dấu mốc ngoài đồng bằng.</summary>
        public static IReadOnlyList<Landmark> Flatten(WorldData world, MapTable maps, WorldMapParams p, IReadOnlyList<Landmark> lms, out StressPlain plain)
        {
            plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, ConfigLoader.LoadSimConfig().regionSize);
            return SimStress.WithoutPlain(lms, plain);
        }

        /// <summary>Camera demo: tâm x + 0,5, cao hơn mặt đồng bằng 16, lùi 21 theo −z.</summary>
        public static Vector3 CameraPos(StressPlain p) =>
            new Vector3(p.X0 + p.Size / 2 + 0.5f, p.Ground + 16f, p.Z0 + p.Size / 2 + 0.5f - 21f);

        /// <summary>Điểm giữa trại (bố trí giãn dz −2..5), trên mặt đất.</summary>
        public static Vector3 CampCenter(StressPlain p) =>
            new Vector3(p.X0 + p.Size / 2 + 0.5f, p.Ground + 1f, p.Z0 + p.Size / 2 + 1.5f);

        /// <summary>Đặt Camera.main (nếu có) và đồng bộ góc DebugFlyCamera.</summary>
        public static void PlaceCamera(Vector3 pos, float pitch)
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, 0f, 0f));
            var fly = cam.GetComponent<DebugFlyCamera>();
            if (fly != null) fly.SyncAngles();
        }
    }
}
