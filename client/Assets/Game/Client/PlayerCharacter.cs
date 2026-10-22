using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Game.Client
{
    /// <summary>
    /// Nhân vật người chơi trên PC (world 7h, brief 0035, 0028): khối ghép cao 2 ô, CharacterController, WASD đi theo hướng camera, Space nhảy,
    /// F3 bật/tắt camera bay (debug). Đầu game đặt túi hạt Cây A × 4 gần chỗ xuất phát (brief 0037). Luật tầm với / ô chiếm / biên / nước ở Game.Core.PlayerRules. Gốc transform = chân.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCharacter : MonoBehaviour
    {
        public TerrainChunkView terrain;
        public float walkSpeed = 5f, gravity = 25f, jumpSpeed = 8f, maxFallSpeed = 30f, bobHeight = 0.06f, bobFrequency = 12f;
        public bool readKeyboard = true;
        public Vector2 MoveInput;
        public bool JumpInput;
        /// <summary>Đặt túi hạt Cây A × 4 gần chỗ xuất phát khi có Editor (sim 7e, brief 0037).</summary>
        public bool giveStarterBag = true;
        /// <summary>Chồng túi hạt đầu game đã đặt; null nếu chưa đặt hoặc không có chỗ.</summary>
        public DropStack StarterStack { get; private set; }
        Vector3 spawnFeetLocal;
        float spawnYaw;
        bool hasSpawn;
        WorldEditor starterGivenFor;

        public static PlayerCharacter Active { get; private set; }
        public bool Ready { get; private set; }
        public bool FlyMode { get; private set; }
        public bool InWater { get; private set; }
        public float VerticalSpeed => vy;
        public CharacterController Controller { get; private set; }
        public Transform Visual { get; private set; }
        public Vector3 FeetLocal => terrain.transform.InverseTransformPoint(transform.position);

        float vy;
        Camera cam;
        FollowCamera follow;
        DebugFlyCamera fly;
        float pendingYaw;
        bool hasPendingYaw;

        void Awake()
        {
            Active = this;
            Controller = GetComponent<CharacterController>();
            Controller.height = 1.8f;
            Controller.radius = 0.4f;
            Controller.center = new Vector3(0f, 0.9f, 0f);
            Controller.stepOffset = 0.3f;
            Controller.slopeLimit = 45f;
            Controller.skinWidth = 0.04f;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            if (terrain != null) terrain.streamTarget = transform;
            BuildVisual();
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        void Start()
        {
            cam = Camera.main;
            follow = cam != null ? cam.GetComponent<FollowCamera>() : null;
            fly = cam != null ? cam.GetComponent<DebugFlyCamera>() : null;
            if (follow != null)
            {
                follow.target = this;
                if (hasPendingYaw) follow.SetAngles(pendingYaw, 20f);
            }
            SetFlyMode(false);
        }

        void BuildVisual()
        {
            Visual = new GameObject("Hình").transform;
            Visual.SetParent(transform, false);
            AddBlock("Thân", new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 1.1f, 0.6f), new Color(0.20f, 0.45f, 0.85f));
            AddBlock("Đầu", new Vector3(0f, 1.48f, 0f), new Vector3(0.7f, 0.7f, 0.7f), new Color(0.95f, 0.80f, 0.62f));
            AddBlock("Mũi", new Vector3(0f, 1.48f, 0.4f), new Vector3(0.15f, 0.15f, 0.15f), new Color(0.15f, 0.15f, 0.15f));
        }

        void AddBlock(string name, Vector3 center, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.layer = gameObject.layer;
            go.transform.SetParent(Visual, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            var src = Resources.Load<UnityEngine.Material>("Materials/Landmark");
            var mat = src != null ? new UnityEngine.Material(src) : new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            mat.SetColor("_BaseColor", color);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        /// <summary>Chỗ xuất hiện SimView / dự phòng TerrainView (QĐ 14): cách tâm đồng bằng 8 ô về −z, chân trên mặt đất.</summary>
        public static Vector3 PlainSpawn(Game.Core.Sim.StressPlain p) => new Vector3(p.X0 + p.Size / 2 + 0.5f, p.Ground + 1f, p.Z0 + p.Size / 2 + 0.5f - 8f);

        /// <summary>TerrainView (QĐ 14): cạnh checkpoint Map 1 (2 ô về −z) nếu còn; không thì PlainSpawn. Toạ độ terrain.</summary>
        public static Vector3 TerrainViewSpawn(WorldData world, IReadOnlyList<Landmark> kept, Game.Core.Sim.StressPlain plain, out bool atCheckpoint)
        {
            foreach (var l in kept)
            {
                if (l.Kind != LandmarkKind.Checkpoint || l.Map != 0) continue;
                MinimapImage.ColumnInfo(world, l.X, l.Z - 2, out int s, out _);
                atCheckpoint = true;
                return new Vector3(l.X + 0.5f, s + 1f, l.Z - 2 + 0.5f);
            }
            atCheckpoint = false;
            return PlainSpawn(plain);
        }

        public void Place(Vector3 feetWorld, float yawDeg)
        {
            Controller.enabled = false;
            transform.SetPositionAndRotation(feetWorld, Quaternion.identity);
            Controller.enabled = true;
            Visual.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            vy = 0f;
            Ready = false;
            if (follow != null) follow.SetAngles(yawDeg, 20f);
            else { pendingYaw = yawDeg; hasPendingYaw = true; }
            if (terrain != null) { spawnFeetLocal = terrain.transform.InverseTransformPoint(feetWorld); spawnYaw = yawDeg; hasSpawn = true; }
        }

        public void SetFlyMode(bool on)
        {
            if (fly == null) { FlyMode = false; return; }
            FlyMode = on;
            fly.enabled = on;
            if (on) fly.SyncAngles();
            if (follow != null)
            {
                follow.enabled = !on;
                if (!on) follow.SnapNow();
            }
        }

        public bool InToolReach(Vector3Int cell)
        {
            var f = FeetLocal;
            return PlayerRules.InToolReach(f.x, f.y, f.z, cell.x, cell.y, cell.z);
        }

        public bool OccupiesCell(int x, int y, int z)
        {
            var f = FeetLocal;
            return PlayerRules.Occupies(f.x, f.y, f.z, x, y, z);
        }

        void Update()
        {
            if (terrain == null || terrain.World == null) return;
            if (terrain.Editor != null && terrain.Editor.IsPlayerAt == null)
                terrain.Editor.IsPlayerAt = OccupiesCell;
            if (giveStarterBag && hasSpawn && terrain.Editor != null && starterGivenFor != terrain.Editor)
            {
                starterGivenFor = terrain.Editor;
                float r = spawnYaw * Mathf.Deg2Rad;
                StarterStack = StarterBag.Place(terrain.Editor, spawnFeetLocal.x, spawnFeetLocal.y, spawnFeetLocal.z, Mathf.RoundToInt(Mathf.Sin(r)), Mathf.RoundToInt(Mathf.Cos(r)));
                if (StarterStack != null) Debug.Log($"STARTERBAG túi hạt Cây A × {StarterBag.Seeds} ở ({StarterStack.X}, {StarterStack.Y}, {StarterStack.Z})");
                else Debug.LogWarning("STARTERBAG không tìm được chỗ đặt túi hạt quanh chỗ xuất phát");
            }

            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) SetFlyMode(!FlyMode);

            if (!Ready)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 64f) && terrain.IsTerrainCollider(hit.collider))
                    Ready = true;
                else
                    return;
            }

            if (FlyMode)
            {
                MoveInput = Vector2.zero;
                JumpInput = false;
            }
            else if (readKeyboard && kb != null)
            {
                MoveInput = new Vector2((kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f), (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));
                JumpInput = kb.spaceKey.isPressed;
            }

            var f = FeetLocal;
            InWater = PlayerRules.InWater(terrain.World, f.x, f.y, f.z);

            float yaw = follow != null ? follow.yaw : 0f;
            var dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(MoveInput.x, 0f, MoveInput.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            var h = dir * (walkSpeed * (InWater ? PlayerRules.WaterSpeedFactor : 1f) * Time.deltaTime);

            if (Controller.isGrounded && vy < 0f) vy = -2f;
            if (Controller.isGrounded && JumpInput) vy = jumpSpeed;
            vy = Mathf.Max(vy - gravity * Time.deltaTime, -maxFallSpeed);

            var lh = terrain.transform.InverseTransformVector(h);
            float lx = lh.x, lz = lh.z;
            PlayerRules.ClampToWorld(terrain.World.Size, f.x, f.z, ref lx, ref lz);
            h = terrain.transform.TransformVector(new Vector3(lx, 0f, lz));

            Controller.Move(h + Vector3.up * (vy * Time.deltaTime));

            if (dir.sqrMagnitude > 0.01f)
                Visual.rotation = Quaternion.Slerp(Visual.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime);
            Visual.localPosition = Controller.isGrounded && dir.sqrMagnitude > 0.01f
                ? new Vector3(0f, bobHeight * Mathf.Abs(Mathf.Sin(Time.time * bobFrequency)), 0f)
                : Vector3.zero;
        }
    }
}
