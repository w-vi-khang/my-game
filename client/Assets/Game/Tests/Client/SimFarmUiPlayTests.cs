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
            Assert.Fail("Could not find a marked cell near the camp");
            return default;
        }

        void ToTill()
        {
            input.ToggleMode(ControlMode.Till);
            Assert.AreEqual(ControlMode.Till, input.Control.Mode);
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
        public IEnumerator TillModeOnlyWithSim()
        {
            Assert.IsTrue(input.TillAvailable);
            input.ToggleMode(ControlMode.Till);
            Assert.AreEqual(ControlMode.Till, input.Control.Mode);
            input.ToggleMode(ControlMode.Till);
            Assert.AreEqual(ControlMode.Default, input.Control.Mode);
            yield break;
        }

        [UnityTest]
        public IEnumerator MarkShowsSignSameFrame()
        {
            ToTill();
            var cell = FindMarkable();
            input.RightClickAt(true, cell, cell);
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
            input.RightClickAt(true, cell, cell);
            int id = sv.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            TickUntilTilled(id);
            yield return null;
            Assert.IsFalse(sv.SignVisible(id));
            var p = sv.Sim.State.Plots[id];
            Assert.AreEqual(0, p.Slots.Count);
            Assert.AreEqual(PlotPhase.Idle, p.Phase);
        }

        [UnityTest]
        public IEnumerator UnmarkAndDigHideSign()
        {
            var a = FindMarkable();
            var ca = new CellPos(a.x, a.y, a.z);
            Assert.AreEqual(TillResult.Marked, sv.UseTill(a));
            int idA = sv.Sim.PlotAt(ca);
            Assert.AreEqual(TillResult.SelfTill, sv.UseTill(a));
            Assert.IsTrue(sv.UnmarkAt(a));
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
        public IEnumerator ShowAimTillHighlight()
        {
            ToTill();
            var cell = FindMarkable();
            input.ShowAim(input.Preview(true, cell, cell), true, cell, cell);
            Assert.IsTrue(input.HighlightVisible);
            Assert.IsFalse(input.GhostVisible);
            input.ShowAim(input.Preview(true, Vector3Int.zero, Vector3Int.zero), true, Vector3Int.zero, Vector3Int.zero);
            Assert.IsFalse(input.HighlightVisible);
            yield break;
        }

        [UnityTest]
        public IEnumerator PlotPanelSlots()
        {
            OpenPlot0();
            var p = sv.Sim.State.Plots[0];
            Assert.AreEqual(0, p.Slots.Count);
            Assert.AreEqual(0, p.CurrentCrop);
            Assert.IsTrue(sv.ChooseCrop(1));
            Assert.IsTrue(sv.ChooseCrop(1));
            Assert.AreEqual(new[] { 1, 2 }, Flat(p));
            Assert.IsTrue(sv.ChooseCrop(0));
            Assert.AreEqual(new[] { 1, 2, 0, 1 }, Flat(p));
            Assert.IsFalse(sv.CanChooseCrop(1));
            Assert.IsFalse(sv.ChooseCrop(1));
            Assert.IsTrue(sv.ChooseCrop(0));
            Assert.AreEqual(new[] { 1, 2, 0, 2 }, Flat(p));
            Assert.IsTrue(sv.IncreaseQueued(0));
            Assert.AreEqual(3, p.Slots[0].Count);
            Assert.IsFalse(sv.IncreaseQueued(0));
            Assert.IsTrue(sv.DecreaseQueued(1));
            Assert.IsTrue(sv.DecreaseQueued(1));
            Assert.AreEqual(new[] { 1, 3 }, Flat(p));
            Assert.IsTrue(sv.ClearQueued(0));
            Assert.AreEqual(0, p.Slots.Count);
            sv.ClosePlotPanel();
            Assert.AreEqual(-1, sv.OpenPlotId);
            Assert.IsFalse(sv.ChooseCrop(1));
            yield break;
        }

        static int[] Flat(FarmPlot p)
        {
            var l = new System.Collections.Generic.List<int>();
            foreach (var sl in p.Slots) { l.Add(sl.CropId); l.Add(sl.Count); }
            return l.ToArray();
        }

        [UnityTest]
        public IEnumerator ProgressPauseResumeAndCloseResumes()
        {
            OpenPlot0();
            var p = sv.Sim.State.Plots[0];
            for (int i = 0; i < 3000 && p.Phase != PlotPhase.Water && p.Phase != PlotPhase.Growing; i++) sv.Sim.Tick();
            Assert.IsTrue(p.Phase == PlotPhase.Water || p.Phase == PlotPhase.Growing);
            Assert.IsTrue(sv.ProgressAction());
            Assert.IsTrue(sv.ChooseCrop(0));
            Assert.AreEqual(PlotPhase.Till, p.Phase);
            Assert.IsTrue(sv.ProgressAction());
            Assert.IsTrue(p.TillPaused);
            for (int i = 0; i < 20; i++) sv.Sim.Tick();
            Assert.AreNotEqual(StageStatus.Assigned, sv.Sim.State.Stages[p.StageId].Status);
            Assert.IsTrue(sv.ProgressAction());
            Assert.IsFalse(p.TillPaused);
            Assert.IsTrue(sv.ProgressAction());
            Assert.IsTrue(p.TillPaused);
            sv.ClosePlotPanel();
            Assert.IsFalse(p.TillPaused);
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
            Assert.IsTrue(sv.ProgressAction());
            Assert.AreEqual(0, sv.Sim.State.Plots[0].Slots.Count);
            Assert.AreEqual(-1, sv.Sim.State.Plots[0].CurrentCrop);
            long t0 = sv.Sim.State.Tick;
            for (int i = 0; i < 300 && sv.Sim.State.Tick < t0 + 3; i++) yield return null;
            Assert.GreaterOrEqual(sv.Sim.State.Tick, t0 + 3);
            Assert.AreEqual(0, sv.Sim.State.Plots[0].Slots.Count);
            Assert.AreEqual(-1, sv.Sim.State.Plots[0].CurrentCrop);
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
