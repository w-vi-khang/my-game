using System.Collections;
using Game.Core;
using Game.Core.Sim;
using Game.Core.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7l e2 (brief 0065) — icon + vòng tiến trình trên ô ruộng, icon sinh vật, ẩn khi Cài đặt mở / xa quá 30 ô.</summary>
    public class WorldIconsPlayTests
    {
        GameObject camGo, go;
        TerrainChunkView view;
        SimView sv;
        WorldIconsView icons;
        float savedTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            savedTimeScale = Time.timeScale;
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            sv = go.AddComponent<SimView>();
            yield return null;
            yield return null;
            Assert.IsNotNull(sv.Sim);
            icons = go.GetComponent<WorldIconsView>();
            Assert.IsNotNull(icons);
            AimCamera(15f);
            for (int i = 0; i < 5 && !icons.IsReady; i++) yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = savedTimeScale;
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        void AimCamera(float height)
        {
            var s = sv.Sim.State.Plots[0].Soil;
            var target = go.transform.TransformPoint(new Vector3(s.X + 0.5f, s.Y + 1f, s.Z + 0.5f));
            camGo.transform.position = target + new Vector3(0f, height, -10f);
            camGo.transform.LookAt(target);
        }

        [UnityTest]
        public IEnumerator BuildsOneItemPerPlotAndCreature()
        {
            Assert.IsTrue(icons.IsReady);
            Assert.AreEqual(sv.Sim.State.Plots.Count, icons.PlotItemCount);
            Assert.AreEqual(sv.Sim.State.Creatures.Count, icons.CreatureItemCount);
            yield return null;
        }

        void AssertPlotsMatch()
        {
            var st = sv.Sim.State;
            for (int i = 0; i < st.Plots.Count; i++)
            {
                var expect = WorldIcons.ForPlot(st.Plots[i], st, sv.Sim.Config);
                if (expect.Kind == WorldIconKind.None) continue;
                Assert.AreEqual(expect, icons.PlotIcon(i), "plot " + i);
                if (expect.Percent >= 0)
                    Assert.AreEqual(expect.Percent / 100f, icons.PlotRingFill(i), 0.011f, "ring " + i);
                var shape = expect.Kind switch
                {
                    WorldIconKind.Till => TimeDialShape.Hoe,
                    WorldIconKind.Sow => TimeDialShape.Seed,
                    WorldIconKind.Water => TimeDialShape.Drop,
                    WorldIconKind.Grow => TimeDialShape.Sprout,
                    WorldIconKind.Ripe => TimeDialShape.Disk,
                    _ => TimeDialShape.Dots,
                };
                Assert.AreEqual(shape, icons.PlotShape(i), "shape " + i);
            }
        }

        [UnityTest]
        public IEnumerator PlotIconMatchesStageAndProgress()
        {
            Time.timeScale = 0f; // SimView không tự chạy nhịp giữa lúc icon cập nhật (LateUpdate) và lúc test đọc State — brief 0068
            yield return null;
            AssertPlotsMatch();
            var before = new WorldIcon[sv.Sim.State.Plots.Count];
            for (int i = 0; i < before.Length; i++) before[i] = icons.PlotIcon(i);
            for (int i = 0; i < 3000; i++) sv.Sim.Tick();
            yield return null;
            AssertPlotsMatch();
            bool changed = false;
            for (int i = 0; i < before.Length; i++) changed |= before[i] != icons.PlotIcon(i);
            Assert.IsTrue(changed, "no plot icon changed after 3000 ticks");
        }

        [UnityTest]
        public IEnumerator CreatureIconMatchesState()
        {
            Time.timeScale = 0f; // SimView không tự chạy nhịp giữa lúc icon cập nhật (LateUpdate) và lúc test đọc State — brief 0068
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < 500; i++) sv.Sim.Tick();
                yield return null;
                var st = sv.Sim.State;
                for (int i = 0; i < st.Creatures.Count; i++)
                {
                    var expect = WorldIcons.ForCreature(st, st.Creatures[i]);
                    if (expect.Kind == WorldIconKind.None) continue;
                    Assert.AreEqual(expect, icons.CreatureIcon(i), "creature " + i);
                }
            }
        }

        [UnityTest]
        public IEnumerator HiddenWhileSettingsOpen()
        {
            var settings = go.GetComponent<SettingsView>();
            Assert.IsTrue(icons.CanvasActive);
            settings.SetOpen(true);
            yield return null;
            Assert.IsFalse(icons.CanvasActive);
            settings.SetOpen(false);
            yield return null;
            Assert.IsTrue(icons.CanvasActive);
        }

        [UnityTest]
        public IEnumerator FarIconsHidden()
        {
            AimCamera(60f);
            yield return null;
            Assert.AreEqual(0, icons.VisiblePlotIcons);
        }
    }
}
