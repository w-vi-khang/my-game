using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7j (brief 0053): observation camera — place, tool cycle, load then view, return, locks. Brief 0057: place on a cell, look while viewing.</summary>
    public class ObservationCameraPlayTests
    {
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        TerrainEditInput tei;
        ObservationCamera obs;
        PlayerCharacter player;
        FollowCamera follow;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<DebugFlyCamera>();
            follow = camGo.AddComponent<FollowCamera>();
            camGo.transform.SetPositionAndRotation(new Vector3(192, 40, 150), Quaternion.Euler(25f, 0f, 0f));
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            go.AddComponent<DemoPlain>();
            tei = go.AddComponent<TerrainEditInput>();
            playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            player = playerGo.AddComponent<PlayerCharacter>();
            player.terrain = view;
            player.readKeyboard = false;
            playerGo.SetActive(true);
            yield return null;
            yield return null;
            for (int i = 0; i < 60 && !player.Ready; i++)
                yield return null;
            Assert.IsTrue(player.Ready);
            obs = go.GetComponent<ObservationCamera>();
            Assert.IsNotNull(obs);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(playerGo);
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        static Vector2 ButtonCenterOnScreen()
        {
            float s = UiScale.Gui, gw = Screen.width / s, gh = Screen.height / s;
            var r = ObservationCamera.ButtonRect(gw, gh);
            return new Vector2(r.center.x * s, Screen.height - r.center.y * s);
        }

        static void AssertPose(Transform t, Vector3 p, Quaternion r)
        {
            Assert.Less(Vector3.Distance(t.position, p), 1e-3f, "position");
            Assert.Less(Quaternion.Angle(t.rotation, r), 0.01f, "rotation");
        }

        Vector3 Map2Pose() // above the Map 2 centroid, world space
        {
            var c = WalkBench.MapCentroid(view.Maps, 1);
            return view.transform.TransformPoint(new Vector3(c.x, view.World.Size.Y, c.y));
        }

        Vector3Int GroundAhead(int dx) { var f = player.FeetLocal; return new Vector3Int(Mathf.FloorToInt(f.x) + dx, Mathf.FloorToInt(f.y) - 1, Mathf.FloorToInt(f.z)); }
        Vector3 AboveCell(Vector3Int c) => view.transform.TransformPoint(new Vector3(c.x + 0.5f, c.y + 1f + PlayerMotion.ObservationHeight, c.z + 0.5f));

        IEnumerator WaitViewing()
        {
            for (int i = 0; i < 600 && obs.State != ObservationState.Viewing; i++) yield return null;
            Assert.AreEqual(ObservationState.Viewing, obs.State, "not loaded within 600 frames");
        }

        [UnityTest]
        public IEnumerator PlaceOnCellUsesCameraAnglesAndReplaces()
        {
            follow.enabled = false;
            Assert.IsFalse(obs.HasCamera);
            Assert.IsFalse(obs.ButtonVisible);
            Assert.IsFalse(ObservationCamera.BlocksPointer(ButtonCenterOnScreen()));
            var c1 = GroundAhead(2);
            var r1 = Quaternion.Euler(30f, 45f, 0f);
            camGo.transform.SetPositionAndRotation(new Vector3(150.25f, 45.5f, 140.75f), r1);
            Assert.IsTrue(tei.PlaceObservationCamera(c1));
            AssertPose(obs.Point, AboveCell(c1), r1);
            StringAssert.Contains("Đã đặt camera quan sát", tei.HudText()); // vi-text
            Assert.IsTrue(obs.ButtonVisible);
            Assert.IsTrue(obs.MarkerVisible);
            Assert.AreEqual("Xem camera (V)", obs.ButtonText);
            Assert.IsTrue(ObservationCamera.BlocksPointer(ButtonCenterOnScreen()));
            Assert.IsFalse(ObservationCamera.BlocksPointer(new Vector2(Screen.width / 2f, Screen.height / 2f)));
            var first = obs.Point;
            var c2 = GroundAhead(-3);
            var r2 = Quaternion.Euler(10f, 200f, 0f);
            camGo.transform.SetPositionAndRotation(new Vector3(200.5f, 50f, 210.5f), r2);
            Assert.IsTrue(tei.PlaceObservationCamera(c2));
            Assert.AreSame(first, obs.Point);
            AssertPose(obs.Point, AboveCell(c2), r2);
            var far = GroundAhead(30);
            Assert.IsFalse(tei.PlaceObservationCamera(far));
            AssertPose(obs.Point, AboveCell(c2), r2);
            Assert.AreEqual(1, go.GetComponentsInChildren<Transform>(true).Count(t => t.name == "ObservationPoint"));
            yield return null;
            Assert.AreEqual(0, obs.Point.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(1, view.ViewpointCount);
        }

        [UnityTest]
        public IEnumerator HoldCameraAndRightClickPlaces()
        {
            follow.enabled = false;
            Assert.IsTrue(tei.HoldItem(ItemId.Tool(ToolItem.ObservationCamera)));
            Assert.AreEqual(ItemKind.Tool, tei.Control.Held.Kind);
            StringAssert.Contains("<color=yellow>Camera quan sát</color>", tei.ModeLine()); // vi-text
            StringAssert.Contains("chuột phải lên khối = đặt", SettingsView.GuideText()); // vi-text
            var r1 = Quaternion.Euler(30f, 45f, 0f);
            camGo.transform.SetPositionAndRotation(new Vector3(150.25f, 45.5f, 140.75f), r1);
            var c = GroundAhead(2);
            Assert.AreEqual(RightClickAction.UseHeld, tei.RightClickAt(true, c, c));
            float t = 0f;
            while (t < 5f && tei.Walk.State == WalkToActState.Walking) { yield return null; t += Time.deltaTime; }
            Assert.AreEqual(WalkToActState.Done, tei.Walk.State);
            AssertPose(obs.Point, AboveCell(c), r1);
            var far = GroundAhead(30);
            Assert.AreEqual(RightClickAction.OutOfReach, tei.RightClickAt(true, far, far));
            AssertPose(obs.Point, AboveCell(c), r1);
            Assert.AreEqual(ItemKind.Tool, tei.Control.Held.Kind, "still held after placing");
            tei.Control.ClearToDefault();
            Assert.IsFalse(tei.Control.HasHeld);
        }

        [UnityTest]
        public IEnumerator ViewRightAfterPlacingMovesCamera()
        {
            yield return null;
            var before = camGo.transform.position;
            Assert.IsTrue(tei.PlaceObservationCamera(GroundAhead(2)));
            obs.Toggle();
            yield return WaitViewing();
            Assert.Greater(Vector3.Distance(camGo.transform.position, before), 1f);
            AssertPose(camGo.transform, obs.Point.position, obs.Point.rotation);
            obs.Toggle();
        }

        [UnityTest]
        public IEnumerator LookRotatesWhileViewingAndIsKept()
        {
            follow.enabled = false;
            camGo.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            Assert.IsTrue(tei.PlaceObservationCamera(GroundAhead(2)));
            Assert.IsFalse(obs.ApplyLook(new Vector2(100f, 0f), 0.3f, false));
            Assert.Less(Quaternion.Angle(obs.Point.rotation, Quaternion.Euler(20f, 0f, 0f)), 0.01f);
            obs.Toggle();
            yield return WaitViewing();
            Assert.IsTrue(obs.ApplyLook(new Vector2(100f, 0f), 0.3f, false));
            Assert.Less(Quaternion.Angle(obs.Point.rotation, Quaternion.Euler(20f, 30f, 0f)), 0.01f);
            Assert.Less(Quaternion.Angle(camGo.transform.rotation, obs.Point.rotation), 0.01f);
            obs.ApplyLook(new Vector2(0f, 1000f), 0.3f, false);
            Assert.Less(Quaternion.Angle(obs.Point.rotation, Quaternion.Euler(-80f, 30f, 0f)), 0.01f);
            obs.ApplyLook(new Vector2(0f, 10f), 0.3f, true);
            Assert.Less(Quaternion.Angle(obs.Point.rotation, Quaternion.Euler(-77f, 30f, 0f)), 0.01f);
            obs.Toggle();
            Assert.Less(Quaternion.Angle(obs.Point.rotation, Quaternion.Euler(-77f, 30f, 0f)), 0.01f);
            obs.Toggle();
            yield return WaitViewing();
            Assert.Less(Quaternion.Angle(camGo.transform.rotation, Quaternion.Euler(-77f, 30f, 0f)), 0.01f);
            obs.Toggle();
            camGo.transform.rotation = Quaternion.Euler(25f, 90f, 0f);
            Assert.IsTrue(tei.PlaceObservationCamera(GroundAhead(-3)));
            Assert.Less(Quaternion.Angle(obs.Point.rotation, Quaternion.Euler(25f, 90f, 0f)), 0.01f);
        }

        [UnityTest]
        public IEnumerator ViewWaitsForLoadThenShowsAndReturns()
        {
            Assert.IsTrue(obs.Place(Map2Pose(), Quaternion.Euler(40f, 0f, 0f)));
            Assert.Greater(view.MissingColumnsWithin(obs.Point, 128), 0, "Map 2 must need loading");
            Assert.AreEqual(1, view.ViewpointCount);
            Assert.IsTrue(obs.Toggle());
            Assert.AreEqual(ObservationState.Loading, obs.State);
            Assert.IsTrue(obs.LoadingVisible);
            Assert.AreEqual(2, view.ViewpointCount);
            Assert.IsFalse(follow.enabled);
            Assert.Greater(Vector3.Distance(camGo.transform.position, obs.Point.position), 1f);
            Assert.IsTrue(ObservationCamera.InputLocked);
            Assert.AreEqual("Quay về (V)", obs.ButtonText); // vi-text
            yield return WaitViewing();
            Assert.AreEqual(0, view.MissingColumnsWithin(obs.Point, 128));
            AssertPose(camGo.transform, obs.Point.position, obs.Point.rotation);
            Assert.IsFalse(obs.MarkerVisible);
            Assert.IsFalse(obs.LoadingVisible);
            Assert.AreEqual(2, view.ViewpointCount);
            var pc = view.CellAt(player.transform.position);
            var held = Enumerable.Range(0, 576)
                .Where(col => view.ChunkMesh(col) != null && ChunkStreamPlan.DistanceSq(pc.x, pc.z, col % 24, col / 24) > 160L * 160)
                .ToList();
            Assert.IsNotEmpty(held);
            Assert.IsTrue(obs.Toggle());
            Assert.AreEqual(ObservationState.Off, obs.State);
            Assert.AreEqual(1, view.ViewpointCount);
            Assert.IsTrue(follow.enabled);
            Assert.IsTrue(obs.MarkerVisible);
            Assert.IsFalse(ObservationCamera.InputLocked);
            Assert.AreEqual("Xem camera (V)", obs.ButtonText);
            yield return null;
            Assert.Less(Vector3.Distance(camGo.transform.position, player.transform.position), follow.maxDistance + 3f);
            bool released = false;
            for (int i = 0; i < 300 && !released; i++)
            {
                released = held.All(col => view.ChunkMesh(col) == null);
                if (!released) yield return null;
            }
            if (!released) Assert.Fail("columns of the observation area were not unloaded within 300 frames");
            for (int col = 0; col < 576; col++)
                if (view.ChunkMesh(col) != null)
                    Assert.LessOrEqual(ChunkStreamPlan.DistanceSq(pc.x, pc.z, col % 24, col / 24), 160L * 160, "column " + col);
            Assert.AreEqual(0, view.MissingColumnsWithin(128));
        }

        [UnityTest]
        public IEnumerator KeysWithoutCameraDoNothing()
        {
            obs.HandleKeys(true, false);
            obs.HandleKeys(false, true);
            Assert.AreEqual(ObservationState.Off, obs.State);
            Assert.AreEqual(1, view.ViewpointCount);
            Assert.IsTrue(follow.enabled);
            Assert.IsFalse(obs.ButtonVisible);
            Assert.IsFalse(obs.Toggle());
            yield break;
        }

        [UnityTest]
        public IEnumerator ExitDuringLoadingCancels()
        {
            Assert.IsTrue(obs.Place(Map2Pose(), Quaternion.Euler(40f, 0f, 0f)));
            obs.HandleKeys(true, false);
            Assert.AreEqual(ObservationState.Loading, obs.State);
            Assert.IsFalse(obs.ApplyLook(new Vector2(100f, 0f), 0.3f, false));
            Assert.Less(Quaternion.Angle(obs.Point.rotation, Quaternion.Euler(40f, 0f, 0f)), 0.01f);
            obs.HandleKeys(true, false);
            Assert.AreEqual(ObservationState.Off, obs.State);
            Assert.AreEqual(1, view.ViewpointCount);
            Assert.IsTrue(follow.enabled);
            Assert.IsTrue(obs.MarkerVisible);
            yield break;
        }

        [UnityTest]
        public IEnumerator BackExitsWithoutOpeningSettings()
        {
            var settings = go.AddComponent<SettingsView>();
            yield return null;
            Assert.IsTrue(tei.PlaceObservationCamera(GroundAhead(2)));
            obs.Toggle();
            yield return WaitViewing();
            settings.HandleKeys(false, true);
            Assert.IsFalse(settings.IsOpen);
            obs.HandleKeys(false, true);
            Assert.AreEqual(ObservationState.Off, obs.State);
            settings.HandleKeys(false, true);
            Assert.IsFalse(settings.IsOpen);
            yield return null;
            settings.HandleKeys(false, true);
            Assert.IsTrue(settings.IsOpen);
            settings.SetOpen(false);
        }

        [UnityTest]
        public IEnumerator ToolsAndMovementLockedWhileViewing()
        {
            Assert.IsTrue(tei.PlaceObservationCamera(GroundAhead(2)));
            obs.Toggle();
            yield return WaitViewing();
            tei.ToggleMode(ControlMode.Dig);
            Assert.IsFalse(tei.HoldItem(ItemId.Tool(ToolItem.ObservationCamera)));
            Assert.AreEqual(ControlMode.Default, tei.Control.Mode);
            Assert.IsFalse(tei.Control.HasHeld);
            var f = player.FeetLocal;
            var c = new Vector3Int(Mathf.FloorToInt(f.x) + 2, Mathf.FloorToInt(f.y) - 1, Mathf.FloorToInt(f.z));
            var before = view.World.Get(c.x, c.y, c.z);
            Assert.AreEqual(RightClickAction.None, tei.RightClickAt(true, c, c));
            Assert.AreEqual(before, view.World.Get(c.x, c.y, c.z));
            Assert.IsFalse(tei.PlaceObservationCamera(GroundAhead(2)));
            var p0 = player.transform.position;
            player.MoveInput = new Vector2(0f, 1f);
            player.JumpInput = true;
            for (int i = 0; i < 30; i++) yield return null;
            var p = player.transform.position;
            Assert.Less(new Vector2(p.x - p0.x, p.z - p0.z).magnitude, 0.05f);
            Assert.AreEqual(Vector2.zero, player.MoveInput);
            obs.Toggle();
            tei.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(ControlMode.Dig, tei.Control.Mode);
        }

        [UnityTest]
        public IEnumerator NoFreeViewpointRefuses()
        {
            var dummy = new GameObject("Dummy").transform;
            try
            {
                view.AddViewpoint(dummy, 128, 160);
                tei.PlaceObservationCamera(GroundAhead(2));
                LogAssert.Expect(LogType.Warning, new Regex("^OBSERVE"));
                Assert.IsFalse(obs.Toggle());
                Assert.AreEqual(ObservationState.Off, obs.State);
                Assert.IsTrue(follow.enabled);
                Assert.AreEqual(2, view.ViewpointCount);
            }
            finally
            {
                view.RemoveViewpoint(dummy);
                Object.Destroy(dummy.gameObject);
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator MarkerHiddenWhileCameraClose()
        {
            follow.enabled = false;
            camGo.transform.SetPositionAndRotation(new Vector3(150.25f, 45.5f, 140.75f), Quaternion.Euler(30f, 45f, 0f));
            Assert.IsTrue(tei.PlaceObservationCamera(GroundAhead(2)));
            yield return null;
            Assert.IsTrue(obs.MarkerVisible);
            Assert.IsTrue(obs.MarkerRendered, "marker is drawn while the camera is far");
            camGo.transform.position = obs.Point.position + new Vector3(1f, 0f, 0f);
            yield return null;
            Assert.IsFalse(obs.MarkerRendered);
            camGo.transform.position = obs.Point.position + new Vector3(0f, 0f, -3f);
            yield return null;
            Assert.IsTrue(obs.MarkerRendered);
        }
    }
}
