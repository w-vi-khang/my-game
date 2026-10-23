using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Game.Client
{
    /// <summary>
    /// Nhân vật người chơi trên PC (world 7h, brief 0035, 0028): khối ghép cao 2 ô, CharacterController, chuột trái đi tới chỗ click (0034, 7l b), A nhảy, Shift lướt (MoveInput/JumpInput vẫn gán được từ code: test, WalkBench),
    /// F3 bật/tắt camera bay (debug), tự đi theo đường ô (sim 7f, brief 0041). Máu + mana (0034 mục 5, brief 0060): Vitals; tốn mana ở TerrainEditInput / PlayerFarmActions; rơi cao, ngập đầu, hồi sinh ở chỗ Place lần đầu. Đầu game đặt túi hạt Cây A × 4 gần chỗ xuất phát (brief 0037). Luật tầm với / ô chiếm / biên / nước ở Game.Core.PlayerRules. Gốc transform = chân.
    /// </summary>
    public enum AutoWalkState { None, Walking, Arrived, Cancelled, Failed }

    public enum ClickMoveState { None, Walking, Arrived, Blocked }

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
        /// <summary>Máu + mana (0034 mục 5, brief 0060).</summary>
        public PlayerVitals Vitals { get; private set; }
        /// <summary>Đầu ngập nước ở frame vừa rồi.</summary>
        public bool HeadUnderwater { get; private set; }
        /// <summary>Số lần hồi sinh (máu về 0); TerrainEditInput theo dõi để huỷ việc đang làm.</summary>
        public int Respawns { get; private set; }
        /// <summary>Chỗ xuất phát = chỗ Place lần đầu (toạ độ terrain); hồi sinh về đây.</summary>
        public Vector3 RespawnFeetLocal => respawnFeetLocal;
        public bool ManaFlashing => manaFlashLeft > 0f;
        Vector3 respawnFeetLocal;
        float respawnYaw, manaFlashLeft, airPeakY, vitalsMsAcc;
        bool hasRespawn, airborne;
        public float VerticalSpeed => vy;
        public CharacterController Controller { get; private set; }
        public Transform Visual { get; private set; }
        /// <summary>Hướng mặt nhân vật (độ, quanh trục Y).</summary>
        public float FacingYaw => Visual.eulerAngles.y;
        public bool BodyVisible { get; private set; } = true;

        /// <summary>Ẩn/hiện thân (góc nhìn thứ nhất, brief 0056).</summary>
        public void SetBodyVisible(bool on)
        {
            BodyVisible = on;
            if (Visual == null) return;
            foreach (var r in Visual.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
        }
        public Vector3 FeetLocal => terrain.transform.InverseTransformPoint(transform.position);

        /// <summary>Trạng thái tự đi theo đường ô (sim 7f, brief 0041).</summary>
        public AutoWalkState AutoWalk { get; private set; }
        readonly List<Game.Core.Sim.CellPos> autoPath = new List<Game.Core.Sim.CellPos>();
        int autoIndex;
        float autoStall;

        /// <summary>Bắt đầu tự đi qua các ô chân (toạ độ terrain, Game.Core.Sim.CellPos). Đường rỗng → Arrived ngay.</summary>
        public void StartAutoWalk(IReadOnlyList<Game.Core.Sim.CellPos> path)
        {
            ClickMove = ClickMoveState.None;
            autoPath.Clear();
            for (int i = 0; i < path.Count; i++) autoPath.Add(path[i]);
            autoIndex = 0;
            autoStall = 0f;
            AutoWalk = autoPath.Count == 0 ? AutoWalkState.Arrived : AutoWalkState.Walking;
        }

        /// <summary>Đang Walking → None (người gọi tự huỷ).</summary>
        public void StopAutoWalk()
        {
            if (AutoWalk == AutoWalkState.Walking) AutoWalk = AutoWalkState.None;
        }

        /// <summary>Đi tới chỗ click (0034 mục 1).</summary>
        public ClickMoveState ClickMove { get; private set; }
        /// <summary>Đích click, toạ độ terrain; y = độ cao chân lúc click.</summary>
        public Vector3 ClickTargetLocal { get; private set; }
        public DashState Dash { get; private set; }
        public bool Dashing => Dash != null && Dash.Active;
        /// <summary>Hướng đi ngang (world) frame vừa rồi; zero khi đứng yên. Mũi tên dưới chân dùng.</summary>
        public Vector3 MoveDirection { get; private set; }
        public MoveMarkers Markers { get; private set; }
        /// <summary>Người chơi đang tự di chuyển → huỷ tự đi theo đường ô / tự xới.</summary>
        public bool HasMoveIntent => FlyMode || MoveInput.sqrMagnitude > 0.01f || JumpInput || ClickMove == ClickMoveState.Walking || Dashing;
        public const float ClickRayDistance = 500f;
        StuckTimer stuck;
        Vector3 dashDir;
        bool holdFromWorld;

        /// <summary>Đặt đích đi (toạ độ terrain); y bỏ qua — đích quy về độ cao chân hiện tại.</summary>
        public void MoveTo(Vector3 targetLocal)
        {
            ClickTargetLocal = new Vector3(targetLocal.x, FeetLocal.y, targetLocal.z);
            ClickMove = ClickMoveState.Walking;
            stuck.Reset();
        }

        public void StopClickMove()
        {
            if (ClickMove == ClickMoveState.Walking) ClickMove = ClickMoveState.None;
        }

        /// <summary>Điểm world dưới tia: trúng collider, không thì mặt phẳng ngang qua chân; còn lại false.</summary>
        public bool PointOnWorld(Ray ray, out Vector3 pointWorld, out Vector3 normalWorld)
        {
            if (Physics.Raycast(ray, out var hit, ClickRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                pointWorld = hit.point;
                normalWorld = hit.normal;
                return true;
            }
            if (new Plane(Vector3.up, transform.position).Raycast(ray, out float enter) && enter > 0f)
            {
                pointWorld = ray.GetPoint(enter);
                normalWorld = Vector3.up;
                return true;
            }
            pointWorld = default;
            normalWorld = Vector3.up;
            return false;
        }

        /// <summary>Click trái: đi tới điểm dưới tia; mark → dấu click. Trả false khi tia không cho điểm.</summary>
        public bool ClickMoveRay(Ray ray, bool mark)
        {
            if (!PointOnWorld(ray, out var p, out var n)) return false;
            MoveTo(terrain.transform.InverseTransformPoint(p));
            if (mark) Markers.ShowClick(p, n);
            return true;
        }

        /// <summary>Shift: lướt theo hướng đang đi (đứng yên → hướng mặt).</summary>
        public bool TryDash()
        {
            if (FlyMode || ObservationCamera.InputLocked || !Ready || !Dash.TryStart(Controller.isGrounded)) return false;
            var d = Vector3.zero;
            if (ClickMove == ClickMoveState.Walking)
            {
                var f = FeetLocal;
                d = terrain.transform.TransformDirection(new Vector3(ClickTargetLocal.x - f.x, 0f, ClickTargetLocal.z - f.z));
                d.y = 0f;
            }
            if (d.sqrMagnitude < 1e-6f) { d = Visual.forward; d.y = 0f; }
            dashDir = d.normalized;
            return true;
        }

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
            Vitals = new PlayerVitals(ConfigLoader.LoadSimConfig().player);
            stuck = new StuckTimer(PlayerMotion.StuckSeconds);
            Dash = new DashState(PlayerMotion.DashCells, PlayerMotion.DashSeconds, PlayerMotion.DashCooldownSeconds);
            Markers = gameObject.AddComponent<MoveMarkers>();
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
                if (hasPendingYaw) follow.SetAngles(pendingYaw, follow.defaultPitch);
            }
            SetFlyMode(false);
        }

        void BuildVisual()
        {
            Visual = new GameObject("Visual").transform;
            Visual.SetParent(transform, false);
            AddBlock("Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 1.1f, 0.6f), new Color(0.20f, 0.45f, 0.85f));
            AddBlock("Head", new Vector3(0f, 1.48f, 0f), new Vector3(0.7f, 0.7f, 0.7f), new Color(0.95f, 0.80f, 0.62f));
            AddBlock("Nose", new Vector3(0f, 1.48f, 0.4f), new Vector3(0.15f, 0.15f, 0.15f), new Color(0.15f, 0.15f, 0.15f));
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

        /// <summary>Thiếu mana: thanh mana nháy (0034 mục 5).</summary>
        public void FlashMana() => manaFlashLeft = PlayerMotion.ManaFlashSeconds;

        public void Place(Vector3 feetWorld, float yawDeg)
        {
            Controller.enabled = false;
            transform.SetPositionAndRotation(feetWorld, Quaternion.identity);
            Controller.enabled = true;
            Visual.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            vy = 0f;
            ClickMove = ClickMoveState.None;
            Dash?.Stop();
            stuck?.Reset();
            Ready = false;
            if (follow != null) follow.SetAngles(yawDeg, follow.defaultPitch);
            else { pendingYaw = yawDeg; hasPendingYaw = true; }
            if (terrain != null) { spawnFeetLocal = terrain.transform.InverseTransformPoint(feetWorld); spawnYaw = yawDeg; hasSpawn = true; }
            if (terrain != null && !hasRespawn) { respawnFeetLocal = terrain.transform.InverseTransformPoint(feetWorld); respawnYaw = yawDeg; hasRespawn = true; }
            airborne = false;
            if (terrain != null) airPeakY = terrain.transform.InverseTransformPoint(feetWorld).y;
        }

        /// <summary>Mana hồi theo quãng đi, rơi cao, ngập đầu, hồi sinh (0034 mục 5, brief 0060).</summary>
        void UpdateVitals(Vector3 movedWorld)
        {
            var fa = FeetLocal;
            if (!FlyMode)
            {
                var ml = terrain.transform.InverseTransformVector(movedWorld);
                float cells = Mathf.Sqrt(ml.x * ml.x + ml.z * ml.z);
                if (cells > 0f) Vitals.AddMoved(cells);
            }
            if (FlyMode || PlayerRules.InWater(terrain.World, fa.x, fa.y, fa.z)) { airborne = false; airPeakY = fa.y; }
            else if (!Controller.isGrounded) { if (!airborne) { airborne = true; airPeakY = Mathf.Max(airPeakY, fa.y); } else airPeakY = Mathf.Max(airPeakY, fa.y); }
            else
            {
                if (airborne) Vitals.Land(Mathf.Max(0f, airPeakY - fa.y));
                airborne = false;
                airPeakY = fa.y;
            }
            HeadUnderwater = PlayerRules.HeadUnderwater(terrain.World, fa.x, fa.y, fa.z, Game.World.BlockMesher.WaterSurfaceDrop);
            if (!FlyMode)
            {
                vitalsMsAcc += Time.deltaTime * 1000f;
                int ms = (int)vitalsMsAcc;
                vitalsMsAcc -= ms;
                if (ms > 0) Vitals.Tick(ms, HeadUnderwater);
            }
            if (Vitals.IsDead) RespawnAtStart();
        }

        /// <summary>Máu về 0 (0034 mục 5): về chỗ xuất phát, đầy máu + mana, không mất đồ.</summary>
        void RespawnAtStart()
        {
            StopAutoWalk();
            if (hasRespawn) Place(terrain.transform.TransformPoint(respawnFeetLocal), respawnYaw);
            Vitals.Respawn();
            Respawns++;
        }

        public void SetFlyMode(bool on)
        {
            if (on) { ClickMove = ClickMoveState.None; Dash?.Stop(); }
            if (fly == null) { FlyMode = false; return; }
            FlyMode = on;
            fly.enabled = on;
            if (on) fly.SyncAngles();
            if (follow != null)
            {
                if (on && follow.FirstPerson) follow.SetFirstPerson(false);
                follow.enabled = !on;
                if (!on) follow.SnapNow();
            }
        }

        public bool InTargetReach(Vector3Int cell)
        {
            var f = FeetLocal;
            return PlayerRules.InTargetReach(f.x, f.y, f.z, cell.x, cell.y, cell.z);
        }

        public bool InActReach(Vector3Int cell)
        {
            var f = FeetLocal;
            return PlayerRules.InActReach(f.x, f.y, f.z, cell.x, cell.y, cell.z);
        }

        public bool InDigReach(Vector3Int cell)
        {
            var f = FeetLocal;
            return PlayerRules.InDigReach(f.x, f.y, f.z, cell.x, cell.y, cell.z);
        }

        public bool OccupiesCell(int x, int y, int z)
        {
            var f = FeetLocal;
            return PlayerRules.Occupies(f.x, f.y, f.z, x, y, z);
        }

        void ReadPointerMove()
        {
            if (!GameInput.HasPointer || MinimapView.IsAnyOpen || cam == null) return;
            bool pressed = GameInput.MoveClick.WasPressedThisFrame();
            if (!pressed && !(holdFromWorld && GameInput.MoveClick.IsPressed())) return;
            var pos = GameInput.PointerPosition;
            bool blocked = MinimapView.BlocksPointer(pos) || TimeDial.BlocksPointer(pos) || SimView.BlocksPointer(pos) || ObservationCamera.BlocksPointer(pos);
            if (pressed) holdFromWorld = !blocked;
            if (blocked) return;
            ClickMoveRay(cam.ScreenPointToRay(pos), pressed);
        }

        void Update()
        {
            if (terrain == null || terrain.World == null) return;
            if (manaFlashLeft > 0f) manaFlashLeft -= Time.deltaTime;
            if (terrain.Editor != null && terrain.Editor.IsPlayerAt == null)
                terrain.Editor.IsPlayerAt = OccupiesCell;
            if (giveStarterBag && hasSpawn && terrain.Editor != null && starterGivenFor != terrain.Editor)
            {
                starterGivenFor = terrain.Editor;
                float r = spawnYaw * Mathf.Deg2Rad;
                StarterStack = StarterBag.Place(terrain.Editor, spawnFeetLocal.x, spawnFeetLocal.y, spawnFeetLocal.z, Mathf.RoundToInt(Mathf.Sin(r)), Mathf.RoundToInt(Mathf.Cos(r)));
                if (StarterStack != null) Debug.Log($"STARTERBAG seed bag Crop A x {StarterBag.Seeds} at ({StarterStack.X}, {StarterStack.Y}, {StarterStack.Z})");
                else Debug.LogWarning("STARTERBAG could not find a place for the seed bag near the spawn point");
            }

            if (!ObservationCamera.InputLocked && DebugKeys.WasPressed(Key.F3)) SetFlyMode(!FlyMode);

            if (!Ready)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 64f) && terrain.IsTerrainCollider(hit.collider))
                    Ready = true;
                else
                    return;
            }

            if (FlyMode || ObservationCamera.InputLocked)
            {
                MoveInput = Vector2.zero;
                JumpInput = false;
                if (ClickMove == ClickMoveState.Walking) ClickMove = ClickMoveState.None;
                Dash.Stop();
            }
            else if (readKeyboard)
            {
                JumpInput = GameInput.Jump.IsPressed();
                ReadPointerMove();
                if (GameInput.Dash.WasPressedThisFrame() && !SimView.AnyPanelOpen) TryDash();
            }

            var f = FeetLocal;
            InWater = PlayerRules.InWater(terrain.World, f.x, f.y, f.z);

            if (AutoWalk == AutoWalkState.Walking && HasMoveIntent)
                AutoWalk = AutoWalkState.Cancelled;
            bool autoJump = false;
            float step = walkSpeed * (InWater ? PlayerRules.WaterSpeedFactor : 1f) * Time.deltaTime;
            float yaw = follow != null ? follow.yaw : 0f;
            var dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(MoveInput.x, 0f, MoveInput.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            if (AutoWalk == AutoWalkState.Walking)
            {
                autoStall += Time.deltaTime;
                var t = autoPath[autoIndex];
                float dx = t.X + 0.5f - f.x, dz = t.Z + 0.5f - f.z;
                if (Mathf.Sqrt(dx * dx + dz * dz) < 0.15f)
                {
                    autoIndex++;
                    autoStall = 0f;
                    if (autoIndex >= autoPath.Count) AutoWalk = AutoWalkState.Arrived;
                    else t = autoPath[autoIndex];
                }
                if (AutoWalk == AutoWalkState.Walking)
                {
                    dx = t.X + 0.5f - f.x; dz = t.Z + 0.5f - f.z;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    dir = dist > 1e-4f ? terrain.transform.TransformDirection(new Vector3(dx / dist, 0f, dz / dist)) : Vector3.zero;
                    step = Mathf.Min(step, dist);
                    autoJump = t.Y > Mathf.FloorToInt(f.y + 0.05f);
                    if (autoStall > 3f) AutoWalk = AutoWalkState.Failed;
                }
            }
            else if (ClickMove == ClickMoveState.Walking)
            {
                float dx = ClickTargetLocal.x - f.x, dz = ClickTargetLocal.z - f.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist <= PlayerMotion.ArriveDistance)
                {
                    ClickMove = ClickMoveState.Arrived;
                    dir = Vector3.zero;
                }
                else
                {
                    dir = terrain.transform.TransformDirection(new Vector3(dx / dist, 0f, dz / dist));
                    step = Mathf.Min(step, dist);
                    autoJump = Controller.isGrounded && PlayerMotion.ShouldStepUp(terrain.World, f.x, f.y, f.z, dx / dist, dz / dist);
                }
            }
            var h = dir * step;

            bool grounded = Controller.isGrounded;
            Dash.Tick(Time.deltaTime, grounded);
            if (Dashing)
            {
                h = dashDir * Dash.Step(Time.deltaTime);
                vy = 0f;
            }
            else
            {
                if (grounded && vy < 0f) vy = -2f;
                if (grounded && (JumpInput || autoJump)) vy = jumpSpeed;
                vy = Mathf.Max(vy - gravity * Time.deltaTime, -maxFallSpeed);
            }

            var lh = terrain.transform.InverseTransformVector(h);
            float lx = lh.x, lz = lh.z;
            PlayerRules.ClampToWorld(terrain.World.Size, f.x, f.z, ref lx, ref lz);
            h = terrain.transform.TransformVector(new Vector3(lx, 0f, lz));

            var before = transform.position;
            Controller.Move(h + Vector3.up * (vy * Time.deltaTime));
            var moved = transform.position - before;
            float movedH = Mathf.Sqrt(moved.x * moved.x + moved.z * moved.z);
            float wantH = Mathf.Sqrt(h.x * h.x + h.z * h.z);
            if (Dashing && movedH < wantH * 0.5f) Dash.Stop();
            UpdateVitals(moved);
            if (ClickMove == ClickMoveState.Walking && !Dashing)
            {
                if (stuck.Update(Time.deltaTime, wantH > 1e-5f && movedH >= wantH * 0.25f)) ClickMove = ClickMoveState.Blocked;
            }
            else stuck.Reset();

            if (Dashing)
            {
                Visual.rotation = Quaternion.LookRotation(dashDir);
                MoveDirection = dashDir;
            }
            else
            {
                if (dir.sqrMagnitude > 0.01f)
                    Visual.rotation = Quaternion.Slerp(Visual.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime);
                MoveDirection = dir.sqrMagnitude > 0.01f ? new Vector3(dir.x, 0f, dir.z).normalized : Vector3.zero;
            }
            Visual.localPosition = Controller.isGrounded && dir.sqrMagnitude > 0.01f
                ? new Vector3(0f, bobHeight * Mathf.Abs(Mathf.Sin(Time.time * bobFrequency)), 0f)
                : Vector3.zero;
        }
    }
}
