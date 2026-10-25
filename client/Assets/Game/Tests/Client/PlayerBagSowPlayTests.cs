using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Sim 7f client B (brief 0042) — bảng túi, cầm túi hạt (brief 0059), kho ↔ túi, click khối kho, UnmarkAimed.</summary>
    public class PlayerBagSowPlayTests
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
            farming = input.Farming;
            Assert.IsNotNull(farming);

            // Đứng ở ô đánh dấu được quanh ô ruộng 0 có nhiều ô đánh dấu được cách 3,5–6,5 ô.
            var s0 = sv.Sim.State.Plots[0].Soil;
            var all = new List<Vector3Int>();
            for (int dz = -8; dz <= 8; dz++)
                for (int dx = -8; dx <= 8; dx++)
                {
                    int x = s0.X + dx, z = s0.Z + dz;
                    if (!view.World.Size.InBounds(x, 0, z)) continue;
                    int h = view.World.SurfaceHeight(x, z);
                    if (sv.Sim.CanMarkTill(new CellPos(x, h, z))) all.Add(new Vector3Int(x, h, z));
                }
            bool found = false;
            foreach (var c in all)
            {
                int n = 0;
                foreach (var d in all)
                {
                    float e = Mathf.Sqrt((d.x - c.x) * (d.x - c.x) + (d.z - c.z) * (d.z - c.z));
                    if (e >= 3.5f && e <= 6.5f) n++;
                }
                if (n >= 6) { spot = c; found = true; break; }
            }
            Assert.IsTrue(found, "Could not find a standing spot around the camp");
            player.Place(view.transform.TransformPoint(new Vector3(spot.x + 0.5f, spot.y + 1f, spot.z + 0.5f)), 0f);
            for (int i = 0; i < 600 && !player.Ready; i++) yield return null;
            Assert.IsTrue(player.Ready);
            for (int i = 0; i < 20; i++) yield return null;
            sv.enabled = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(playerGo);
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        CellPos FeetCell()
        {
            var f = player.FeetLocal;
            return new CellPos(Mathf.FloorToInt(f.x), Mathf.FloorToInt(f.y + 0.05f), Mathf.FloorToInt(f.z));
        }

        IEnumerable<Vector3Int> Candidates()
        {
            var f = FeetCell();
            var list = new List<(int d, Vector3Int c)>();
            for (int dz = -6; dz <= 6; dz++)
                for (int dx = -6; dx <= 6; dx++)
                {
                    int x = f.X + dx, z = f.Z + dz;
                    float e = Mathf.Sqrt(dx * dx + dz * dz);
                    if (e < 3.5f || e > 6.5f || !view.World.Size.InBounds(x, 0, z)) continue;
                    int h = view.World.SurfaceHeight(x, z);
                    if (sv.Sim.CanMarkTill(new CellPos(x, h, z))) list.Add((-Mathf.RoundToInt(e * 10f), new Vector3Int(x, h, z)));
                }
            list.Sort((a, b) => a.d.CompareTo(b.d));
            foreach (var e in list) yield return e.c;
        }

        /// <summary>Đánh dấu một ô có đường đi tới; trả id, ô và đường.</summary>
        int MarkReachable(out Vector3Int cell, List<CellPos> path)
        {
            foreach (var c in Candidates())
            {
                if (sv.UseTill(c) != TillResult.Marked) continue;
                int id = sv.Sim.PlotAt(new CellPos(c.x, c.y, c.z));
                if (sv.Sim.FindPlayerPathToPlot(FeetCell(), id, path)) { cell = c; return id; }
                sv.UnmarkAt(c);
            }
            Assert.Fail("No marked cell with a path");
            cell = default;
            return -1;
        }

        /// <summary>Đưa ô ruộng tới pha Water (trả id). Gần nhân vật.</summary>
        int PlotToPhase(PlotPhase target, out Vector3Int cell)
        {
            int id = MarkReachable(out cell, new List<CellPos>());
            var sim = sv.Sim;
            Assert.IsTrue(sim.PlayerTill(id));
            if (target == PlotPhase.Idle) return id;
            view.Bag.AddSeeds(0, 1);
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            Assert.AreEqual(PlayerSowResult.Ok, sim.PlayerSow(id, 0, view.Bag));
            if (target == PlotPhase.Water) return id;
            Assert.AreEqual(RainResult.Watered, sim.Rain(new CellPos(cell.x, cell.y, cell.z)));
            sim.State.Plots[id].RipeTick = sim.State.Tick;
            sim.Tick();
            Assert.AreEqual(PlotPhase.Harvest, sim.State.Plots[id].Phase);
            return id;
        }

        void ToTill()
        {
            if (input.Control.Mode != ControlMode.Till) input.ToggleMode(ControlMode.Till);
        }

        IEnumerator PlaceAt(int x, int z)
        {
            player.Place(view.transform.TransformPoint(new Vector3(x + 0.5f, view.World.SurfaceHeight(x, z) + 1f, z + 0.5f)), 0f);
            for (int i = 0; i < 600 && !player.Ready; i++) yield return null;
            Assert.IsTrue(player.Ready);
            for (int i = 0; i < 5; i++) yield return null;
        }

        IEnumerator GoNearStorage()
        {
            var c = sv.Sim.State.Storages[0].Cell;
            yield return PlaceAt(c.X + 2, c.Z);
        }

        IEnumerator GoFarFromStorage()
        {
            foreach (var dz in new[] { 14, -14, 20, -20 })
                foreach (var dx in new[] { 14, -14, 0 })
                {
                    int x = spot.x + dx, z = spot.z + dz;
                    if (!view.World.Size.InBounds(x, 0, z)) continue;
                    bool far = true;
                    foreach (var s in sv.Sim.State.Storages)
                        if (Mathf.Sqrt((s.Cell.X - x) * (s.Cell.X - x) + (s.Cell.Z - z) * (s.Cell.Z - z)) < 12f) far = false;
                    if (!far) continue;
                    yield return PlaceAt(x, z);
                    yield break;
                }
            Assert.Fail("No spot >= 12 cells from storage");
        }

        void EnterSow(int crop, int seeds)
        {
            view.Bag.AddSeeds(crop, seeds);
            Assert.IsTrue(input.HoldItem(ItemId.SeedBag(crop)));
            Assert.AreEqual(ItemId.SeedBag(crop), input.Control.Held);
        }

        /// <summary>Tâm rect (toạ độ GUI) → điểm màn hình gốc dưới-trái.</summary>
        static Vector2 ScreenOf(Rect r, float s) => new Vector2(r.center.x * s, Screen.height - r.center.y * s);

        [Test]
        public void HoldSeedRules()
        {
            view.Bag.AddSeeds(0, 2);
            var inv = input.Inventory;
            inv.ToggleBag();
            Assert.IsTrue(inv.HoldFromSlot(ItemContainer.Bag, 1)); // ô 0 = camera quan sát, ô 1 = túi hạt
            Assert.AreEqual(ItemId.SeedBag(0), input.Control.Held);
            Assert.IsFalse(inv.BagOpen);
            StringAssert.Contains("Cầm: <b><color=yellow>Túi hạt Cây A</color></b>", input.HudText()); // vi-text
            Assert.IsFalse(input.HoldItem(ItemId.SeedBag(1))); // hết hạt loại 1
            Assert.AreEqual(ItemId.SeedBag(0), input.Control.Held);

            input.Control.ClearToDefault();
            ToTill();
            MarkReachable(out var cell, new List<CellPos>());
            input.RightClickAt(true, cell, cell);
            Assert.AreEqual(SelfTillState.Walking, farming.TillState);
            Assert.IsTrue(input.HoldItem(ItemId.SeedBag(0)));
            Assert.AreEqual(SelfTillState.None, farming.TillState);
        }

        [Test]
        public void SowIdlePlot()
        {
            int id = PlotToPhase(PlotPhase.Idle, out var cell);
            EnterSow(0, 2);
            int slots = sv.Sim.State.Plots[id].Slots.Count;
            player.Place(view.transform.TransformPoint(new Vector3(cell.x + 0.5f, cell.y + 1f, cell.z + 1.5f)), 0f);
            Assert.AreEqual(RightClickAction.UseHeld, input.RightClickAt(true, cell, cell));
            var p = sv.Sim.State.Plots[id];
            Assert.AreEqual(PlotPhase.Water, p.Phase);
            Assert.AreEqual(0, p.CurrentCrop);
            Assert.AreEqual(1, view.Bag.GetSeeds(0));
            Assert.AreEqual(slots, p.Slots.Count);
            Assert.AreEqual($"Gieo Cây A vào ô {cell}", farming.StatusLine); // vi-text
            Assert.AreEqual(ItemId.SeedBag(0), input.Control.Held);
        }

        [Test]
        public void SowWaitingSameCrop()
        {
            int id = PlotToPhase(PlotPhase.Idle, out var cell);
            Assert.IsTrue(sv.Sim.EnqueueCrop(id, 0));
            Assert.AreEqual(PlotPhase.Sow, sv.Sim.State.Plots[id].Phase);
            view.Bag.AddSeeds(0, 2);
            Assert.IsTrue(farming.SowAt(cell, 0));
            Assert.AreEqual(PlotPhase.Water, sv.Sim.State.Plots[id].Phase);
        }

        [UnityTest]
        public IEnumerator SowRejections()
        {
            view.Bag.AddSeeds(0, 5);
            int seeds = view.Bag.GetSeeds(0);

            // chờ gieo Cây B
            int id = PlotToPhase(PlotPhase.Idle, out var cell);
            Assert.IsTrue(sv.Sim.EnqueueCrop(id, 1));
            Assert.IsFalse(farming.SowAt(cell, 0));
            Assert.AreEqual("Ô đang chờ gieo Cây B", farming.StatusLine); // vi-text

            // đã có cây
            int id2 = PlotToPhase(PlotPhase.Water, out var cell2);
            Assert.IsFalse(farming.SowAt(cell2, 0));
            Assert.AreEqual("Gieo: ô đã có cây", farming.StatusLine); // vi-text
            seeds = view.Bag.GetSeeds(0);

            // biển chưa xới
            int id3 = MarkReachable(out var cell3, new List<CellPos>());
            Assert.IsFalse(farming.SowAt(cell3, 0));
            Assert.AreEqual("Gieo: ô chưa xới xong", farming.StatusLine); // vi-text

            // đất trống ngoài ô ruộng
            Vector3Int free = default;
            bool found = false;
            foreach (var c in Candidates())
                if (sv.Sim.PlotAt(new CellPos(c.x, c.y, c.z)) < 0) { free = c; found = true; break; }
            Assert.IsTrue(found);
            Assert.IsFalse(farming.SowAt(free, 0));
            Assert.AreEqual("Gieo: chỉ gieo vào ô ruộng đã xới", farming.StatusLine); // vi-text

            Assert.AreEqual(seeds, view.Bag.GetSeeds(0));

            // ngoài tầm: ô ruộng đứng yên rồi đi xa
            int id4 = PlotToPhase(PlotPhase.Idle, out var cell4);
            seeds = view.Bag.GetSeeds(0);
            yield return PlaceAt(cell4.x + 12, cell4.z);
            Assert.IsFalse(farming.SowAt(cell4, 0));
            Assert.AreEqual("Gieo: ngoài tầm 10 ô", farming.StatusLine); // vi-text
            Assert.AreEqual(seeds, view.Bag.GetSeeds(0));
        }

        [Test]
        public void SowLastSeedExits()
        {
            int id = PlotToPhase(PlotPhase.Idle, out var cell);
            while (view.Bag.GetSeeds(0) > 0) view.Bag.TryTakeSeeds(0, 1);
            EnterSow(0, 1);
            player.Place(view.transform.TransformPoint(new Vector3(cell.x + 0.5f, cell.y + 1f, cell.z + 1.5f)), 0f);
            Assert.AreEqual(RightClickAction.UseHeld, input.RightClickAt(true, cell, cell));
            Assert.AreEqual(0, view.Bag.GetSeeds(0));
            Assert.IsFalse(input.Control.HasHeld);
            StringAssert.Contains("thôi cầm", input.HudText()); // vi-text
        }

        [Test]
        public void HeldSeedIgnoresDigMode()
        {
            input.ToggleMode(ControlMode.Dig);
            EnterSow(0, 2);
            Assert.AreEqual(ControlMode.Default, input.Control.Mode, "holding resets to Default");
            Vector3Int grass = default;
            bool found = false;
            foreach (var c in Candidates())
                if (sv.Sim.PlotAt(new CellPos(c.x, c.y, c.z)) < 0) { grass = c; found = true; break; }
            Assert.IsTrue(found);
            var before = view.World.Get(grass.x, grass.y, grass.z);
            Assert.AreEqual(RightClickAction.None, input.RightClickAt(true, grass, grass));
            Assert.AreEqual(before, view.World.Get(grass.x, grass.y, grass.z));
            Assert.AreEqual("Gieo: chỉ gieo vào ô ruộng đã xới", farming.StatusLine); // vi-text
        }

        [Test]
        public void ExitHold()
        {
            EnterSow(0, 2);
            input.ToggleMode(ControlMode.Default);
            Assert.IsFalse(input.Control.HasHeld);
            EnterSow(0, 2);
            input.ToggleMode(ControlMode.Dig);
            Assert.IsFalse(input.Control.HasHeld, "mode key puts the item away");
            Assert.AreEqual(ControlMode.Dig, input.Control.Mode);
        }

        [UnityTest]
        public IEnumerator ClickStorageBlock()
        {
            Collider storage = null, terrain = null;
            foreach (var c in go.GetComponentsInChildren<Collider>())
            {
                if (c.name == "Storage 0") storage = c;
                else if (terrain == null && sv.StorageIdOf(c) < 0 && c.GetComponent<MeshCollider>() != null) terrain = c;
            }
            Assert.IsNotNull(storage);
            Assert.AreEqual(0, sv.StorageIdOf(storage));
            if (terrain != null) Assert.AreEqual(-1, sv.StorageIdOf(terrain));
            yield return GoNearStorage();
            input.Inventory.CloseAll();
            Assert.IsTrue(sv.OpenStockAt(0));
            Assert.IsTrue(input.Inventory.StockOpen);
            input.Inventory.CloseAll();
            yield return GoFarFromStorage();
            Assert.IsFalse(sv.OpenStockAt(0));
            Assert.IsFalse(input.Inventory.StockOpen);
        }

        [UnityTest]
        public IEnumerator ArrivedClearsWalkingLine()
        {
            ToTill();
            MarkReachable(out var cell, new List<CellPos>());
            input.RightClickAt(true, cell, cell);
            float t = 0f;
            while (t < 10f && farming.TillState == SelfTillState.Walking)
            {
                yield return null;
                t += Time.deltaTime;
            }
            Assert.AreEqual(SelfTillState.Tilling, farming.TillState);
            Assert.AreEqual("", farming.StatusLine);
            var lines = farming.HudLines();
            StringAssert.Contains("Đang xới ô", lines); // vi-text
            StringAssert.DoesNotContain("đang đi tới", lines); // vi-text
        }
    }
}
