using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>
    /// Đào/đặt bằng chuột trong scene TerrainView (world-tech bước 4, user duyệt 2026-10-13):
    /// chuột trái đào, chuột phải đặt, phím 1–4 hoặc lăn chuột chọn đất/cỏ/đá/cát; khung trắng = ô sẽ đào, nhãn góc trái = kho vật liệu + lý do khi không đào/đặt được.
    /// Ô chọn = raycast vào collider rồi lùi/tiến nửa ô theo pháp tuyến — mesh mịn nên ở góc có thể lệch 1 ô (chấp nhận ở M1).
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class TerrainEditInput : MonoBehaviour
    {
        public float maxDistance = 200f;
        /// <summary>Độ dày nét khung chọn tính bằng pixel màn hình — giữ nguyên dù camera xa hay gần.</summary>
        public float outlinePixels = 3f;

        static readonly BlockMaterial[] Palette =
            { BlockMaterial.Dirt, BlockMaterial.Grass, BlockMaterial.Stone, BlockMaterial.Sand };
        static readonly string[] PaletteNames = { "đất", "cỏ", "đá", "cát" };

        TerrainChunkView view;
        LineRenderer outline;
        int selected;
        string lastResult = "";

        void Awake()
        {
            view = GetComponent<TerrainChunkView>();
            outline = CreateOutline();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) selected = 0;
                if (kb.digit2Key.wasPressedThisFrame) selected = 1;
                if (kb.digit3Key.wasPressedThisFrame) selected = 2;
                if (kb.digit4Key.wasPressedThisFrame) selected = 3;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                // Lăn chuột cũng đổi loại (phòng khi phím số bị bộ gõ tiếng Việt chặn).
                float wheel = mouse.scroll.ReadValue().y;
                if (wheel > 0.01f) selected = (selected + Palette.Length - 1) % Palette.Length;
                else if (wheel < -0.01f) selected = (selected + 1) % Palette.Length;
            }

            var cam = Camera.main;
            if (mouse == null || cam == null || view.Editor == null)
                return;

            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, maxDistance))
            {
                outline.enabled = false;
                return;
            }

            var digCell = view.CellAt(hit.point - hit.normal * 0.5f);
            var placeCell = view.CellAt(hit.point + hit.normal * 0.5f);
            outline.enabled = true;
            outline.transform.localPosition = digCell;
            // Độ rộng nét theo đơn vị thế giới để ra outlinePixels trên màn hình ở khoảng cách tới tâm ô.
            float dist = Vector3.Distance(cam.transform.position, outline.transform.TransformPoint(0.5f, 0.5f, 0.5f));
            float worldPerPixel = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
            outline.widthMultiplier = outlinePixels * worldPerPixel;

            if (DebugFlyCamera.IsMoveKeyHeld(kb)) return; // Đang bay: chỉ đào/đặt khi dừng (user 2026-10-20).

            if (mouse.leftButton.wasPressedThisFrame)
            {
                var m = view.World.Size.InBounds(digCell.x, digCell.y, digCell.z)
                    ? view.World.GetMaterial(digCell.x, digCell.y, digCell.z) : BlockMaterial.Empty;
                var r = view.Dig(digCell);
                lastResult = r == EditResult.Ok ? $"Đào {digCell}: +1 {Name(m)}" : $"Đào {digCell}: không được — {Explain(r)}";
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                var r = view.Place(placeCell, Palette[selected]);
                lastResult = r == EditResult.Ok ? $"Đặt {PaletteNames[selected]} {placeCell}" : $"Đặt {PaletteNames[selected]}: không được — {Explain(r)}";
            }
        }

        static string Name(BlockMaterial m)
        {
            int i = System.Array.IndexOf(Palette, m);
            return i >= 0 ? PaletteNames[i] : m.ToString();
        }

        static string Explain(EditResult r) => r switch
        {
            EditResult.NoMaterial => "kho hết loại này (đào loại đó trước, hoặc chọn loại khác bằng 1–4 / lăn chuột)",
            EditResult.NotEmpty => "ô đã có khối",
            EditResult.NotAttached => "phải kề một khối đặc",
            EditResult.NotDiggable => "ô rỗng, nước, đá đáy hoặc khối cốt truyện",
            EditResult.OutOfBounds => "ngoài map",
            EditResult.Occupied => "ô đang bị chiếm",
            EditResult.NotPlaceable => "loại này không đặt được",
            _ => r.ToString(),
        };

        void OnGUI()
        {
            if (view.Inventory == null)
                return;
            var text = "Chuột trái: đào · Chuột phải: đặt loại đang chọn · 1–4 / lăn chuột: chọn loại · WASD, Q/E, giữ chuột giữa: camera · dừng bay mới đào/đặt được\nKho: ";
            for (int i = 0; i < Palette.Length; i++)
                text += i == selected
                    ? $"<b><color=yellow>[{i + 1} {PaletteNames[i]}: {view.Inventory.Get(Palette[i])}]</color></b>   "
                    : $"{i + 1} {PaletteNames[i]}: {view.Inventory.Get(Palette[i])}   ";
            text += $"\n{lastResult}";
            if (view.LastRebuildFrame >= 0)
                text += $"\nDựng lại {view.LastRebuildChunks} chunk: mesh {view.LastMeshMs:F1} ms, ghi {view.LastWriteMs:F1} ms, bake {view.LastBakeMs:F1} ms";
            // Phóng chữ theo chiều cao màn hình (mốc 720p) — màn Retina chữ mặc định quá nhỏ.
            float scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.Label(new Rect(10, 10, 1000, 90), text, new GUIStyle(GUI.skin.label) { richText = true });
        }

        LineRenderer CreateOutline()
        {
            var go = new GameObject("Ô đang chọn");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.04f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Vẽ đè lên địa hình: khung bao ô đặc nên nửa dưới nằm trong mặt mesh mịn, ZTest thường sẽ che mất.
            var shader = Shader.Find("Game/SelectionOverlay");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = shader != null
                ? new UnityEngine.Material(shader)
                : new UnityEngine.Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial);
            mat.SetColor("_BaseColor", Color.white);
            lr.sharedMaterial = mat;
            // 12 cạnh khối lập phương đơn vị trong một đường 16 điểm (3 cạnh vẽ lặp).
            var p = new[]
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 0),
                new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1),
                new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1),
                new Vector3(1, 0, 0),
            };
            lr.positionCount = p.Length;
            lr.SetPositions(p);
            lr.enabled = false;
            return lr;
        }
    }
}
