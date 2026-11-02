using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Cảm ứng (b), brief 0102: tâm nhắm giữa màn hình, nút hành động chính / chế độ / Thu / Túi / Huỷ, chạm trong bảng túi, bố cục (TouchControls.Feed thay cho chạm thật; dpi 160 → 1 dp = 1 px).</summary>
    public class TouchActionPlayTests
    {
        const float Dpi = 160f;
        static readonly ItemId Dirt = ItemId.Block(BlockMaterial.Dirt);
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        SimView sv;
        TerrainEditInput input;
        PlayerCharacter player;
        InventoryView inv;
        TouchControls touch;
        FollowCamera follow;
        Vector3Int spot;

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
            sv.habitat = false;
            input = go.AddComponent<TerrainEditInput>();
            playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            player = playerGo.AddComponent<PlayerCharacter>();
            player.terrain = view;
            player.readKeyboard = false;
            playerGo.SetActive(true);
            go.SetActive(true);
            for (int i = 0; i < 600 && sv.Sim == null; i++) yield return null;
            Assert.IsNotNull(sv.Sim);

            // Đứng cạnh trại, nơi có dải đất phẳng 8 ô theo +x.
            var s0 = sv.Sim.State.Plots[0].Soil;
            bool found = false;
            for (int dz = -12; dz <= 12 && !found; dz++)
                for (int dx = -12; dx <= 12 && !found; dx++)
                {
                    int x = s0.X + dx, z = s0.Z + dz;
                    if (FlatStrip(x, z, 8)) { spot = new Vector3Int(x, view.World.SurfaceHeight(x, z), z); found = true; }
                }
            Assert.IsTrue(found, "no flat strip near camp");
            Stand(spot.x, spot.z);
            for (int i = 0; i < 600 && !player.Ready; i++) yield return null;
            Assert.IsTrue(player.Ready);
            for (int i = 0; i < 20; i++) yield return null;
            sv.enabled = false;

            inv = input.Inventory;
            touch = go.GetComponent<TouchControls>();
            touch.readTouches = false;
            TouchControls.SetForceOn(true);
            follow = camGo.GetComponent<FollowCamera>();
            follow.enabled = false;
            Feed();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            TouchControls.SetForceOn(false);
            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(go);
            UnityEngine.Object.Destroy(camGo);
            yield return null;
        }

        bool Plain(int x, int z, int h)
        {
            if (!view.World.Size.InBounds(x, 0, z) || !view.World.Size.InBounds(x, h + 4, z)) return false;
            if (view.World.SurfaceHeight(x, z) != h) return false;
            var m = view.World.GetMaterial(x, h, z);
            if (m == BlockMaterial.Water || m == BlockMaterial.Empty) return false;
            return sv.Sim.PlotAt(new CellPos(x, h, z)) < 0 && view.World.GetMaterial(x, h + 1, z) == BlockMaterial.Empty && view.World.GetMaterial(x, h + 2, z) == BlockMaterial.Empty;
        }

        bool FlatStrip(int x, int z, int len)
        {
            if (!view.World.Size.InBounds(x, 0, z)) return false;
            int h = view.World.SurfaceHeight(x, z);
            for (int i = 0; i <= len; i++)
                for (int dz = -1; dz <= 1; dz++)
                    if (!Plain(x + i, z + dz, h)) return false;
            return true;
        }

        void Stand(int x, int z) =>
            player.Place(view.transform.TransformPoint(new Vector3(x + 0.5f, view.World.SurfaceHeight(x, z) + 1f, z + 0.5f)), 0f);

        /// <summary>Ô mặt đất cách nhân vật dx ô theo +x.</summary>
        Vector3Int Ahead(int dx) => new Vector3Int(spot.x + dx, spot.y, spot.z);

        IEnumerator WaitWalk(float s = 5f)
        {
            float t = 0f;
            while (t < s && input.Walk.State == WalkToActState.Walking)
            {
                yield return null;
                t += Time.deltaTime;
            }
        }

        IEnumerator Wait(float s)
        {
            float t = 0f;
            while (t < s) { yield return null; t += Time.deltaTime; }
        }

        IEnumerator Frames(int n = 2)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        void Feed(params TouchSample[] s) => touch.Feed(s, Screen.width, Dpi);

        Vector3Int Surface(int x, int z) => new Vector3Int(x, view.World.SurfaceHeight(x, z), z);

        void AimAt(Vector3Int cell, Vector3? offset = null)
        {
            var top = view.transform.TransformPoint(cell.x + 0.5f, cell.y + 1f, cell.z + 0.5f);
            camGo.transform.position = top + (offset ?? new Vector3(0f, 5f, -1.5f));
            camGo.transform.LookAt(top);
        }

        IEnumerator Tap(int id)
        {
            var c = touch.ButtonRect(id).center;
            Feed(new TouchSample(9, c.x, c.y, TouchSamplePhase.Began));
            yield return null;
            Feed(new TouchSample(9, c.x, c.y, TouchSamplePhase.Ended));
            yield return null;
        }

        IEnumerator TapUi(Vector2 p)
        {
            Feed(new TouchSample(8, p.x, p.y, TouchSamplePhase.Began, true));
            yield return null;
            Feed(new TouchSample(8, p.x, p.y, TouchSamplePhase.Ended, true));
            yield return null;
        }

        BlockMaterial Mat(Vector3Int c) => view.World.GetMaterial(c.x, c.y, c.z);

        [UnityTest]
        public IEnumerator AimHighlightsCenterCellAndActionDigsNow()
        {
            yield return Tap(5);
            AimAt(Ahead(2));
            yield return null;
            Assert.IsTrue(input.HighlightVisible);
            Assert.AreEqual(Ahead(2), input.HighlightCell);
            // Ahead(2) is ~2.5 cells from the body centre (> DigReach 2), so dig-now is checked on Ahead(1).
            AimAt(Ahead(1));
            yield return null;
            var c = touch.ButtonRect(2).center;
            Feed(new TouchSample(9, c.x, c.y, TouchSamplePhase.Began));
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.AreEqual(BlockMaterial.Empty, Mat(Ahead(1)));
            Feed(new TouchSample(9, c.x, c.y, TouchSamplePhase.Ended));
        }

        [UnityTest]
        public IEnumerator ActionWalksThenDigs()
        {
            yield return Tap(5);
            AimAt(Ahead(6));
            yield return null;
            yield return Tap(2);
            Assert.AreEqual(WalkToActState.Walking, input.Walk.State);
            Assert.AreNotEqual(BlockMaterial.Empty, Mat(Ahead(6)));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.AreEqual(BlockMaterial.Empty, Mat(Ahead(6)));
        }

        [UnityTest]
        public IEnumerator ActionOutOfReachFlashesRing()
        {
            yield return Tap(5);
            var c = Surface(spot.x + 12, spot.z);
            var before = Mat(c);
            AimAt(c);
            yield return null;
            Assert.AreEqual(RightClickAction.OutOfReach, input.TouchAction());
            Assert.IsTrue(input.ReachRingVisible);
            Assert.AreEqual(before, Mat(c));
            Assert.AreEqual(ClickMoveState.None, player.ClickMove);
        }

        [UnityTest]
        public IEnumerator AimRayPassesThroughPlayer()
        {
            yield return Tap(5);
            var top = view.transform.TransformPoint(Ahead(3).x + 0.5f, Ahead(3).y + 1f, Ahead(3).z + 0.5f);
            camGo.transform.position = player.transform.position + new Vector3(-2f, 1.2f, 0f);
            camGo.transform.LookAt(top);
            yield return null;
            Assert.AreEqual(Ahead(3), input.HighlightCell);
            // The player is already on the Ignore Raycast layer, so even the PC ray (Physics.Raycast) never hits the body.
            Assert.AreEqual(LayerMask.NameToLayer("Ignore Raycast"), player.gameObject.layer);
            Assert.IsTrue(input.Pick(Camera.main.ScreenPointToRay(TerrainEditInput.AimScreenPoint), out var d, out _));
            Assert.AreEqual(Ahead(3), d);
        }

        [UnityTest]
        public IEnumerator ActionOpensUnripePlotPanelInDefaultMode()
        {
            var cell = Ahead(2);
            Assert.AreEqual(TillResult.Marked, sv.UseTill(cell));
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            AimAt(cell);
            yield return null;
            yield return Tap(2);
            float t = 0f;
            while (t < 5f && sv.OpenPlotId < 0) { yield return null; t += Time.deltaTime; }
            Assert.AreEqual(id, sv.OpenPlotId);
        }

        [UnityTest]
        public IEnumerator ModeButtonsToggle()
        {
            yield return Tap(5);
            Assert.AreEqual(ControlMode.Dig, input.Control.Mode);
            yield return Tap(5);
            Assert.AreEqual(ControlMode.Default, input.Control.Mode);
            yield return Tap(7);
            Assert.AreEqual(ControlMode.Rain, input.Control.Mode);
            yield return Tap(4);
            Assert.AreEqual(ControlMode.Default, input.Control.Mode);
        }

        [UnityTest]
        public IEnumerator CollectButtonTakesNearOnly()
        {
            var near = view.Editor.Drops.Add(spot.x + 1, spot.y + 1, spot.z, ItemId.Block(BlockMaterial.Dirt), 1);
            var far = view.Editor.Drops.Add(spot.x + 3, spot.y + 1, spot.z, ItemId.Block(BlockMaterial.Stone), 1);
            yield return Tap(3);
            Assert.IsNull(view.Editor.Drops.Find(near.Id));
            Assert.IsNotNull(view.Editor.Drops.Find(far.Id));
        }

        [UnityTest]
        public IEnumerator BagButtonToggles()
        {
            yield return Tap(8);
            Assert.IsTrue(inv.BagOpen);
            yield return Tap(8);
            Assert.IsFalse(inv.BagOpen);
        }

        [UnityTest]
        public IEnumerator BackClosesBag()
        {
            inv.ToggleBag();
            yield return Tap(9);
            Assert.IsFalse(inv.BagOpen);
            Assert.IsFalse(SettingsView.IsAnyOpen);
        }

        [UnityTest]
        public IEnumerator BackDropsHeldItem()
        {
            view.Bag.Add(BlockMaterial.Dirt, 2);
            input.HoldItem(Dirt);
            Assert.IsTrue(input.Control.HasHeld);
            yield return Tap(9);
            Assert.IsFalse(input.Control.HasHeld);
            Assert.IsFalse(SettingsView.IsAnyOpen);
        }

        [UnityTest]
        public IEnumerator BackWithNothingOpensThenClosesSettings()
        {
            yield return Tap(9);
            Assert.IsTrue(SettingsView.IsAnyOpen);
            yield return Tap(9);
            Assert.IsFalse(SettingsView.IsAnyOpen);
        }

        [UnityTest]
        public IEnumerator BackFlagLastsOneFrame()
        {
            var c = touch.ButtonRect(9).center;
            Feed(new TouchSample(9, c.x, c.y, TouchSamplePhase.Began));
            yield return null;
            Assert.IsTrue(SettingsView.IsAnyOpen);
            yield return Frames(3);
            Assert.IsTrue(SettingsView.IsAnyOpen);
        }

        [UnityTest]
        public IEnumerator TapHotbarSlotHolds()
        {
            view.Bag.Hotbar.AddToSlot(0, Dirt, 2);
            yield return Frames();
            var p = inv.SlotScreenCenter(ItemContainer.Hotbar, 0);
            Assert.IsTrue(InventoryView.BlocksPointer(p), "Covers false at slot center");
            Feed(new TouchSample(8, p.x, p.y, TouchSamplePhase.Began, true));
            Assert.IsFalse(touch.Gestures.JoystickActive);
            yield return null;
            Feed(new TouchSample(8, p.x, p.y, TouchSamplePhase.Ended, true));
            Assert.IsFalse(touch.Gestures.JoystickActive);
            yield return null;
            Assert.IsTrue(input.Control.HasHeld);
            Assert.AreEqual(Dirt, input.Control.Held);
        }

        [UnityTest]
        public IEnumerator DragBagToHotbarByTouch()
        {
            view.Bag.Add(BlockMaterial.Dirt, 5);
            inv.ToggleBag();
            yield return Frames();
            var from = inv.SlotScreenCenter(ItemContainer.Bag, 1);
            var to = inv.SlotScreenCenter(ItemContainer.Hotbar, 1);
            Assert.IsTrue(InventoryView.BlocksPointer(from) && InventoryView.BlocksPointer(to), "Covers false at slot center");
            var mid = Vector2.Lerp(from, to, 0.5f);
            Feed(new TouchSample(8, from.x, from.y, TouchSamplePhase.Began, true));
            yield return null;
            Feed(new TouchSample(8, mid.x, mid.y, TouchSamplePhase.Moved, true));
            Feed(new TouchSample(8, to.x, to.y, TouchSamplePhase.Moved, true));
            yield return null;
            Feed(new TouchSample(8, to.x, to.y, TouchSamplePhase.Ended, true));
            yield return null;
            Assert.AreEqual(Dirt, view.Bag.Hotbar.Get(1).Item);
            Assert.AreEqual(5, view.Bag.Hotbar.Get(1).Count);
            Assert.IsTrue(view.Bag.Main.Get(1).IsEmpty);
        }

        [UnityTest]
        public IEnumerator TapBagSlotSelects()
        {
            view.Bag.Add(BlockMaterial.Dirt, 5);
            inv.ToggleBag();
            yield return Frames();
            yield return TapUi(inv.SlotScreenCenter(ItemContainer.Bag, 1));
            Assert.IsTrue(inv.HasSelection);
            Assert.IsFalse(input.Control.HasHeld);
        }

        [UnityTest]
        public IEnumerator LayoutFitsSafeAreaWithoutOverlap()
        {
            Feed();
            yield return null;
            var s = Screen.safeArea;
            float min = TouchGestures.DpToPx(48f, Dpi);
            var ids = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            string dump = Screen.width + "x" + Screen.height;
            foreach (int id in ids) dump += " " + id + "=" + touch.ButtonRect(id);
            foreach (int id in ids)
            {
                var r = touch.ButtonRect(id);
                Assert.IsTrue(s.Contains(r.min) && s.Contains(r.max), "outside safeArea " + dump);
                Assert.GreaterOrEqual(r.width, min - 0.01f, dump);
                Assert.GreaterOrEqual(r.height, min - 0.01f, dump);
                Assert.LessOrEqual(r.width, touch.ButtonRect(2).width + 0.01f, dump);
            }
            for (int i = 0; i < ids.Length; i++)
                for (int j = i + 1; j < ids.Length; j++)
                    Assert.IsFalse(touch.ButtonRect(ids[i]).Overlaps(touch.ButtonRect(ids[j])), "overlap " + ids[i] + "/" + ids[j] + " " + dump);
        }

        [UnityTest]
        public IEnumerator CrosshairHidesWhenPanelOpen()
        {
            Assert.IsTrue(touch.Crosshair.gameObject.activeInHierarchy);
            inv.ToggleBag();
            Feed();
            Assert.IsFalse(touch.Crosshair.gameObject.activeInHierarchy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TouchOffIgnoresButtons()
        {
            TouchControls.SetForceOn(false);
            int f0 = GameInput.TouchBackFrame;
            var c9 = touch.ButtonRect(9).center;
            var c5 = touch.ButtonRect(5).center;
            Feed(new TouchSample(9, c9.x, c9.y, TouchSamplePhase.Began));
            Feed(new TouchSample(7, c5.x, c5.y, TouchSamplePhase.Began));
            yield return Frames();
            Assert.IsFalse(TouchControls.Active);
            Assert.AreEqual(f0, GameInput.TouchBackFrame);
            Assert.IsFalse(SettingsView.IsAnyOpen);
            Assert.AreEqual(ControlMode.Default, input.Control.Mode);
            Assert.IsFalse(touch.Crosshair.gameObject.activeInHierarchy);
        }
    }
}
