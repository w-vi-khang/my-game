using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Cảm ứng (a), brief 0101: joystick, vuốt/chụm camera, nút Nhảy, chế độ cảm ứng (TouchControls.Feed thay cho chạm thật; dpi 160 → 1 dp = 1 px).</summary>
    public class TouchControlsPlayTests
    {
        const float Dpi = 160f;
        GameObject camGo, go, playerGo, touchGo;
        TerrainChunkView view;
        DemoPlain dp;
        PlayerCharacter player;
        FollowCamera follow;
        TouchControls touch;
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
            player.readKeyboard = true; // JumpInput from TouchJump is read in the keyboard branch
            playerGo.SetActive(true);
            uw.terrain = view;
            touchGo = new GameObject("Touch");
            touch = touchGo.AddComponent<TouchControls>();
            touch.readTouches = false;
            TouchControls.SetForceOn(true);
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
            TouchControls.SetForceOn(false);
            Object.Destroy(touchGo);
            Object.Destroy(playerGo);
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        static TouchSample S(int id, float x, float y, TouchSamplePhase ph) => new TouchSample(id, x, y, ph);

        void Feed(params TouchSample[] s) => touch.Feed(s, Screen.width, Dpi);

        Vector3 Feet => player.transform.position;
        Vector3 Spot => new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f);

        IEnumerator Teleport(Vector3 feetLocal)
        {
            player.Place(view.transform.TransformPoint(feetLocal), 0f);
            for (int i = 0; i < 300 && !player.Ready; i++)
                yield return null;
            if (!player.Ready)
                Assert.Fail($"Chunk not ready at {feetLocal}");
            for (int i = 0; i < 20; i++) yield return null;
        }

        static float R => PlayerMotion.TouchJoystickRadiusDp * Dpi / 160f;

        [UnityTest]
        public IEnumerator JoystickWalksForwardThenStops()
        {
            yield return Teleport(Spot);
            float x = Screen.width * 0.25f, y = Screen.height * 0.3f;
            var start = Feet;
            Feed(S(1, x, y, TouchSamplePhase.Began));
            for (int i = 0; i < 60; i++)
            {
                Feed(S(1, x, y + R, TouchSamplePhase.Moved));
                yield return null;
            }
            var moved = Feet - start;
            moved.y = 0f;
            var fwd = Quaternion.Euler(0f, follow.yaw, 0f) * Vector3.forward;
            Assert.Greater(Vector3.Dot(moved, fwd), 1f);
            Feed(S(1, x, y + R, TouchSamplePhase.Ended));
            var stop = Feet;
            for (int i = 0; i < 10; i++)
            {
                Feed();
                yield return null;
            }
            var drift = Feet - stop;
            drift.y = 0f;
            Assert.Less(drift.magnitude, 0.05f);
        }

        [UnityTest]
        public IEnumerator JoystickCancelsClickMove()
        {
            yield return Teleport(Spot);
            player.MoveTo(Spot + new Vector3(10f, 0f, 0.5f));
            yield return null;
            Assert.AreEqual(ClickMoveState.Walking, player.ClickMove);
            float x = Screen.width * 0.25f, y = Screen.height * 0.3f;
            Feed(S(1, x, y, TouchSamplePhase.Began));
            Feed(S(1, x, y + R, TouchSamplePhase.Moved));
            yield return null;
            Assert.AreNotEqual(ClickMoveState.Walking, player.ClickMove);
        }

        [UnityTest]
        public IEnumerator SwipeRightRotatesCamera()
        {
            yield return Teleport(Spot);
            float yaw0 = follow.yaw;
            float x = Screen.width * 0.75f, y = Screen.height * 0.5f;
            Feed(S(1, x, y, TouchSamplePhase.Began));
            Feed(S(1, x + 100f, y, TouchSamplePhase.Moved));
            Assert.AreEqual(yaw0 + 100f * PlayerMotion.TouchLookDegPerDp, follow.yaw, 0.01f);
            Feed(S(1, x + 100f, y, TouchSamplePhase.Ended));
        }

        [UnityTest]
        public IEnumerator JumpButtonJumpsWithoutRotating()
        {
            yield return Teleport(Spot);
            Feed();
            Vector2 c = touch.JumpButton.position;
            float yaw0 = follow.yaw;
            float y0 = Feet.y;
            float maxY = y0;
            Feed(S(1, c.x, c.y, TouchSamplePhase.Began));
            Feed(S(1, c.x + 100f, c.y, TouchSamplePhase.Moved));
            Assert.IsTrue(player.TouchJump);
            Assert.AreEqual(yaw0, follow.yaw, 1e-4f);
            for (int i = 0; i < 30; i++)
            {
                Feed(S(1, c.x + 100f, c.y, TouchSamplePhase.Moved));
                yield return null;
                maxY = Mathf.Max(maxY, Feet.y);
            }
            Assert.Greater(maxY - y0, 0.3f);
            Assert.AreEqual(yaw0, follow.yaw, 1e-4f);
            Feed(S(1, c.x + 100f, c.y, TouchSamplePhase.Ended));
            Assert.IsFalse(player.TouchJump);
        }

        [UnityTest]
        public IEnumerator PinchZoomsCamera()
        {
            yield return Teleport(Spot);
            float d0 = follow.distance;
            float x = Screen.width * 0.7f, y = Screen.height * 0.5f;
            Feed(S(1, x - 50f, y, TouchSamplePhase.Began), S(2, x + 50f, y, TouchSamplePhase.Began));
            Feed(S(1, x - 100f, y, TouchSamplePhase.Moved), S(2, x + 100f, y, TouchSamplePhase.Moved));
            Assert.AreEqual(Mathf.Max(d0 / 2f, follow.minDistance), follow.distance, 0.01f);
            Feed();
        }

        [UnityTest]
        public IEnumerator TouchModeUsesTouchDefaultPitch()
        {
            yield return null;
            Assert.AreEqual(PlayerMotion.TouchCameraPitch, follow.defaultPitch, 1e-4f);
            TouchControls.SetForceOn(false);
            Assert.AreEqual(PlayerMotion.CameraPitch, follow.defaultPitch, 1e-4f);
            TouchControls.SetForceOn(true);
        }

        [UnityTest]
        public IEnumerator DisablingTouchModeClearsInput()
        {
            yield return Teleport(Spot);
            float x = Screen.width * 0.25f, y = Screen.height * 0.3f;
            Feed(S(1, x, y, TouchSamplePhase.Began));
            Feed(S(1, x, y + R, TouchSamplePhase.Moved));
            Assert.AreNotEqual(Vector2.zero, player.MoveInput);
            TouchControls.SetForceOn(false);
            Feed(S(1, x, y + R, TouchSamplePhase.Moved));
            Assert.IsFalse(TouchControls.Active);
            Assert.AreEqual(Vector2.zero, player.MoveInput);
            var canvas = touchGo.transform.Find("Touch Controls");
            Assert.IsTrue(canvas == null || !canvas.gameObject.activeSelf);
            TouchControls.SetForceOn(true);
        }

        [UnityTest]
        public IEnumerator JumpButtonLayout()
        {
            yield return null;
            Feed();
            var corners = new Vector3[4];
            touch.JumpButton.GetWorldCorners(corners);
            var safe = Screen.safeArea;
            foreach (var p in corners)
                Assert.IsTrue(safe.Contains(new Vector2(p.x, p.y)) || (p.x == safe.xMax || p.y == safe.yMax), p.ToString());
            Assert.GreaterOrEqual(touch.JumpButton.rect.width, TouchGestures.DpToPx(48f, Dpi));
            Assert.GreaterOrEqual(touch.JumpButton.rect.height, TouchGestures.DpToPx(48f, Dpi));
        }
    }
}
