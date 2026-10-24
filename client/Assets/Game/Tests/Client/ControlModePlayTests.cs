using System;
using System.Collections;
using Game.Core;
using Game.Core.DayCycle;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7l c1 (brief 0058) — chế độ D/Q/E/R, chuột phải tự đi tới 2 ô, lớp đỏ, F Thu 2 ô, click trái ô ruộng + Bỏ đánh dấu. Brief 0059 (7l c2): cầm vật phẩm từ túi, vòng tầm khi chuột phải ngoài tầm, F vùng + gió.</summary>
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

        [UnityTest]
        public IEnumerator SpellMenuRainEntersRainMode()
        {
            var dial = UnityEngine.Object.FindAnyObjectByType<TimeDial>();
            if (dial == null)
            {
                var lightGo = new GameObject("Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                var day = camGo.AddComponent<DayCycleView>();
                day.sun = light;
                dial = camGo.AddComponent<TimeDial>();
            }
            for (int i = 0; i < 600 && !dial.IsReady; i++) yield return null;
            Assert.IsTrue(dial.IsReady, "Dial not built");
            Assert.IsTrue(dial.ChooseRain());
            Assert.AreEqual(ControlMode.Rain, input.Control.Mode);
            Assert.IsTrue(dial.ChooseRain());
            Assert.AreEqual(ControlMode.Rain, input.Control.Mode);
            farming.RainCooldown.Start();
            yield return null;
            yield return null;
            Assert.AreEqual("10 giây", dial.RainCooldownText); // vi-text
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

        void SetBag(BlockMaterial m, int n)
        {
            while (view.Bag.TryTake(m)) { }
            if (n > 0) view.Bag.Add(m, n);
        }

        void SetSeeds(int n)
        {
            int have = view.Bag.GetSeeds(0);
            if (have > 0) view.Bag.TryTakeSeeds(0, have);
            if (n > 0) view.Bag.AddSeeds(0, n);
        }

        /// <summary>Ô ruộng đã xới (trống) ở Ahead(dx); trả id.</summary>
        int TilledPlot(int dx)
        {
            var cell = Ahead(dx);
            Assert.AreEqual(TillResult.Marked, sv.UseTill(cell));
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            Assert.IsTrue(sv.Sim.PlayerTill(id));
            return id;
        }

        [UnityTest]
        public IEnumerator ModeKeysAndHoldItem()
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
            SetBag(BlockMaterial.Dirt, 2);
            SetBag(BlockMaterial.Stone, 1);
            SetBag(BlockMaterial.Sand, 0);
            Assert.IsTrue(input.HoldItem(ItemId.Block(BlockMaterial.Dirt)));
            Assert.AreEqual(BlockMaterial.Dirt, input.Control.Held.Material);
            Assert.IsTrue(input.HoldItem(ItemId.Tool(ToolItem.ObservationCamera)));
            Assert.AreEqual(ItemKind.Tool, input.Control.Held.Kind);
            Assert.IsFalse(input.HoldItem(ItemId.Produce(0)));
            Assert.IsTrue(input.HoldItem(ItemId.Block(BlockMaterial.Stone)));
            Assert.IsFalse(input.HoldItem(ItemId.Block(BlockMaterial.Sand)));
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
        public IEnumerator OutOfReachRightClickFlashesRing()
        {
            var cell = Ahead(12);
            input.ToggleMode(ControlMode.Dig);
            var before = view.World.GetMaterial(cell.x, cell.y, cell.z);
            input.ShowAim(input.Preview(true, cell, cell + Vector3Int.up), true, cell, cell + Vector3Int.up);
            Assert.IsFalse(input.ReachRingVisible); // di chuột không hiện
            Assert.AreEqual(RightClickAction.OutOfReach, input.RightClickAt(true, cell, cell + Vector3Int.up));
            Assert.AreEqual(ClickMoveState.None, player.ClickMove);
            Assert.AreEqual(before, view.World.GetMaterial(cell.x, cell.y, cell.z));
            Assert.IsTrue(input.ReachRingVisible);
            Assert.IsFalse(input.HighlightVisible);
            Assert.Greater(player.Markers.ReachRingAlpha, 0.2f);
            Assert.LessOrEqual(player.Markers.ReachRingAlpha, 0.25f);
            yield return Wait(0.5f);
            Assert.Less(player.Markers.ReachRingAlpha, 0.2f);
            Assert.Greater(player.Markers.ReachRingAlpha, 0f);
            yield return Wait(PlayerMotion.ReachRingFadeSeconds + 0.2f);
            Assert.IsFalse(input.ReachRingVisible);
            input.ToggleMode(ControlMode.Default);
            input.RightClickAt(true, cell, cell + Vector3Int.up);
            Assert.IsTrue(input.ReachRingVisible);
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

        void Drain() { while (player.Vitals.TrySpend(ManaAction.Dig)) { } }

        [UnityTest]
        public IEnumerator DigSpendsMana()
        {
            var cell = Ahead(1);
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(RightClickAction.Dig, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.AreEqual(BlockMaterial.Empty, view.World.GetMaterial(cell.x, cell.y, cell.z));
            Assert.AreEqual(95, player.Vitals.Mana);
        }

        [UnityTest]
        public IEnumerator NoManaBlocksDigAndFlashes()
        {
            Drain();
            var cell = Ahead(6);
            var before = view.World.GetMaterial(cell.x, cell.y, cell.z);
            input.ToggleMode(ControlMode.Dig);
            Assert.AreEqual(RightClickAction.None, input.RightClickAt(true, cell, cell + Vector3Int.up));
            Assert.AreNotEqual(WalkToActState.Walking, input.Walk.State);
            Assert.AreEqual(ClickMoveState.None, player.ClickMove);
            Assert.AreEqual(before, view.World.GetMaterial(cell.x, cell.y, cell.z));
            Assert.IsTrue(player.ManaFlashing);
            StringAssert.Contains("Không đủ mana: cần 5, còn 0", input.HudText()); // vi-text
            yield return Wait(PlayerMotion.ManaFlashSeconds + 0.2f);
            Assert.IsFalse(player.ManaFlashing);
        }

        [UnityTest]
        public IEnumerator PlaceSpendsMana()
        {
            SetBag(BlockMaterial.Dirt, 2);
            Assert.IsTrue(input.HoldItem(ItemId.Block(BlockMaterial.Dirt)));
            var cell = Ahead(2);
            var place = cell + Vector3Int.up;
            Assert.AreEqual(RightClickAction.PlaceBlock, input.RightClickAt(true, cell, place));
            yield return WaitWalk();
            Assert.AreEqual(BlockMaterial.Dirt, view.World.GetMaterial(place.x, place.y, place.z));
            Assert.AreEqual(97, player.Vitals.Mana);
        }

        [UnityTest]
        public IEnumerator RainSpendsMana()
        {
            var cell = Ahead(1);
            view.World.Set(cell.x, cell.y, cell.z, Cell.Make(BlockMaterial.Dirt));
            input.ToggleMode(ControlMode.Rain);
            Assert.AreEqual(RightClickAction.Rain, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.IsFalse(farming.RainCooldown.Ready);
            Assert.AreEqual(85, player.Vitals.Mana);
        }

        [UnityTest]
        public IEnumerator RainBlockedWithoutMana()
        {
            var cell = Ahead(1);
            view.World.Set(cell.x, cell.y, cell.z, Cell.Make(BlockMaterial.Dirt));
            Drain();
            Assert.AreEqual(RainResult.NoEffect, farming.CastRain(cell));
            Assert.IsTrue(farming.RainCooldown.Ready);
            StringAssert.Contains("Không đủ mana: cần 15, còn 0", farming.StatusLine); // vi-text
            Assert.AreEqual(BlockMaterial.Dirt, view.World.GetMaterial(cell.x, cell.y, cell.z));
            Assert.IsTrue(player.ManaFlashing);
            yield break;
        }

        [UnityTest]
        public IEnumerator WalkingRegensMana()
        {
            Drain();
            player.MoveTo(new Vector3(spot.x + 6.5f, 0f, spot.z + 0.5f));
            float t = 0f;
            while (t < 5f && player.ClickMove == ClickMoveState.Walking) { yield return null; t += Time.deltaTime; }
            Assert.AreNotEqual(ClickMoveState.Walking, player.ClickMove);
            Assert.That(player.Vitals.Mana, Is.InRange(10, 12));
        }

        [UnityTest]
        public IEnumerator RespawnShowsLineAndCancelsWalk()
        {
            var cell = Ahead(6);
            input.ToggleMode(ControlMode.Dig);
            input.RightClickAt(true, cell, cell + Vector3Int.up);
            Assert.AreEqual(WalkToActState.Walking, input.Walk.State);
            player.Place(view.transform.TransformPoint(new Vector3(spot.x + 0.5f, spot.y + 1f + 16.5f, spot.z + 0.5f)), 0f);
            float t = 0f;
            while (t < 5f && player.Respawns < 1) { yield return null; t += Time.deltaTime; }
            Assert.AreEqual(1, player.Respawns);
            yield return null;
            Assert.AreNotEqual(WalkToActState.Walking, input.Walk.State);
            StringAssert.Contains("tỉnh lại ở chỗ xuất phát", input.HudText()); // vi-text
            Assert.AreEqual(player.Vitals.HpMax, player.Vitals.Hp);
            Assert.AreEqual(player.Vitals.ManaMax, player.Vitals.Mana);
        }

        [UnityTest]
        public IEnumerator HeldBlockPlacesAfterWalkingAndExitsWhenEmpty()
        {
            SetBag(BlockMaterial.Dirt, 1);
            Assert.IsTrue(input.HoldItem(ItemId.Block(BlockMaterial.Dirt)));
            var cell = Ahead(6);
            var place = cell + Vector3Int.up;
            Assert.AreEqual(RightClickAction.PlaceBlock, input.RightClickAt(true, cell, place));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.AreEqual(BlockMaterial.Dirt, view.World.GetMaterial(place.x, place.y, place.z));
            Assert.IsFalse(input.Control.HasHeld);
            StringAssert.Contains("hết Đất, thôi cầm", input.HudText()); // vi-text
        }

        [UnityTest]
        public IEnumerator HeldCameraPlacesAfterWalking()
        {
            Assert.IsTrue(input.HoldItem(ItemId.Tool(ToolItem.ObservationCamera)));
            var cell = Ahead(6);
            Assert.AreEqual(RightClickAction.UseHeld, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.IsTrue(input.Observation.HasCamera);
            Assert.AreEqual(ItemKind.Tool, input.Control.Held.Kind);
        }

        [UnityTest]
        public IEnumerator GhostRedWhenCannotPlace()
        {
            SetBag(BlockMaterial.Dirt, 3);
            Assert.IsTrue(input.HoldItem(ItemId.Block(BlockMaterial.Dirt)));
            var cell = Ahead(3);
            Assert.AreEqual(RightClickAction.None, input.Preview(true, cell, cell)); // ô đặc
            input.ShowAim(RightClickAction.None, true, cell, cell);
            Assert.IsTrue(input.GhostVisible);
            Assert.AreEqual(TerrainEditInput.InvalidColor, input.GhostColor);
            var place = cell + Vector3Int.up;
            var a = input.Preview(true, cell, place);
            Assert.AreEqual(RightClickAction.PlaceBlock, a);
            input.ShowAim(a, true, cell, place);
            Assert.IsTrue(input.GhostVisible);
            Assert.AreEqual(TerrainEditInput.GhostColorOf(BlockMaterial.Dirt), input.GhostColor);
            yield break;
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
        public IEnumerator HeldSeedWalksThenSows()
        {
            var cell = Ahead(6);
            TilledPlot(6);
            SetSeeds(2);
            input.ToggleMode(ControlMode.Dig);
            Assert.IsTrue(input.HoldItem(ItemId.SeedBag(0)));
            Assert.AreEqual(ControlMode.Default, input.Control.Mode);
            Assert.AreEqual(RightClickAction.UseHeld, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.AreEqual(WalkToActState.Done, input.Walk.State);
            Assert.AreEqual(1, view.Bag.GetSeeds(0));
            Assert.IsTrue(input.Control.HasHeld);
            Assert.AreEqual(ItemId.SeedBag(0), input.Control.Held);
        }

        [UnityTest]
        public IEnumerator HeldSeedLastSeedExits()
        {
            var cell = Ahead(2);
            TilledPlot(2);
            SetSeeds(1);
            Assert.IsTrue(input.HoldItem(ItemId.SeedBag(0)));
            Assert.AreEqual(RightClickAction.UseHeld, input.RightClickAt(true, cell, cell + Vector3Int.up));
            yield return WaitWalk();
            Assert.AreEqual(0, view.Bag.GetSeeds(0));
            Assert.IsFalse(input.Control.HasHeld);
            StringAssert.Contains("thôi cầm", input.HudText()); // vi-text
        }

        [UnityTest]
        public IEnumerator HeldSeedRejectsQueuedPlot()
        {
            var cell = Ahead(2);
            int id = TilledPlot(2);
            Assert.IsTrue(sv.Sim.EnqueueCrop(id, 0));
            Assert.IsTrue(sv.Sim.EnqueueCrop(id, 0));
            Assert.Greater(sv.Sim.State.Plots[id].Slots.Count, 0);
            SetSeeds(2);
            Assert.IsTrue(input.HoldItem(ItemId.SeedBag(0)));
            Assert.AreNotEqual(RightClickAction.UseHeld, input.Preview(true, cell, cell + Vector3Int.up));
            input.RightClickAt(true, cell, cell + Vector3Int.up);
            yield return WaitWalk();
            Assert.AreEqual(2, view.Bag.GetSeeds(0));
            Assert.AreEqual("Gieo: ô còn hạt trong hàng đợi — chỉ gieo tay ô có hàng đợi trống", farming.StatusLine); // vi-text
        }

        [UnityTest]
        public IEnumerator HeldSeedDefersToHarvest()
        {
            var cell = Ahead(2);
            int id = TilledPlot(2);
            SetSeeds(1);
            Assert.IsTrue(sv.Sim.EnqueueCrop(id, 0));
            Assert.AreEqual(PlayerSowResult.Ok, sv.Sim.PlayerSow(id, 0, view.Bag));
            Assert.AreEqual(RainResult.Watered, sv.Sim.Rain(new CellPos(cell.x, cell.y, cell.z)));
            sv.Sim.State.Plots[id].RipeTick = sv.Sim.State.Tick;
            sv.Sim.Tick();
            Assert.AreEqual(PlotPhase.Harvest, sv.Sim.State.Plots[id].Phase);
            SetSeeds(1);
            Assert.IsTrue(input.HoldItem(ItemId.SeedBag(0)));
            Assert.AreEqual(RightClickAction.Harvest, input.Preview(true, cell, cell + Vector3Int.up));
            yield break;
        }

        [UnityTest]
        public IEnumerator CollectShowsAreaAndWindEvenWhenEmpty()
        {
            Assert.AreEqual(0, input.CollectNearby());
            Assert.IsTrue(player.Markers.CollectVisible);
            Assert.AreEqual(MoveMarkers.WindStreaks, player.Markers.WindVisibleCount);
            Assert.AreEqual(0.5f, player.Markers.CollectAlpha, 0.05f);
            yield return Wait(PlayerMotion.CollectShowSeconds + 0.2f);
            Assert.IsFalse(player.Markers.CollectVisible);
            Assert.AreEqual(0, player.Markers.WindVisibleCount);
        }

        [UnityTest]
        public IEnumerator CollectedDropFliesForConfiguredTime()
        {
            view.Editor.Drops.Add(spot.x + 1, spot.y + 1, spot.z, ItemId.Block(BlockMaterial.Dirt), 1);
            Assert.AreEqual(1, input.CollectNearby());
            Assert.AreEqual(1, input.DropView.FlyingCount);
            yield return Wait(PlayerMotion.CollectFlySeconds + 0.2f);
            Assert.AreEqual(0, input.DropView.FlyingCount);
        }

        [UnityTest]
        public IEnumerator BagPanelHoldsAndCloses()
        {
            var inv = input.Inventory;
            inv.ToggleBag();
            Assert.IsTrue(inv.BagOpen);
            Assert.AreEqual(ItemId.Tool(ToolItem.ObservationCamera), inv.SlotAt(ItemContainer.Bag, 0).Item);
            Assert.IsTrue(inv.HoldFromSlot(ItemContainer.Bag, 0));
            Assert.IsFalse(inv.BagOpen);
            Assert.AreEqual(ItemKind.Tool, input.Control.Held.Kind);
            view.Bag.AddProduce(0, 1);
            inv.ToggleBag();
            Assert.IsFalse(inv.HoldFromSlot(ItemContainer.Bag, 1));
            yield break;
        }
    }
}
