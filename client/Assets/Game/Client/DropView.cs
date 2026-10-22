using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Client
{
    /// <summary>
    /// Vẽ vật rơi (task contract sim 7e, brief 0037): mỗi chồng một khối mini cạnh 0,3 ô màu như khối (túi hạt: hộp 0,3 × 0,4 × 0,3 màu nâu), xoay chậm;
    /// vẽ gộp theo loại bằng Graphics.RenderMeshInstanced, không tạo GameObject cho từng chồng. Chồng vừa nhặt bay về nhân vật 0,2 giây (chỉ hiển thị).
    /// TerrainEditInput tự thêm component này.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class DropView : MonoBehaviour
    {
        public const float BlockSize = 0.3f;
        public static readonly Vector3 SeedBagSize = new Vector3(0.3f, 0.4f, 0.3f);
        public static readonly Color SeedBagColor = new Color(0.55f, 0.40f, 0.20f);
        public const float FlySeconds = 0.2f;
        public const float SpinDegreesPerSecond = 45f;
        /// <summary>Số thể hiện mỗi lần vẽ: ≤ 511 khi mỗi thể hiện là Matrix4x4 (docs RenderMeshInstanced).</summary>
        const int Batch = 500;

        struct Flying { public ItemId Item; public Vector3 From; public float Start; }

        TerrainChunkView view;
        WorldEditor subscribed;
        Mesh cube;
        UnityEngine.Material baseMat;
        readonly Dictionary<ItemId, UnityEngine.Material> mats = new Dictionary<ItemId, UnityEngine.Material>();
        readonly Dictionary<ItemId, List<Matrix4x4>> groups = new Dictionary<ItemId, List<Matrix4x4>>();
        readonly List<Flying> flying = new List<Flying>();

        /// <summary>Số thể hiện đã vẽ ở khung gần nhất (chồng + vật đang bay).</summary>
        public int LastInstanceCount { get; private set; }
        public int FlyingCount => flying.Count;

        public static Vector3 SizeOf(ItemId item) => item.Kind == ItemKind.SeedBag ? SeedBagSize : Vector3.one * BlockSize;
        /// <summary>Tâm hình của chồng (toạ độ terrain): giữa ô theo x/z, đáy ở mặt ô.</summary>
        public static Vector3 LocalCenter(DropStack s) => new Vector3(s.X + 0.5f, s.Y + SizeOf(s.Item).y * 0.5f, s.Z + 0.5f);
        public static Color ColorOf(ItemId item)
        {
            if (item.Kind == ItemKind.SeedBag) return SeedBagColor;
            var c = TerrainEditInput.GhostColorOf(item.Material);
            c.a = 1f;
            return c;
        }

        void Awake()
        {
            view = GetComponent<TerrainChunkView>();
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tmp.SetActive(false);
            cube = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            baseMat = Resources.Load<UnityEngine.Material>("Materials/Landmark");
            if (baseMat == null) baseMat = GraphicsSettings.currentRenderPipeline.defaultMaterial;
        }

        void OnDestroy()
        {
            if (subscribed != null) subscribed.Drops.Collected -= OnCollected;
            foreach (var m in mats.Values) Destroy(m);
        }

        public UnityEngine.Material MaterialFor(ItemId item)
        {
            if (!mats.TryGetValue(item, out var m))
            {
                m = new UnityEngine.Material(baseMat) { enableInstancing = true };
                m.SetColor("_BaseColor", ColorOf(item));
                mats[item] = m;
            }
            return m;
        }

        void OnCollected(DropStack s) => flying.Add(new Flying { Item = s.Item, From = LocalCenter(s), Start = Time.time });

        void LateUpdate()
        {
            LastInstanceCount = 0;
            var editor = view.Editor;
            if (editor != subscribed)
            {
                if (subscribed != null) subscribed.Drops.Collected -= OnCollected;
                subscribed = editor;
                flying.Clear();
                if (editor != null) editor.Drops.Collected += OnCollected;
            }
            if (editor == null) return;

            foreach (var list in groups.Values) list.Clear();
            var toWorld = view.transform.localToWorldMatrix;
            float now = Time.time;
            foreach (var s in editor.Drops.Stacks)
                AddInstance(s.Item, toWorld * Matrix4x4.TRS(LocalCenter(s), Quaternion.Euler(0f, now * SpinDegreesPerSecond + s.Id * 37f, 0f), SizeOf(s.Item)));
            var player = PlayerCharacter.Active;
            for (int i = flying.Count - 1; i >= 0; i--)
            {
                var f = flying[i];
                float k = (now - f.Start) / FlySeconds;
                if (k >= 1f) { flying.RemoveAt(i); continue; }
                var to = player != null ? player.FeetLocal + Vector3.up * PlayerRules.BodyCenterHeight : f.From;
                AddInstance(f.Item, toWorld * Matrix4x4.TRS(Vector3.Lerp(f.From, to, k), Quaternion.identity, SizeOf(f.Item) * (1f - 0.5f * k)));
            }

            var size = new Vector3(view.World.Size.X, view.World.Size.Y, view.World.Size.Z);
            var bounds = new Bounds(view.transform.TransformPoint(size * 0.5f), size + Vector3.one * 2f);
            foreach (var kv in groups)
            {
                var list = kv.Value;
                if (list.Count == 0) continue;
                var rp = new RenderParams(MaterialFor(kv.Key)) { worldBounds = bounds, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true };
                for (int start = 0; start < list.Count; start += Batch)
                    Graphics.RenderMeshInstanced(rp, cube, 0, list, Mathf.Min(Batch, list.Count - start), start);
                LastInstanceCount += list.Count;
            }
        }

        void AddInstance(ItemId item, Matrix4x4 m)
        {
            if (!groups.TryGetValue(item, out var list)) groups[item] = list = new List<Matrix4x4>();
            list.Add(m);
        }
    }
}
