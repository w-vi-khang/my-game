using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Sim 7f client A (brief 0041) — tự đi, tự xới, huỷ, Mưa, thu hoạch, nút Tự xới, HUD.</summary>
    public class PlayerFarmPlayTests
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
            Assert.IsTrue(found, "Không tìm được chỗ đứng quanh trại");
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
            Assert.Fail("Không có ô đánh dấu nào có đường tới");
            cell = default;
            return -1;
        }

        void ToTill()
        {
            while (input.Tool != EditTool.Till) input.ToggleTool();
        }

        IEnumerator WaitSeconds(float s, System.Func<bool> until)
        {
            float t = 0f;
            while (t < s && !until())
            {
                yield return null;
                t += Time.deltaTime;
            }
        }

        IEnumerator PressForward()
        {
            player.MoveInput = new Vector2(0f, 1f);
            yield return null;
            yield return null;
            player.MoveInput = Vector2.zero;
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

        [UnityTest]
        public IEnumerator AutoWalkArrives()
        {
            var path = new List<CellPos>();
            MarkReachable(out _, path);
            Assert.GreaterOrEqual(path.Count, 1);
            player.StartAutoWalk(path);
            Assert.AreEqual(AutoWalkState.Walking, player.AutoWalk);
            yield return WaitSeconds(10f, () => player.AutoWalk != AutoWalkState.Walking);
            Assert.AreEqual(AutoWalkState.Arrived, player.AutoWalk);
            var last = path[path.Count - 1];
            var f = player.FeetLocal;
            float dx = last.X + 0.5f - f.x, dz = last.Z + 0.5f - f.z;
            Assert.Less(Mathf.Sqrt(dx * dx + dz * dz), 0.3f);

            player.StartAutoWalk(new List<CellPos>());
            Assert.AreEqual(AutoWalkState.Arrived, player.AutoWalk);
        }

        [UnityTest]
        public IEnumerator AutoWalkCancelledByKey()
        {
            var path = new List<CellPos>();
            MarkReachable(out _, path);
            player.StartAutoWalk(path);
            yield return PressForward();
            Assert.AreEqual(AutoWalkState.Cancelled, player.AutoWalk);
        }

        [UnityTest]
        public IEnumerator SelfTillFull()
        {
            ToTill();
            int id = MarkReachable(out var cell, new List<CellPos>());
            input.UseTool(cell, cell);
            Assert.AreEqual(SelfTillState.Walking, farming.TillState);
            yield return WaitSeconds(10f, () => farming.TillState != SelfTillState.Walking);
            Assert.AreEqual(SelfTillState.Tilling, farming.TillState);
            yield return WaitSeconds(8f, () => farming.TillState == SelfTillState.None);
            Assert.AreEqual(SelfTillState.None, farming.TillState);
            Assert.IsTrue(sv.Sim.State.Plots[id].Tilled);
            StringAssert.Contains("Xới xong ô", farming.StatusLine);
        }

        [UnityTest]
        public IEnumerator CancelWhileTilling()
        {
            ToTill();
            int id = MarkReachable(out var cell, new List<CellPos>());
            input.UseTool(cell, cell);
            yield return WaitSeconds(10f, () => farming.TillState != SelfTillState.Walking);
            Assert.AreEqual(SelfTillState.Tilling, farming.TillState);
            yield return PressForward();
            Assert.AreEqual(SelfTillState.None, farming.TillState);
            Assert.IsFalse(sv.Sim.State.Plots[id].Tilled);
            Assert.AreEqual("Huỷ tự xới", farming.StatusLine);
        }

        [UnityTest]
        public IEnumerator TabCancelsWalking()
        {
            ToTill();
            int id = MarkReachable(out var cell, new List<CellPos>());
            input.UseTool(cell, cell);
            Assert.AreEqual(SelfTillState.Walking, farming.TillState);
            input.ToggleTool();
            Assert.AreEqual(SelfTillState.None, farming.TillState);
            Assert.AreEqual("Huỷ tự xới", farming.StatusLine);
            Assert.IsFalse(sv.Sim.State.Plots[id].Tilled);
            yield return null;
            Assert.AreNotEqual(AutoWalkState.Walking, player.AutoWalk);
        }

        [UnityTest]
        public IEnumerator Unreachable()
        {
            ToTill();
            int id = MarkReachable(out var cell, new List<CellPos>());
            var soil = sv.Sim.State.Plots[id].Soil;
            foreach (var (ox, oz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                for (int y = soil.Y + 1; y <= soil.Y + 3; y++)
                    if (view.World.Size.InBounds(soil.X + ox, y, soil.Z + oz))
                        view.World.Set(soil.X + ox, y, soil.Z + oz, Cell.Make(BlockMaterial.Stone));
            Assert.IsFalse(farming.BeginSelfTill(id));
            Assert.AreEqual("Không tới được ô này", farming.StatusLine);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreatureTillsFirst()
        {
            ToTill();
            int id = MarkReachable(out var cell, new List<CellPos>());
            input.UseTool(cell, cell);
            Assert.AreEqual(SelfTillState.Walking, farming.TillState);
            Assert.IsTrue(sv.Sim.PlayerTill(id));
            yield return null;
            Assert.AreEqual(SelfTillState.None, farming.TillState);
            Assert.AreEqual("Tự xới: ô đã được xới", farming.StatusLine);
        }

        [UnityTest]
        public IEnumerator SelfTillButtonRetill()
        {
            int id = PlotToPhase(PlotPhase.Harvest, out var cell);
            Assert.IsTrue(sv.Sim.EnqueueCrop(id, 0));
            Assert.IsTrue(sv.Sim.PlayerHarvest(id, view.Bag));
            Assert.AreEqual(PlotPhase.Till, sv.Sim.State.Plots[id].Phase);
            Assert.IsTrue(sv.Sim.State.Plots[id].Tilled);
            Assert.AreEqual(TillResult.OpenedPlot, sv.UseTill(cell));
            Assert.IsTrue(sv.SelfTillOpenPlot());
            Assert.AreEqual(-1, sv.OpenPlotId);
            Assert.AreNotEqual(SelfTillState.None, farming.TillState);
            farming.CancelSelfTill();

            int idle = PlotToPhase(PlotPhase.Idle, out var cell2);
            Assert.AreEqual(TillResult.OpenedPlot, sv.UseTill(cell2));
            Assert.IsFalse(sv.SelfTillOpenPlot());
            yield return null;
        }

        [UnityTest]
        public IEnumerator UseTillAndUnmarkAt()
        {
            int id = MarkReachable(out var cell, new List<CellPos>());
            Assert.AreEqual(TillResult.SelfTill, sv.UseTill(cell));
            Assert.IsTrue(sv.SignVisible(id));
            Assert.IsTrue(sv.UnmarkAt(cell));
            Assert.IsTrue(sv.Sim.State.Plots[id].Removed);
            int id2 = PlotToPhase(PlotPhase.Idle, out var cell2);
            Assert.IsFalse(sv.UnmarkAt(cell2));
            yield return null;
        }

        /// <summary>Ô cỏ mặt trong tầm → đào → ô đất bên dưới là ô nhắm Mưa.</summary>
        Vector3Int MakeDirtTarget()
        {
            foreach (var c in Candidates())
            {
                if (view.World.GetMaterial(c.x, c.y, c.z) != BlockMaterial.Grass) continue;
                if (sv.Sim.PlotAt(new CellPos(c.x, c.y, c.z)) >= 0) continue;
                Assert.AreEqual(EditResult.Ok, view.Dig(c));
                var below = new Vector3Int(c.x, c.y - 1, c.z);
                if (view.World.GetMaterial(below.x, below.y, below.z) == BlockMaterial.Dirt) return below;
            }
            Assert.Fail("Không tìm được ô đất để thử Mưa");
            return default;
        }

        [UnityTest]
        public IEnumerator RainRules()
        {
            var dirt = MakeDirtTarget();
            Assert.AreEqual(RainResult.Grassed, farming.CastRain(dirt));
            Assert.AreEqual(BlockMaterial.Grass, view.World.GetMaterial(dirt.x, dirt.y, dirt.z));
            Assert.IsFalse(farming.RainCooldown.Ready);
            StringAssert.Contains("Mưa (R): 10 giây", farming.HudLines());
            Assert.IsTrue(farming.RainBox.activeSelf);

            Assert.AreEqual(RainResult.NoEffect, farming.CastRain(dirt));
            Assert.AreEqual("Mưa: còn 10 giây", farming.StatusLine);

            int id = PlotToPhase(PlotPhase.Water, out var cell);
            Assert.AreEqual(PlotPhase.Water, sv.Sim.State.Plots[id].Phase);
            farming.RainCooldown.Advance(10f);
            Assert.AreEqual(RainResult.Watered, farming.CastRain(cell));
            Assert.AreEqual(PlotPhase.Growing, sv.Sim.State.Plots[id].Phase);

            farming.RainCooldown.Advance(10f);
            Assert.AreEqual(RainResult.NoEffect, farming.CastRain(dirt)); // đã là cỏ
            Assert.IsTrue(farming.RainCooldown.Ready);
            var stone = new Vector3Int(dirt.x, dirt.y - 1, dirt.z);
            view.World.Set(stone.x, stone.y, stone.z, Cell.Make(BlockMaterial.Stone));
            Assert.AreEqual(RainResult.NoEffect, farming.CastRain(stone));
            Assert.IsTrue(farming.RainCooldown.Ready);

            var f = FeetCell();
            Assert.AreEqual(RainResult.NoEffect, farming.CastRain(new Vector3Int(f.X + 12, f.Y, f.Z)));
            Assert.AreEqual("Mưa: ngoài tầm 8 ô", farming.StatusLine);
            Assert.IsTrue(farming.RainCooldown.Ready);

            yield return WaitSeconds(1f, () => !farming.RainBox.activeSelf);
            Assert.IsFalse(farming.RainBox.activeSelf);
        }

        [UnityTest]
        public IEnumerator HarvestIntoBag()
        {
            int id = PlotToPhase(PlotPhase.Harvest, out var cell);
            Assert.AreEqual(id, farming.RipePlotAt(cell));
            int p0 = view.Bag.GetProduce(0), s0 = view.Bag.GetSeeds(0);
            var cd = sv.Sim.Config.crops[0];
            Assert.IsTrue(farming.Harvest(id));
            Assert.AreEqual(p0 + cd.producePerHarvest, view.Bag.GetProduce(0));
            Assert.AreEqual(s0 + cd.seedsPerHarvest, view.Bag.GetSeeds(0));
            StringAssert.Contains("Nông sản Cây A: " + view.Bag.GetProduce(0), input.HudText());
            StringAssert.Contains("Thu hoạch Cây A", farming.StatusLine);

            // ô chín ngoài tầm → -1: dịch nhân vật ra xa
            int id2 = PlotToPhase(PlotPhase.Harvest, out var cell2);
            Assert.AreEqual(id2, farming.RipePlotAt(cell2));
            player.Place(view.transform.TransformPoint(new Vector3(cell2.x + 12.5f, cell2.y + 1f, cell2.z + 0.5f)), 0f);
            Assert.AreEqual(-1, farming.RipePlotAt(cell2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudInitial()
        {
            var t = input.HudText();
            StringAssert.Contains("Mưa (R): sẵn sàng", t);
            ToTill();
            StringAssert.Contains("lên biển = tự đi tới xới", input.HudText());
            yield return null;
        }
    }
}
