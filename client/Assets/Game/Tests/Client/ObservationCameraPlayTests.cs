using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7j (brief 0053): observation camera — place, tool cycle, load then view, return, locks.</summary>
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

        IEnumerator WaitViewing()
        {
            for (int i = 0; i < 600 && obs.State != ObservationState.Viewing; i++) yield return null;
            Assert.AreEqual(ObservationState.Viewing, obs.State, "not loaded within 600 frames");
        }

        [UnityTest]
        public IEnumerator PlaceTakesCameraPoseAndReplaces()
        {
            Assert.IsFalse(obs.HasCamera);
            Assert.IsFalse(obs.ButtonVisible);
            Assert.IsFalse(ObservationCamera.BlocksPointer(ButtonCenterOnScreen()));
            var p1 = new Vector3(150.25f, 45.5f, 140.75f);
            var r1 = Quaternion.Euler(30f, 45f, 0f);
            camGo.transform.SetPositionAndRotation(p1, r1);
            Assert.IsTrue(tei.PlaceObservationCamera());
            AssertPose(obs.Point, p1, r1);
            StringAssert.Contains("Đã đặt camera quan sát", tei.HudText()); // vi-text
            Assert.IsTrue(obs.ButtonVisible);
            Assert.IsTrue(obs.MarkerVisible);
            Assert.AreEqual("Xem camera (V)", obs.ButtonText);
            Assert.IsTrue(ObservationCamera.BlocksPointer(ButtonCenterOnScreen()));
            Assert.IsFalse(ObservationCamera.BlocksPointer(new Vector2(Screen.width / 2f, Screen.height / 2f)));
            var first = obs.Point;
            var p2 = new Vector3(200.5f, 50f, 210.5f);
            var r2 = Quaternion.Euler(10f, 200f, 0f);
            camGo.transform.SetPositionAndRotation(p2, r2);
            Assert.IsTrue(tei.PlaceObservationCamera());
            Assert.AreSame(first, obs.Point);
            AssertPose(obs.Point, p2, r2);
            Assert.AreEqual(1, go.GetComponentsInChildren<Transform>(true).Count(t => t.name == "ObservationPoint"));
            yield return null;
            Assert.AreEqual(0, obs.Point.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(1, view.ViewpointCount);
        }

        [UnityTest]
        public IEnumerator CameraToolCycleAndClickPlaces()
        {
            tei.ToggleTool();
            Assert.AreEqual(EditTool.Place, tei.Tool);
            tei.ToggleTool();
            Assert.AreEqual(EditTool.Camera, tei.Tool);
            StringAssert.Contains("<color=yellow>Camera</color>", tei.HudText());
            StringAssert.Contains("đặt camera quan sát tại chỗ và hướng đang nhìn", tei.HudText()); // vi-text
            var p1 = new Vector3(150.25f, 45.5f, 140.75f);
            var r1 = Quaternion.Euler(30f, 45f, 0f);
            camGo.transform.SetPositionAndRotation(p1, r1);
            Assert.AreEqual(EditResult.Ok, tei.UseTool(default, default));
            AssertPose(obs.Point, p1, r1);
            tei.ToggleTool();
            Assert.AreEqual(EditTool.Dig, tei.Tool);
            yield break;
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
            Assert.IsTrue(tei.PlaceObservationCamera());
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
            Assert.IsTrue(tei.PlaceObservationCamera());
            obs.Toggle();
            yield return WaitViewing();
            var tool = tei.Tool;
            tei.ToggleTool();
            Assert.AreEqual(tool, tei.Tool);
            var f = player.FeetLocal;
            var c = new Vector3Int(Mathf.FloorToInt(f.x) + 2, Mathf.FloorToInt(f.y) - 1, Mathf.FloorToInt(f.z));
            var before = view.World.Get(c.x, c.y, c.z);
            Assert.AreEqual(EditResult.NotPlaceable, tei.UseTool(c, c));
            Assert.AreEqual(before, view.World.Get(c.x, c.y, c.z));
            Assert.IsFalse(tei.PlaceObservationCamera());
            var p0 = player.transform.position;
            player.MoveInput = new Vector2(0f, 1f);
            player.JumpInput = true;
            for (int i = 0; i < 30; i++) yield return null;
            var p = player.transform.position;
            Assert.Less(new Vector2(p.x - p0.x, p.z - p0.z).magnitude, 0.05f);
            Assert.AreEqual(Vector2.zero, player.MoveInput);
            obs.Toggle();
            tei.ToggleTool();
            Assert.AreNotEqual(tool, tei.Tool);
        }

        [UnityTest]
        public IEnumerator NoFreeViewpointRefuses()
        {
            var dummy = new GameObject("Dummy").transform;
            try
            {
                view.AddViewpoint(dummy, 128, 160);
                tei.PlaceObservationCamera();
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
            Assert.IsTrue(tei.PlaceObservationCamera());
            yield return null;
            Assert.IsTrue(obs.MarkerVisible);
            Assert.IsFalse(obs.MarkerRendered, "marker must not cover the view right after placing");
            camGo.transform.position = obs.Point.position + new Vector3(0f, 0f, -3f);
            yield return null;
            Assert.IsTrue(obs.MarkerRendered);
            camGo.transform.position = obs.Point.position + new Vector3(1f, 0f, 0f);
            yield return null;
            Assert.IsFalse(obs.MarkerRendered);
        }
    }
}
