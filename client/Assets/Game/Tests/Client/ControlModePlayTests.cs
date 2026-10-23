using System;
using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7l c1 (brief 0058) — chế độ D/Q/E/R, Tab cầm tạm, chuột phải tự đi tới 2 ô, lớp đỏ + vòng tầm, F Thu 2 ô, click trái ô ruộng + Bỏ đánh dấu.</summary>
    public class ControlModePlayTests
    {
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        SimView sv;
        TerrainEditInput input;
        PlayerCharacter player;
        PlayerFarmActions farming;
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
            farming = input.Farming;

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
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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

        [UnityTest]
        public IEnumerator ModeKeysToggleAndTabCycle()
        {
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(ControlMode.Dig, input.Control.Mode);
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(ControlMode.Default, input.Control.Mode);
            input.ToggleMode(ControlMode.Till);
            Assert.AreEqual(ControlMode.Till, input.Control.Mode);
            input.ToggleMode(ControlMode.Rain);
            Assert.AreEqual(ControlMode.Rain, input.Control.Mode);
            input.ToggleMode(ControlMode.Default);
            input.CycleTempHold();
            Assert.AreEqual(ItemKind.Block, input.Control.Held.Kind);
            Assert.AreEqual(input.SelectedMaterial, input.Control.Held.Material);
            input.CycleTempHold();
            Assert.AreEqual(ItemKind.Tool, input.Control.Held.Kind);
            input.CycleTempHold();
            Assert.IsFalse(input.Control.HasHeld);
            input.CycleTempHold();
            input.SelectMaterial(2);
            Assert.AreEqual(BlockMaterial.Stone, input.Control.Held.Material);
            input.ToggleMode(ControlMode.Dig);
            Assert.IsFalse(input.Control.HasHeld);
            Assert.AreEqual(ControlMode.Dig, input.Control.Mode);
            yield break;
        }

        [UnityTest]
        public IEnumerator DigWalksToTwoCellsThenDigs()
        {
            var cell = Ahead(6);
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(RightClickAction.Dig, input.RightClickAt(true, cell, cell + Vector3Int.up));
            Assert.AreEqual(WalkToActState.Walking, input.Walk.State);
            Assert.AreNotEqual(BlockMaterial.Empty, view.World.GetMaterial(cell.x, cell.y, cell.z));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.AreEqual(BlockMaterial.Empty, view.World.GetMaterial(cell.x, cell.y, cell.z));
            Assert.IsTrue(player.InDigReach(cell));
        }

        [UnityTest]
        public IEnumerator OutOfTargetReachDoesNothingAndShowsRing()
        {
            var cell = Ahead(12);
            input.ToggleMode(ControlMode.Dig);
            var before = view.World.GetMaterial(cell.x, cell.y, cell.z);
            Assert.AreEqual(RightClickAction.OutOfReach, input.RightClickAt(true, cell, cell + Vector3Int.up));
            Assert.AreEqual(ClickMoveState.None, player.ClickMove);
            Assert.AreEqual(before, view.World.GetMaterial(cell.x, cell.y, cell.z));
            input.ShowAim(input.Preview(true, cell, cell + Vector3Int.up), true, cell, cell + Vector3Int.up);
            Assert.IsTrue(input.ReachRingVisible);
            Assert.IsFalse(input.HighlightVisible);
            input.ToggleMode(ControlMode.Default);
            input.ShowAim(input.Preview(true, cell, cell + Vector3Int.up), true, cell, cell + Vector3Int.up);
            Assert.IsFalse(input.ReachRingVisible);
            yield break;
        }

        [UnityTest]
        public IEnumerator HighlightInRangeDefaultNone()
        {
            var cell = Ahead(5);
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(RightClickAction.Dig, input.Preview(true, cell, cell + Vector3Int.up));
            input.ShowAim(RightClickAction.Dig, true, cell, cell + Vector3Int.up);
            Assert.IsTrue(input.HighlightVisible);
            Assert.AreEqual(cell, input.HighlightCell);
            input.ToggleMode(ControlMode.Default);
            Assert.AreEqual(RightClickAction.None, input.Preview(true, cell, cell + Vector3Int.up));
            input.ShowAim(RightClickAction.None, true, cell, cell + Vector3Int.up);
            Assert.IsFalse(input.HighlightVisible);
            yield break;
        }

        [UnityTest]
        public IEnumerator BlockedWalkIsUnreachable()
        {
            var cell = Ahead(6);
            view.Bag.Add(BlockMaterial.Stone, 60);
            for (int dz = -3; dz <= 3; dz++)
                for (int y = 1; y <= 3; y++)
                    Assert.AreEqual(EditResult.Ok, view.Place(new Vector3Int(spot.x + 3, spot.y + y, spot.z + dz), BlockMaterial.Stone));
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(RightClickAction.Dig, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk(8f);
            Assert.AreEqual(WalkToActState.Unreachable, input.Walk.State);
            yield return null;
            Assert.AreNotEqual(BlockMaterial.Empty, view.World.GetMaterial(cell.x, cell.y, cell.z));
            StringAssert.Contains("Không tới được ô này", input.HudText()); // vi-text
        }

        [UnityTest]
        public IEnumerator ModeKeyCancelsWalkAndStops()
        {
            var cell = Ahead(6);
            input.ToggleMode(ControlMode.Dig);
            input.RightClickAt(true, cell, cell + Vector3Int.up);
            Assert.AreEqual(WalkToActState.Walking, input.Walk.State);
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(WalkToActState.Cancelled, input.Walk.State);
            Assert.AreNotEqual(ClickMoveState.Walking, player.ClickMove);
            float t = 0f;
            while (t < 1f) { yield return null; t += Time.deltaTime; }
            Assert.AreNotEqual(BlockMaterial.Empty, view.World.GetMaterial(cell.x, cell.y, cell.z));
        }

        [UnityTest]
        public IEnumerator TillMarksAfterWalking()
        {
            var cell = Ahead(6);
            Assert.IsTrue(sv.Sim.CanMarkTill(new CellPos(cell.x, cell.y, cell.z)), "target cell markable");
            input.ToggleMode(ControlMode.Till);
            Assert.AreEqual(RightClickAction.Till, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.GreaterOrEqual(sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z)), 0);
        }

        [UnityTest]
        public IEnumerator RainAfterWalking()
        {
            var cell = Ahead(6);
            view.World.Set(cell.x, cell.y, cell.z, Cell.Make(BlockMaterial.Dirt));
            input.ToggleMode(ControlMode.Rain);
            Assert.AreEqual(RightClickAction.Rain, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.IsFalse(farming.RainCooldown.Ready);
        }

        [UnityTest]
        public IEnumerator TempHoldPlacesBlockAfterWalking()
        {
            view.Bag.Add(BlockMaterial.Dirt, 3);
            input.CycleTempHold();
            var cell = Ahead(6);
            var place = cell + Vector3Int.up;
            Assert.AreEqual(RightClickAction.PlaceBlock, input.RightClickAt(true, cell, place));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.AreEqual(BlockMaterial.Dirt, view.World.GetMaterial(place.x, place.y, place.z));
        }

        [UnityTest]
        public IEnumerator TempHoldCameraPlacesAfterWalking()
        {
            input.CycleTempHold();
            input.CycleTempHold();
            var cell = Ahead(6);
            Assert.AreEqual(RightClickAction.UseHeld, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.IsTrue(input.Observation.HasCamera);
        }

        [UnityTest]
        public IEnumerator CollectTakesOnlyWithin2()
        {
            var near = view.Editor.Drops.Add(spot.x + 1, spot.y + 1, spot.z, ItemId.Block(BlockMaterial.Dirt), 1);
            var far = view.Editor.Drops.Add(spot.x + 3, spot.y + 1, spot.z, ItemId.Block(BlockMaterial.Stone), 1);
            Assert.AreEqual(1, input.CollectNearby());
            Assert.IsNull(view.Editor.Drops.Find(near.Id));
            Assert.IsNotNull(view.Editor.Drops.Find(far.Id));
            yield break;
        }

        [UnityTest]
        public IEnumerator RightClickDropWalksThenPicks()
        {
            var stack = view.Editor.Drops.Add(spot.x + 6, spot.y + 1, spot.z, ItemId.Block(BlockMaterial.Stone), 2);
            int s0 = view.Bag.Get(BlockMaterial.Stone);
            Assert.AreEqual(RightClickAction.PickDrop, input.RightClickAt(false, default, default, stack));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.IsNull(view.Editor.Drops.Find(stack.Id));
            Assert.AreEqual(s0 + 2, view.Bag.Get(BlockMaterial.Stone));
        }

        [UnityTest]
        public IEnumerator LeftClickPlotOpensPanelWithUnmark()
        {
            var cell = Ahead(6);
            Assert.AreEqual(TillResult.Marked, sv.UseTill(cell));
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            Assert.IsTrue(input.ViewPlotAt(cell));
            Assert.Less(sv.OpenPlotId, 0);
            float t = 0f;
            while (t < 5f && sv.OpenPlotId < 0) { yield return null; t += Time.deltaTime; }
            Assert.AreEqual(id, sv.OpenPlotId);
            Assert.IsTrue(sv.UnmarkOpenPlot());
            Assert.IsFalse(sv.SignVisible(id));
            Assert.AreEqual(-1, sv.OpenPlotId);
        }

        [UnityTest]
        public IEnumerator UnmarkOpenPlotCancelsSelfTill()
        {
            var cell = Ahead(6);
            Assert.AreEqual(TillResult.Marked, sv.UseTill(cell));
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            Assert.IsTrue(farming.BeginSelfTill(id));
            Assert.AreEqual(SelfTillState.Walking, farming.TillState);
            Assert.IsTrue(sv.OpenPlotPanel(id));
            Assert.IsTrue(sv.UnmarkOpenPlot());
            Assert.AreEqual(SelfTillState.None, farming.TillState);
            yield break;
        }

        [UnityTest]
        public IEnumerator DigModeOnRipePlotDigsIt()
        {
            var cell = Ahead(2);
            Assert.AreEqual(TillResult.Marked, sv.UseTill(cell));
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            Assert.IsTrue(sv.Sim.PlayerTill(id));
            view.Bag.AddSeeds(0, 1);
            Assert.IsTrue(sv.Sim.EnqueueCrop(id, 0));
            Assert.AreEqual(PlayerSowResult.Ok, sv.Sim.PlayerSow(id, 0, view.Bag));
            Assert.AreEqual(RainResult.Watered, sv.Sim.Rain(new CellPos(cell.x, cell.y, cell.z)));
            sv.Sim.State.Plots[id].RipeTick = sv.Sim.State.Tick;
            sv.Sim.Tick();
            Assert.AreEqual(PlotPhase.Harvest, sv.Sim.State.Plots[id].Phase);
            Assert.AreEqual(RightClickAction.Harvest, input.Preview(true, cell, cell + Vector3Int.up));
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(RightClickAction.Dig, input.Preview(true, cell, cell + Vector3Int.up));
            yield break;
        }

        [UnityTest]
        public IEnumerator SowModeWalksThenSows()
        {
            var cell = Ahead(6);
            Assert.AreEqual(TillResult.Marked, sv.UseTill(cell));
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            Assert.IsTrue(sv.Sim.PlayerTill(id));
            view.Bag.AddSeeds(0, 2);
            input.ToggleMode(ControlMode.Dig);
            Assert.IsTrue(farming.EnterSowMode(0));
            Assert.AreEqual(ControlMode.Default, input.Control.Mode);
            int seeds = view.Bag.GetSeeds(0);
            Assert.AreEqual(RightClickAction.UseHeld, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.AreEqual(seeds - 1, view.Bag.GetSeeds(0));
        }
    }
}
