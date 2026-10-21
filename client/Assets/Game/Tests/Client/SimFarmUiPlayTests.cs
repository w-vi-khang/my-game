using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Sim bước 7c (brief 0030) — công cụ Xới, biển canh tác, bảng ô ruộng, nút Kho.</summary>
    public class SimFarmUiPlayTests
    {
        GameObject go;
        TerrainChunkView view;
        SimView sv;
        TerrainEditInput input;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            sv = go.AddComponent<SimView>();
            input = go.AddComponent<TerrainEditInput>();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(go);
            yield return null;
        }

        Vector3Int FindMarkable()
        {
            var s0 = sv.Sim.State.Plots[0].Soil;
            for (int dz = -8; dz <= 8; dz++)
                for (int dx = -8; dx <= 8; dx++)
                {
                    int x = s0.X + dx, z = s0.Z + dz;
                    if (!view.World.Size.InBounds(x, 0, z)) continue;
                    int h = view.World.SurfaceHeight(x, z);
                    if (sv.Sim.CanMarkTill(new CellPos(x, h, z))) return new Vector3Int(x, h, z);
                }
            Assert.Fail("Không tìm được ô đánh dấu được quanh trại");
            return default;
        }

        void ToTill()
        {
            input.ToggleTool();
            input.ToggleTool();
            Assert.AreEqual(EditTool.Till, input.Tool);
        }

        void TickUntilTilled(int id)
        {
            for (int i = 0; i < 3000 && !sv.Sim.State.Plots[id].Tilled; i++) sv.Sim.Tick();
            Assert.IsTrue(sv.Sim.State.Plots[id].Tilled);
        }

        void OpenPlot0()
        {
            TickUntilTilled(0);
            var soil = sv.Sim.State.Plots[0].Soil;
            Assert.AreEqual(TillResult.OpenedPlot, sv.UseTill(new Vector3Int(soil.X, soil.Y, soil.Z)));
            Assert.AreEqual(0, sv.OpenPlotId);
        }

        static Vector2 ToScreen(Rect r)
        {
            float s = Mathf.Max(1, Screen.height / 720f);
            return new Vector2(r.center.x * s, Screen.height - r.center.y * s);
        }

        [UnityTest]
        public IEnumerator TabCyclesThreeToolsWithSim()
        {
            Assert.IsTrue(input.TillAvailable);
            input.ToggleTool();
            Assert.AreEqual(EditTool.Place, input.Tool);
            input.ToggleTool();
            Assert.AreEqual(EditTool.Till, input.Tool);
            input.ToggleTool();
            Assert.AreEqual(EditTool.Dig, input.Tool);
            yield break;
        }

        [UnityTest]
        public IEnumerator MarkShowsSignSameFrame()
        {
            ToTill();
            var cell = FindMarkable();
            input.UseTool(cell, cell);
            Assert.AreEqual(TillResult.Marked, input.LastTill);
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            Assert.GreaterOrEqual(id, 4);
            Assert.Greater(sv.SignCount, id);
            Assert.IsTrue(sv.SignVisible(id));
            yield break;
        }

        [UnityTest]
        public IEnumerator SignHidesWhenTilled()
        {
            ToTill();
            var cell = FindMarkable();
            input.UseTool(cell, cell);
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            TickUntilTilled(id);
            yield return null;
            Assert.IsFalse(sv.SignVisible(id));
            var p = sv.Sim.State.Plots[id];
            Assert.AreEqual(0, p.Queue.Count);
            Assert.AreEqual(PlotPhase.Idle, p.Phase);
        }

        [UnityTest]
        public IEnumerator UnmarkAndDigHideSign()
        {
            var a = FindMarkable();
            var ca = new CellPos(a.x, a.y, a.z);
            Assert.AreEqual(TillResult.Marked, sv.UseTill(a));
            int idA = sv.Sim.PlotAt(ca);
            Assert.AreEqual(TillResult.Unmarked, sv.UseTill(a));
            Assert.IsFalse(sv.SignVisible(idA));
            Assert.IsTrue(sv.Sim.State.Plots[idA].Removed);

            Assert.AreEqual(TillResult.Marked, sv.UseTill(a));
            int idB = sv.Sim.PlotAt(ca);
            Assert.AreNotEqual(idA, idB);
            Assert.AreEqual(EditResult.Ok, view.Dig(a));
            sv.Sim.Tick();
            yield return null;
            Assert.IsTrue(sv.Sim.State.Plots[idB].Removed);
            Assert.IsFalse(sv.SignVisible(idB));
        }

        [UnityTest]
        public IEnumerator ShowAimTillOutline()
        {
            ToTill();
            var cell = FindMarkable();
            input.ShowAim(true, cell, cell);
            Assert.IsTrue(input.OutlineVisible);
            Assert.IsFalse(input.GhostVisible);
            input.ShowAim(true, Vector3Int.zero, Vector3Int.zero);
            Assert.IsFalse(input.OutlineVisible);
            yield break;
        }

        [UnityTest]
        public IEnumerator PlotPanelChooseAndRemove()
        {
            OpenPlot0();
            var q = sv.Sim.State.Plots[0].Queue;
            CollectionAssert.AreEqual(new[] { 0 }, q);
            Assert.IsTrue(sv.ChooseCrop(1));
            CollectionAssert.AreEqual(new[] { 0, 1 }, q);
            Assert.IsFalse(sv.ChooseCrop(0));
            Assert.IsTrue(sv.RemoveQueued(1));
            CollectionAssert.AreEqual(new[] { 0 }, q);
            sv.ClosePlotPanel();
            Assert.AreEqual(-1, sv.OpenPlotId);
            Assert.IsFalse(sv.ChooseCrop(1));
            yield break;
        }

        [UnityTest]
        public IEnumerator PlotPanelClosesWhenDug()
        {
            OpenPlot0();
            var soil = sv.Sim.State.Plots[0].Soil;
            Assert.AreEqual(EditResult.Ok, view.Dig(new Vector3Int(soil.X, soil.Y, soil.Z)));
            sv.Sim.Tick();
            yield return null;
            Assert.AreEqual(-1, sv.OpenPlotId);
        }

        [UnityTest]
        public IEnumerator DemoDoesNotRefillQueue()
        {
            OpenPlot0();
            Assert.IsTrue(sv.RemoveQueued(0));
            Assert.AreEqual(0, sv.Sim.State.Plots[0].Queue.Count);
            long t0 = sv.Sim.State.Tick;
            for (int i = 0; i < 300 && sv.Sim.State.Tick < t0 + 3; i++) yield return null;
            Assert.GreaterOrEqual(sv.Sim.State.Tick, t0 + 3);
            Assert.AreEqual(0, sv.Sim.State.Plots[0].Queue.Count);
        }

        [UnityTest]
        public IEnumerator StockToggleAndPointerBlock()
        {
            Assert.IsFalse(sv.StockOpen);
            sv.ToggleStock();
            Assert.IsTrue(sv.StockOpen);
            sv.ToggleStock();
            Assert.IsFalse(sv.StockOpen);

            float s = Mathf.Max(1, Screen.height / 720f);
            float gw = Screen.width / s, gh = Screen.height / s;
            Assert.IsTrue(SimView.BlocksPointer(ToScreen(SimView.StockButtonRect(gw, gh))));
            Assert.IsFalse(SimView.BlocksPointer(new Vector2(Screen.width / 2, Screen.height / 2)));
            OpenPlot0();
            Assert.IsTrue(SimView.BlocksPointer(ToScreen(SimView.PlotPanelRect(gw, gh))));
            yield break;
        }
    }
}
