using UnityEngine;

namespace Game.Client
{
    /// <summary>Observation camera view state (task 7j, brief 0053).</summary>
    public enum ObservationState { Off, Loading, Viewing }

    /// <summary>
    /// Observation camera (task 7j, brief 0053, decision 0029): one placed pose in the world (M1: max 1, placing again moves it).
    /// V / HUD button: register the pose as the second streaming viewpoint, show the loading text until the columns within
    /// loadRadius are loaded, then move Camera.main there with its follow/fly controllers disabled.
    /// V / button / Esc: restore Camera.main and remove the viewpoint. While Loading or Viewing, tools and player movement are locked.
    /// Placed by right-clicking a block: ObservationHeight cells above its top face, facing like Camera.main. While viewing, middle mouse + move rotates the view; the direction is kept until placed again (decision 0035).
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class ObservationCamera : MonoBehaviour
    {
        static ObservationCamera current;

        TerrainChunkView view;
        Camera shownCamera;
        FollowCamera follow;
        DebugFlyCamera fly;
        bool followWasEnabled, flyWasEnabled;
        Vector3 savedPosition;
        Quaternion savedRotation;
        int exitFrame = -1;
        Renderer[] markerRenderers;
        const float MarkerHideDistance = 1.5f;
        GUIStyle loadingStyle;
        float lookYaw, lookPitch;
        public const float LookPitchMin = -80f, LookPitchMax = 85f;

        public ObservationState State { get; private set; }
        /// <summary>Placed pose (also the streaming viewpoint while engaged); null until first placed.</summary>
        public Transform Point { get; private set; }
        public GameObject Marker { get; private set; }
        public bool HasCamera => Point != null;
        public bool Engaged => State != ObservationState.Off;
        public bool MarkerVisible => Marker != null && Marker.activeSelf;
        /// <summary>Marker is active and its renderers are on (hidden while Camera.main is within MarkerHideDistance).</summary>
        public bool MarkerRendered => MarkerVisible && markerRenderers[0].enabled;
        public bool ButtonVisible => HasCamera;
        public bool LoadingVisible => State == ObservationState.Loading;
        public string ButtonText => State == ObservationState.Off ? T("observe.button_view") : T("observe.button_back");

        /// <summary>True while the current observation camera is Loading or Viewing: player movement and tools are locked.</summary>
        public static bool InputLocked => current != null && current.Engaged;
        /// <summary>Esc belongs to the observation view this frame (engaged, or it just exited) — Settings must not open.</summary>
        public static bool BlocksBack => current != null && (current.Engaged || current.exitFrame == Time.frameCount);
        /// <summary>Screen point (origin bottom-left) is over the HUD button.</summary>
        public static bool BlocksPointer(Vector2 screen) => current != null && current.Covers(screen);
        public static Rect ButtonRect(float gw, float gh) => new Rect(gw - 380f, gh - 46f, 130f, 36f);

        static string T(string k) => GameSettings.Text.Get(k);

        void Awake()
        {
            current = this;
            view = GetComponent<TerrainChunkView>();
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }

        void Update()
        {
            HandleKeys(GameInput.ToggleObservation.WasPressedThisFrame(), GameInput.Back.WasPressedThisFrame());
            if (State == ObservationState.Viewing && GameInput.HasPointer && GameInput.RotateCamera.IsPressed() && !MinimapView.IsAnyOpen && !SettingsView.IsAnyOpen)
                ApplyLook(GameInput.Look.ReadValue<Vector2>(), follow != null ? follow.lookSensitivity : 0.3f, follow != null && follow.invertY);
            if (State == ObservationState.Loading && view.MissingColumnsWithin(Point, view.loadRadius) == 0) ShowView();
        }

        void LateUpdate()
        {
            if (!MarkerVisible) return;
            var cam = Camera.main;
            bool show = cam == null || (cam.transform.position - Point.position).sqrMagnitude > MarkerHideDistance * MarkerHideDistance;
            foreach (var r in markerRenderers) r.enabled = show;
        }

        /// <summary>V toggles; Esc only exits. Split from Update for tests.</summary>
        public void HandleKeys(bool togglePressed, bool backPressed)
        {
            if (togglePressed) Toggle();
            else if (backPressed && Engaged) Exit();
        }

        public bool Covers(Vector2 screen)
        {
            if (!ButtonVisible || MinimapView.IsAnyOpen) return false;
            float s = UiScale.Gui, gw = Screen.width / s, gh = Screen.height / s;
            var g = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
            return ButtonRect(gw, gh).Contains(g);
        }

        /// <summary>Places (or moves) the single observation camera. False while engaged.</summary>
        public bool Place(Vector3 position, Quaternion rotation)
        {
            if (Engaged) return false;
            if (Point == null) BuildPoint();
            var e = rotation.eulerAngles;
            lookYaw = e.y;
            lookPitch = Mathf.Clamp(e.x > 180f ? e.x - 360f : e.x, LookPitchMin, LookPitchMax);
            Point.SetPositionAndRotation(position, Quaternion.Euler(lookPitch, lookYaw, 0f));
            return true;
        }

        /// <summary>Viewing only: rotate the observation pose and the shown camera by a mouse delta (decision 0035). The new direction is kept for later views until Place. False when not Viewing.</summary>
        public bool ApplyLook(Vector2 delta, float sensitivity, bool invertY)
        {
            if (State != ObservationState.Viewing) return false;
            lookYaw += delta.x * sensitivity;
            lookPitch = Mathf.Clamp(lookPitch - delta.y * sensitivity * (invertY ? -1f : 1f), LookPitchMin, LookPitchMax);
            Point.rotation = Quaternion.Euler(lookPitch, lookYaw, 0f);
            if (shownCamera != null) shownCamera.transform.rotation = Point.rotation;
            return true;
        }

        /// <summary>Off → start viewing (false if no camera or no free viewpoint); engaged → exit (true).</summary>
        public bool Toggle()
        {
            if (Engaged)
            {
                Exit();
                return true;
            }
            return Enter();
        }

        bool Enter()
        {
            if (!HasCamera) return false;
            try
            {
                view.AddViewpoint(Point, view.loadRadius, view.unloadRadius);
            }
            catch (System.InvalidOperationException e)
            {
                Debug.LogWarning("OBSERVE no free streaming viewpoint, view not opened: " + e.Message);
                return false;
            }
            var player = PlayerCharacter.Active;
            if (player != null) player.StopAutoWalk();
            var farming = GetComponent<PlayerFarmActions>();
            if (farming != null)
            {
                farming.CancelSelfTill();
                farming.ExitSowMode();
            }
            shownCamera = Camera.main;
            if (shownCamera != null)
            {
                shownCamera.transform.GetPositionAndRotation(out savedPosition, out savedRotation);
                follow = shownCamera.GetComponent<FollowCamera>();
                fly = shownCamera.GetComponent<DebugFlyCamera>();
                followWasEnabled = follow != null && follow.enabled;
                flyWasEnabled = fly != null && fly.enabled;
                if (follow != null) follow.enabled = false;
                if (fly != null) fly.enabled = false;
            }
            State = ObservationState.Loading;
            if (view.MissingColumnsWithin(Point, view.loadRadius) == 0) ShowView();
            return true;
        }

        void ShowView()
        {
            State = ObservationState.Viewing;
            if (shownCamera != null) shownCamera.transform.SetPositionAndRotation(Point.position, Point.rotation);
            Marker.SetActive(false);
        }

        /// <summary>Back to the player's camera: remove the viewpoint, restore the camera pose and controllers.</summary>
        public void Exit()
        {
            if (!Engaged) return;
            view.RemoveViewpoint(Point);
            if (shownCamera != null)
            {
                shownCamera.transform.SetPositionAndRotation(savedPosition, savedRotation);
                if (fly != null) fly.enabled = flyWasEnabled;
                if (follow != null)
                {
                    follow.enabled = followWasEnabled;
                    if (followWasEnabled) follow.SnapNow();
                }
            }
            shownCamera = null;
            follow = null;
            fly = null;
            Marker.SetActive(true);
            State = ObservationState.Off;
            exitFrame = Time.frameCount;
        }

        void OnGUI()
        {
            if (!ButtonVisible || MinimapView.IsAnyOpen) return;
            float scale = UiScale.Gui;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float gw = Screen.width / scale, gh = Screen.height / scale;
            if (GUI.Button(ButtonRect(gw, gh), ButtonText)) Toggle();
            if (LoadingVisible)
            {
                loadingStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
                GUI.Label(new Rect(gw * 0.5f - 150f, gh * 0.5f - 25f, 300f, 50f), T("observe.loading"), loadingStyle);
            }
        }

        void BuildPoint()
        {
            Point = new GameObject("ObservationPoint").transform;
            Point.SetParent(transform, false);
            Marker = new GameObject("Marker");
            Marker.transform.SetParent(Point, false);
            AddBlock("Body", Vector3.zero, new Vector3(0.5f, 0.4f, 0.6f), new Color(1f, 0.55f, 0.1f));
            AddBlock("Lens", new Vector3(0f, 0f, 0.35f), new Vector3(0.2f, 0.2f, 0.2f), new Color(0.1f, 0.1f, 0.1f));
            markerRenderers = Marker.GetComponentsInChildren<Renderer>(true);
        }

        void AddBlock(string name, Vector3 center, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            go.transform.SetParent(Marker.transform, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            var src = Resources.Load<UnityEngine.Material>("Materials/Landmark");
            var mat = src != null ? new UnityEngine.Material(src) : new UnityEngine.Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial);
            mat.SetColor("_BaseColor", color);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
