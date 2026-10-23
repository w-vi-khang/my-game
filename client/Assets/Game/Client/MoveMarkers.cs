using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>Dấu click + vòng dưới chân + mũi tên hướng đi (0034 mục 1, world 7l b). LineRenderer, shader Game/SelectionOverlay.</summary>
    public class MoveMarkers : MonoBehaviour
    {
        public float clickRadius = 0.35f, ringRadius = 0.6f;
        public bool ClickVisible => click.enabled;
        /// <summary>Điểm click (world) lần cuối.</summary>
        public Vector3 ClickPoint { get; private set; }
        public float ClickAlpha { get; private set; }
        public bool RingVisible => ring.enabled;
        public bool ArrowVisible => arrow.enabled;

        LineRenderer click, ring, arrow;
        PlayerCharacter player;
        float clickAge;

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
