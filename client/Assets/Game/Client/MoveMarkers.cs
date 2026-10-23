using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>Dấu click + vòng dưới chân + mũi tên hướng đi (0034 mục 1, world 7l b) + vòng tầm thao tác khi chuột phải ngoài tầm, mờ dần (0036 mục 5) + vòng vùng Thu + vệt gió khi bấm F (0036 mục 6). LineRenderer, shader Game/SelectionOverlay.</summary>
    public class MoveMarkers : MonoBehaviour
    {
        public float clickRadius = 0.35f, ringRadius = 0.6f;
        public bool ClickVisible => click.enabled;
        /// <summary>Điểm click (world) lần cuối.</summary>
        public Vector3 ClickPoint { get; private set; }
        public float ClickAlpha { get; private set; }
        public bool RingVisible => ring.enabled;
        public bool ArrowVisible => arrow.enabled;
        public bool ReachRingVisible => reach.enabled;
        /// <summary>Alpha hiện tại của vòng tầm (0 khi tắt).</summary>
        public float ReachRingAlpha { get; private set; }
        /// <summary>Chuột phải ngoài tầm thao tác (0036 mục 5): bật vòng tầm, alpha 0,25 rồi mờ hết trong PlayerMotion.ReachRingFadeSeconds.</summary>
        public void FlashReachRing()
        {
            reachAge = 0f;
            ReachRingAlpha = 0.25f;
            SetColor(reach, new Color(1f, 1f, 1f, 0.25f));
            reach.enabled = true;
        }
        public bool CollectVisible => collect.enabled;
        public float CollectAlpha { get; private set; }
        /// <summary>Số vệt gió đang bật (0 hoặc WindStreaks).</summary>
        public int WindVisibleCount { get; private set; }
        public const int WindStreaks = 8;
        static readonly Color CollectColor = new Color(0.75f, 0.95f, 1f, 0.5f);

        /// <summary>F Thu (0036 mục 6): vòng vùng bán kính PlayerRules.CollectReach + 8 vệt gió xoáy vào, mờ hết trong PlayerMotion.CollectShowSeconds. Gọi lại → bắt đầu lại.</summary>
        public void ShowCollect()
        {
            collectAge = 0f;
            collect.enabled = true;
            foreach (var w in wind) w.enabled = true;
            UpdateCollect(0f);
        }

        void UpdateCollect(float k)
        {
            CollectAlpha = 0.5f * (1f - k);
            var c = new Color(CollectColor.r, CollectColor.g, CollectColor.b, CollectAlpha);
            SetColor(collect, c);
            float r1 = PlayerRules.CollectReach * (1f - k) + 0.3f * k;
            float r0 = Mathf.Max(0.2f, r1 - 0.6f);
            for (int i = 0; i < WindStreaks; i++)
            {
                float a = (i * 45f + 22.5f + 90f * k) * Mathf.Deg2Rad;
                SetColor(wind[i], c);
                wind[i].SetPosition(0, new Vector3(Mathf.Cos(a) * r1, 0f, Mathf.Sin(a) * r1));
                wind[i].SetPosition(1, new Vector3(Mathf.Cos(a) * r0, 0f, Mathf.Sin(a) * r0));
            }
            WindVisibleCount = WindStreaks;
        }

        LineRenderer click, ring, arrow, reach, collect;
        LineRenderer[] wind;
        PlayerCharacter player;
        float clickAge, reachAge, collectAge;

        void Awake()
        {
            player = GetComponent<PlayerCharacter>();
            var clickGo = new GameObject("ClickMark");
            click = Make(clickGo, Circle(24, clickRadius), true, 0.06f);
            click.enabled = false;
            ring = Make(Child("MoveRing"), Circle(32, ringRadius), true, 0.05f);
            SetColor(ring, new Color(1f, 1f, 1f, 0.35f));
            arrow = Make(Child("MoveArrow"), new[] { new Vector3(-0.25f, 0f, 0.55f), new Vector3(0f, 0f, 0.85f), new Vector3(0.25f, 0f, 0.55f) }, false, 0.06f);
            SetColor(arrow, new Color(1f, 1f, 1f, 0.6f));
            ring.enabled = false;
            arrow.enabled = false;
            reach = Make(Child("ReachRing"), Circle(64, PlayerRules.TargetReach), true, 0.08f);
            SetColor(reach, new Color(1f, 1f, 1f, 0.25f));
            reach.enabled = false;
            collect = Make(Child("CollectRing"), Circle(48, PlayerRules.CollectReach), true, 0.06f);
            collect.enabled = false;
            wind = new LineRenderer[WindStreaks];
            for (int i = 0; i < WindStreaks; i++)
            {
                var w = Child("Wind" + i);
                w.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                wind[i] = Make(w, new[] { Vector3.zero, Vector3.zero }, false, 0.05f);
                wind[i].enabled = false;
            }
        }

        void OnDestroy()
        {
            if (click != null) Destroy(click.gameObject);
        }

        GameObject Child(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            return go;
        }

        static Vector3[] Circle(int n, float r)
        {
            var p = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                p[i] = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }
            return p;
        }

        static LineRenderer Make(GameObject go, Vector3[] pts, bool loop, float width)
        {
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = loop;
            lr.widthMultiplier = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = Shader.Find("Game/SelectionOverlay");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = shader != null
                ? new UnityEngine.Material(shader)
                : new UnityEngine.Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial);
            mat.SetColor("_BaseColor", Color.white);
            lr.sharedMaterial = mat;
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
            return lr;
        }

        static void SetColor(LineRenderer lr, Color c)
        {
            lr.startColor = c;
            lr.endColor = c;
        }

        public void ShowClick(Vector3 pointWorld, Vector3 normalWorld)
        {
            ClickPoint = pointWorld;
            click.transform.SetPositionAndRotation(pointWorld + normalWorld * 0.03f, Quaternion.FromToRotation(Vector3.up, normalWorld));
            clickAge = 0f;
            ClickAlpha = 1f;
            SetColor(click, Color.white);
            click.enabled = true;
        }

        void LateUpdate()
        {
            if (reach.enabled)
            {
                reachAge += Time.deltaTime;
                float k = reachAge / PlayerMotion.ReachRingFadeSeconds;
                if (k >= 1f) { reach.enabled = false; ReachRingAlpha = 0f; }
                else { ReachRingAlpha = 0.25f * (1f - k); SetColor(reach, new Color(1f, 1f, 1f, ReachRingAlpha)); }
            }
            if (collect.enabled)
            {
                collectAge += Time.deltaTime;
                float k = collectAge / PlayerMotion.CollectShowSeconds;
                if (k >= 1f)
                {
                    collect.enabled = false;
                    foreach (var w in wind) w.enabled = false;
                    CollectAlpha = 0f;
                    WindVisibleCount = 0;
                }
                else UpdateCollect(k);
            }
            if (click.enabled)
            {
                clickAge += Time.deltaTime;
                ClickAlpha = 1f - clickAge / PlayerMotion.ClickMarkSeconds;
                if (ClickAlpha <= 0f)
                {
                    click.enabled = false;
                    ClickAlpha = 0f;
                }
                else SetColor(click, new Color(1f, 1f, 1f, ClickAlpha));
            }
            ring.enabled = player.Ready && !player.FlyMode;
            arrow.enabled = ring.enabled && player.MoveDirection.sqrMagnitude > 0.01f;
            if (arrow.enabled) arrow.transform.rotation = Quaternion.LookRotation(player.MoveDirection);
        }
    }
}
