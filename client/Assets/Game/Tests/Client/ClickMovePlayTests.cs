using System;
using Object = UnityEngine.Object;
using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7l b (brief 0055): click di chuyển, tự nhảy bậc, kẹt, lướt, dấu click, vòng + mũi tên, huỷ tự đi.</summary>
    public class ClickMovePlayTests
    {
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        DemoPlain dp;
        PlayerCharacter player;
        FollowCamera follow;
        StressPlain plain;
        Vector3Int A;
        int G;


        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Headless batchmode runs uncapped (dt ≈ 0.0003 s): CharacterController steps fall below skinWidth, so isGrounded flickers and walking stalls. Fix dt like a 60 fps frame.
            Time.captureFramerate = 60;
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<DebugFlyCamera>();
            follow = camGo.AddComponent<FollowCamera>();
            var uw = camGo.AddComponent<UnderwaterView>();
            camGo.transform.SetPositionAndRotation(new Vector3(192, 40, 150), Quaternion.Euler(25f, 0f, 0f));
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            dp = go.AddComponent<DemoPlain>();
            go.AddComponent<TerrainEditInput>();
            playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            player = playerGo.AddComponent<PlayerCharacter>();
            player.terrain = view;
            player.readKeyboard = false;
            playerGo.SetActive(true);
            uw.terrain = view;
            yield return null;
            yield return null;
            for (int i = 0; i < 60 && !player.Ready; i++)
                yield return null;
            Assert.IsTrue(dp.Plain.HasValue);
            plain = dp.Plain.Value;
            G = plain.Ground;
            A = new Vector3Int(plain.X0 + 6, plain.Ground, plain.Z0 + 6);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = 0;
            Object.Destroy(playerGo);
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        IEnumerator Run(Vector2 move, bool jump, float seconds)
        {
            player.MoveInput = move;
            player.JumpInput = jump;
            float t = 0f;
            while (t < seconds)
            {
                yield return null;
                t += Time.deltaTime;
            }
            player.MoveInput = Vector2.zero;
            player.JumpInput = false;
        }

        IEnumerator Teleport(Vector3 feetLocal)
        {
            player.Place(view.transform.TransformPoint(feetLocal), 0f);
            for (int i = 0; i < 300 && !player.Ready; i++)
                yield return null;
            if (!player.Ready)
                Assert.Fail($"Chunk not ready at {feetLocal}");
            yield return Run(Vector2.zero, false, 0.3f);
        }

        static Vector3Int C(int x, int y, int z) => new Vector3Int(x, y, z);

        IEnumerator Build(int x0, int x1, int y0, int y1, int z0, int z1)
        {
            int n = (x1 - x0 + 1) * (y1 - y0 + 1) * (z1 - z0 + 1);
            view.Bag.Add(BlockMaterial.Stone, n);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    for (int z = z0; z <= z1; z++)
                        Assert.AreEqual(EditResult.Ok, view.Place(C(x, y, z), BlockMaterial.Stone), $"Place ({x},{y},{z})");
            yield return null;
            yield return null;
        }


        Vector3 Feet => player.FeetLocal;

        Vector3 Pt(float dx, float dz) => new Vector3(A.x + dx, G + 1, A.z + dz);

        IEnumerator WaitWhile(Func<bool> cond, float maxSeconds)
        {
            float t = 0f;
            while (cond() && t < maxSeconds)
            {
                yield return null;
                t += Time.deltaTime;
            }
        }

        IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                yield return null;
                t += Time.deltaTime;
            }
        }

        float HDist(Vector3 a, Vector3 b) => Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));

        Ray Down(float x, float z) => new Ray(view.transform.TransformPoint(new Vector3(x, G + 20, z)), Vector3.down);

        [UnityTest]
        public IEnumerator ArrivesWithinTenth()
        {
            yield return Teleport(Pt(0.5f, 0.5f));
            var target = Pt(6f, 4f);
            player.MoveTo(target);
            yield return WaitWhile(() => player.ClickMove == ClickMoveState.Walking, 4f);
            Assert.AreEqual(ClickMoveState.Arrived, player.ClickMove);
            Assert.LessOrEqual(HDist(Feet, target), 0.1f);
            yield return null;
            Assert.AreEqual(Vector3.zero, player.MoveDirection);
        }

        [UnityTest]
        public IEnumerator ClickHeightFlattensAndMarksPoint()
        {
            yield return Teleport(Pt(0.5f, 0.5f));
            yield return Build(A.x + 4, A.x + 4, G + 1, G + 3, A.z, A.z);
            Assert.IsTrue(player.ClickMoveRay(Down(A.x + 4.5f, A.z + 0.5f), true));
            Assert.AreEqual(Feet.y, player.ClickTargetLocal.y, 0.01f);
            Assert.AreEqual(A.x + 4.5f, player.ClickTargetLocal.x, 0.05f);
            Assert.AreEqual(A.z + 0.5f, player.ClickTargetLocal.z, 0.05f);
            Assert.IsTrue(player.Markers.ClickVisible);
            Assert.AreEqual(G + 4f, view.transform.InverseTransformPoint(player.Markers.ClickPoint).y, 0.15f);
            player.StopClickMove();

            yield return Teleport(Pt(10.5f, 10.5f));
            yield return Build(A.x - 3, A.x + 1, G + 1, G + 1, A.z - 3, A.z + 1);
            yield return Teleport(new Vector3(A.x - 1.5f, G + 2, A.z - 1.5f));
            Assert.AreEqual(G + 2f, Feet.y, 0.1f);
            Assert.IsTrue(player.ClickMoveRay(Down(A.x + 6.5f, A.z + 0.5f), true));
            Assert.AreEqual(Feet.y, player.ClickTargetLocal.y, 0.05f);
            Assert.AreEqual(G + 1f, view.transform.InverseTransformPoint(player.Markers.ClickPoint).y, 0.15f);
            yield return Wait(0.6f);
            Assert.IsFalse(player.Markers.ClickVisible);
        }

        [UnityTest]
        public IEnumerator TwoHighWallStops()
        {
            yield return Teleport(Pt(0.5f, 0.5f));
            yield return Build(A.x - 2, A.x + 2, G + 1, G + 2, A.z + 3, A.z + 3);
            player.MoveTo(Pt(0.5f, 6.5f));
            yield return WaitWhile(() => player.ClickMove == ClickMoveState.Walking, 3f);
            Assert.AreEqual(ClickMoveState.Blocked, player.ClickMove);
            Assert.Less(Feet.z, A.z + 3 - 0.3f);
            Assert.Less(Feet.y, G + 2f);
        }

        [UnityTest]
        public IEnumerator ClimbsOneHighStep()
        {
            yield return Teleport(Pt(0.5f, 0.5f));
            yield return Build(A.x - 1, A.x + 1, G + 1, G + 1, A.z + 2, A.z + 2);
            player.MoveTo(Pt(0.5f, 5.5f));
            float maxY = 0f;
            float t = 0f;
            while (player.ClickMove == ClickMoveState.Walking && t < 4f)
            {
                yield return null;
                t += Time.deltaTime;
                maxY = Mathf.Max(maxY, Feet.y);
            }
            Assert.AreEqual(ClickMoveState.Arrived, player.ClickMove);
            Assert.GreaterOrEqual(maxY, G + 2f - 0.1f);
            Assert.AreEqual(A.z + 5.5f, Feet.z, 0.1f);
        }

        [UnityTest]
        public IEnumerator NewClickChangesDirection()
        {
            yield return Teleport(Pt(0.5f, 0.5f));
            player.MoveTo(Pt(6.5f, 0.5f));
            yield return Wait(0.3f);
            var turn = Feet;
            var t2 = Pt(0.5f, -5.5f);
            player.MoveTo(t2);
            yield return Wait(0.3f);
            Assert.GreaterOrEqual(turn.z - Feet.z, 0.8f);
            Assert.AreEqual(ClickMoveState.Walking, player.ClickMove);
            yield return WaitWhile(() => player.ClickMove == ClickMoveState.Walking, 4f);
            Assert.AreEqual(ClickMoveState.Arrived, player.ClickMove);
            Assert.LessOrEqual(HDist(Feet, t2), 0.1f);
        }

        [UnityTest]
        public IEnumerator JumpWhileWalkingKeepsHeading()
        {
            yield return Teleport(Pt(0.5f, 0.5f));
            player.MoveTo(Pt(0.5f, 9.5f));
            yield return Wait(0.2f);
            player.JumpInput = true;
            float zLeave = 0f;
            bool left = false;
            for (int i = 0; i < 60 && !left; i++)
            {
                yield return null;
                if (!player.Controller.isGrounded) { left = true; zLeave = Feet.z; }
            }
            player.JumpInput = false;
            Assert.IsTrue(left, "left the ground");
            for (int i = 0; i < 120 && !player.Controller.isGrounded; i++)
                yield return null;
            Assert.IsTrue(player.Controller.isGrounded, "landed");
            Assert.GreaterOrEqual(Feet.z - zLeave, 1.0f);
            Assert.AreNotEqual(ClickMoveState.None, player.ClickMove);
            Assert.AreNotEqual(ClickMoveState.Blocked, player.ClickMove);
        }

        [UnityTest]
        public IEnumerator Dash()
        {
            yield return Teleport(Pt(0.5f, 0.5f));
            var s = Feet;
            Assert.IsTrue(player.TryDash());
            yield return Wait(0.4f);
            Assert.AreEqual(4f, Feet.z - s.z, 0.15f);
            Assert.Less(Mathf.Abs(Feet.x - s.x), 0.1f);
            Assert.IsFalse(player.TryDash(), "cooldown");
            yield return Wait(3.1f);
            Assert.IsTrue(player.TryDash());
            yield return Wait(0.3f);

            player.Dash.Cooldown.Advance(10f);
            yield return Teleport(Pt(8.5f, 0.5f));
            yield return Build(A.x + 7, A.x + 9, G + 1, G + 2, A.z + 2, A.z + 2);
            Assert.IsTrue(player.TryDash());
            yield return Wait(0.4f);
            Assert.LessOrEqual(Feet.z, A.z + 2 - 0.35f);
            Assert.IsFalse(player.Dashing);

            player.Dash.Cooldown.Advance(10f);
            yield return Teleport(Pt(-4.5f, 0.5f));
            var target = Pt(-4.5f, 9.5f);
            player.MoveTo(target);
            yield return Wait(0.1f);
            Assert.IsTrue(player.TryDash());
            yield return WaitWhile(() => player.ClickMove == ClickMoveState.Walking, 3f);
            Assert.AreEqual(ClickMoveState.Arrived, player.ClickMove);
            Assert.LessOrEqual(HDist(Feet, target), 0.1f);
        }

        [UnityTest]
        public IEnumerator IntentAndMarkers()
        {
            yield return Teleport(Pt(0.5f, 0.5f));
            var path = new System.Collections.Generic.List<CellPos>();
            for (int i = 1; i <= 5; i++) path.Add(new CellPos(A.x + i, G + 1, A.z));
            player.StartAutoWalk(path);
            yield return null;
            player.MoveTo(Pt(-2.5f, 0.5f));
            yield return null;
            Assert.AreEqual(AutoWalkState.Cancelled, player.AutoWalk);
            player.SetFlyMode(true);
            yield return null;
            Assert.AreEqual(ClickMoveState.None, player.ClickMove);
            player.SetFlyMode(false);
            player.MoveTo(Pt(-2.5f, 5.5f));
            yield return Wait(0.2f);
            Assert.IsTrue(player.Markers.RingVisible);
            Assert.IsTrue(player.Markers.ArrowVisible);
            yield return WaitWhile(() => player.ClickMove == ClickMoveState.Walking, 4f);
            Assert.AreEqual(ClickMoveState.Arrived, player.ClickMove);
            yield return null;
            yield return null;
            Assert.IsTrue(player.Markers.RingVisible);
            Assert.IsFalse(player.Markers.ArrowVisible);
        }
    }
}
