using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7l b2 (brief 0056): camera PC — mặc định chéo cao, Space về sau lưng, X góc nhìn thứ nhất.</summary>
    public class CameraPcPlayTests
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

        Vector3 Spot => new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f);
        Vector3 Feet => player.transform.position;

        IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds) { yield return null; t += Time.deltaTime; }
        }

        [UnityTest]
        public IEnumerator DefaultHighAngleFollows()
        {
            yield return Teleport(Spot);
            Assert.AreEqual(55f, follow.pitch, 0.01f);
            Assert.AreEqual(14f, follow.distance, 0.01f);
            Assert.AreEqual(55f, follow.defaultPitch, 0.01f);
            Assert.AreEqual(14f, follow.defaultDistance, 0.01f);
            float yaw0 = follow.yaw;
            player.MoveTo(Spot + new Vector3(4.5f, 0f, 0.5f));
            yield return Wait(1.5f);
            Assert.AreEqual(yaw0, follow.yaw, 0.001f);
            float d = Vector3.Distance(camGo.transform.position, Feet + Vector3.up * follow.pivotHeight);
            Assert.LessOrEqual(d, 14.01f);
        }

        [UnityTest]
        public IEnumerator ResetBehindUsesFacing()
        {
            yield return Teleport(Spot);
            follow.SetAngles(200f, 10f);
            follow.distance = 5f;
            player.Visual.rotation = Quaternion.Euler(0f, 90f, 0f);
            follow.ResetBehind();
            Assert.AreEqual(90f, follow.yaw, 0.01f);
            Assert.AreEqual(55f, follow.pitch, 0.01f);
            Assert.AreEqual(14f, follow.distance, 0.01f);
            var pivot = Feet + Vector3.up * follow.pivotHeight;
            var v = camGo.transform.position - pivot;
            var h = new Vector3(v.x, 0f, v.z).normalized;
            Assert.Greater(Vector3.Dot(h, new Vector3(-1f, 0f, 0f)), 0.9f);
            Assert.Greater(camGo.transform.position.y, pivot.y);
        }

        [UnityTest]
        public IEnumerator FirstPersonAtEyes()
        {
            yield return Teleport(Spot);
            player.Visual.rotation = Quaternion.Euler(0f, 90f, 0f);
            float y0 = follow.yaw, p0 = follow.pitch, d0 = follow.distance;
            follow.SetFirstPerson(true);
            yield return null;
            var pos = camGo.transform.position;
            var rot = camGo.transform.eulerAngles;
            var expect = Feet + new Vector3(0.25f, 1.5f, 0f);
            Assert.Less(Vector3.Distance(pos, expect), 0.05f);
            Assert.AreEqual(15f, Mathf.DeltaAngle(0f, rot.x), 0.5f);
            Assert.AreEqual(90f, rot.y, 0.5f);
            Assert.IsFalse(player.BodyVisible);
            follow.ApplyLook(new Vector2(100f, 50f));
            follow.ApplyZoom(1f);
            yield return null;
            Assert.Less(Vector3.Distance(camGo.transform.position, pos), 0.01f);
            Assert.AreEqual(rot.y, camGo.transform.eulerAngles.y, 0.01f);
            Assert.AreEqual(y0, follow.yaw, 0.0001f);
            Assert.AreEqual(p0, follow.pitch, 0.0001f);
            Assert.AreEqual(d0, follow.distance, 0.0001f);
        }

        [UnityTest]
        public IEnumerator ToggleAndResetFromFirstPerson()
        {
            yield return Teleport(Spot);
            follow.SetAngles(30f, 40f);
            follow.SetFirstPerson(true);
            follow.SetFirstPerson(false);
            Assert.AreEqual(30f, follow.yaw, 0.0001f);
            Assert.AreEqual(40f, follow.pitch, 0.0001f);
            Assert.IsTrue(player.BodyVisible);
            follow.SetFirstPerson(true);
            follow.ResetBehind();
            Assert.IsFalse(follow.FirstPerson);
            Assert.IsTrue(player.BodyVisible);
            Assert.AreEqual(55f, follow.pitch, 0.01f);
        }

        [UnityTest]
        public IEnumerator FirstPersonFollowsWalking()
        {
            yield return Teleport(Spot);
            follow.SetFirstPerson(true);
            player.MoveTo(Spot + new Vector3(6.5f, 0f, 0.5f));
            yield return Wait(1f);
            Assert.AreEqual(90f, camGo.transform.eulerAngles.y, 10f);
        }

        [UnityTest]
        public IEnumerator BodyVisibilityOnDisableAndFly()
        {
            yield return Teleport(Spot);
            follow.SetFirstPerson(true);
            follow.enabled = false;
            yield return null;
            Assert.IsTrue(player.BodyVisible);
            follow.enabled = true;
            yield return null;
            Assert.IsFalse(player.BodyVisible);
            player.SetFlyMode(true);
            yield return null;
            Assert.IsFalse(follow.FirstPerson);
            Assert.IsTrue(player.BodyVisible);
        }

        [UnityTest]
        public IEnumerator LookAndZoomThirdPerson()
        {
            yield return Teleport(Spot);
            follow.SetAngles(0f, 55f);
            follow.ApplyLook(new Vector2(100f, 0f));
            Assert.AreEqual(100f * follow.lookSensitivity, follow.yaw, 0.01f);
            for (int i = 0; i < 20; i++) follow.ApplyZoom(-1f);
            Assert.AreEqual(24f, follow.distance, 0.001f);
        }
    }
}
