using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 5j lỗi 4 (brief 0086): đứng / click đi tới dưới chân thác demo không bị đẩy lên 1 ô.</summary>
    public class UnderFallPlayTests
    {
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        SimView sv;
        PlayerCharacter player;
        int cx, cz, g;
        float lastMaxY;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<DebugFlyCamera>();
            camGo.AddComponent<FollowCamera>();
            go = new GameObject("Terrain");
            go.SetActive(false);
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            sv = go.AddComponent<SimView>();
            playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            player = playerGo.AddComponent<PlayerCharacter>();
            player.terrain = view;
            player.readKeyboard = false;
            playerGo.SetActive(true);
            go.SetActive(true);
            for (int i = 0; i < 600 && sv.Sim == null; i++) yield return null;
            Assert.IsNotNull(sv.Sim);
            yield return WaitSettled();
            var plain = sv.Plain.Value;
            cx = plain.X0 + plain.Size / 2;
            cz = plain.Z0 + plain.Size / 2;
            g = plain.Ground;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(playerGo);
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        IEnumerator WaitSettled()
        {
            float t = 0f;
            while (t < 5f && view.Editor.HasPendingWater)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(view.Editor.HasPendingWater);
        }

        IEnumerator PlaceAt(Vector3 feetLocal)
        {
            player.Place(view.transform.TransformPoint(feetLocal), 0f);
            for (int i = 0; i < 600 && !player.Ready; i++) yield return null;
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(player.Ready);
        }

        IEnumerator Track(float seconds, string tag)
        {
            float maxY = float.MinValue;
            bool logged = false;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                var f = player.FeetLocal;
                maxY = Mathf.Max(maxY, f.y);
                if (!logged && f.y > g + 0.3f)
                {
                    logged = true;
                    int x = Mathf.FloorToInt(f.x), y = Mathf.FloorToInt(f.y), z = Mathf.FloorToInt(f.z);
                    var w = view.World;
                    string below = w.Size.InBounds(x, Mathf.FloorToInt(f.y - 0.05f), z) ? w.GetMaterial(x, Mathf.FloorToInt(f.y - 0.05f), z).ToString() : "oob";
                    string at = w.Size.InBounds(x, y, z) ? w.GetMaterial(x, y, z).ToString() : "oob";
                    string col = "none";
                    var from = view.transform.TransformPoint(f + Vector3.up * 0.5f);
                    if (Physics.Raycast(from, Vector3.down, out var hit, 4f))
                        col = hit.collider.name + " y=" + view.transform.InverseTransformPoint(hit.point).y.ToString("F2");
                    Debug.Log($"UNDERFALL {tag} feet={f:F2} vy={player.VerticalSpeed:F2} grounded={player.Controller.isGrounded} inWater={player.InWater} click={player.ClickMove} target={player.ClickTargetLocal:F2} below={below} at={at} ray={col}");
                }
                yield return null;
            }
            lastMaxY = maxY;
            Debug.Log($"UNDERFALL {tag} maxY={lastMaxY:F2} target={player.ClickTargetLocal:F2} state={player.ClickMove}");
        }

        [UnityTest]
        public IEnumerator UnderFallStandStill()
        {
            yield return PlaceAt(new Vector3(cx + 13.5f, g, cz - 10.5f));
            yield return Track(3f, "stand");
            Assert.Less(lastMaxY, g + 0.3f);
        }

        [UnityTest]
        public IEnumerator UnderFallClickMoveNoStepUp()
        {
            yield return PlaceAt(new Vector3(cx + 11.5f, g, cz - 10.5f));
            player.MoveTo(new Vector3(cx + 13.5f, 0f, cz - 10.5f));
            yield return Track(4f, "move");
            Assert.Less(lastMaxY, g + 0.3f);
            Assert.AreEqual(ClickMoveState.Arrived, player.ClickMove);
        }

        [UnityTest]
        public IEnumerator UnderFallClickThroughColumn()
        {
            yield return PlaceAt(new Vector3(cx + 11.5f, g, cz - 10.5f));
            var o = new Vector3(cx + 8.5f, g + 4f, cz - 10.5f);
            var target = new Vector3(cx + 13.5f, g + 0.9f, cz - 10.5f);
            var ray = new Ray(view.transform.TransformPoint(o), view.transform.TransformDirection((target - o).normalized));
            Assert.IsTrue(player.ClickMoveRay(ray, false));
            Assert.That(player.ClickTargetLocal.x, Is.GreaterThanOrEqualTo(cx + 13 - 0.01f).And.LessThan(cx + 14f));
            yield return Track(4f, "ray");
            Assert.Less(lastMaxY, g + 0.3f);
        }
    }
}
