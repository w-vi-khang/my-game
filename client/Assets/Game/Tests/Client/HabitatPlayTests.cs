using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Sim 8b (brief 0074): môi trường sống trong scene SimView trên world nhiều map.</summary>
    public class HabitatPlayTests
    {
        GameObject camGo, go;
        TerrainChunkView view;
        SimView sv;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.transform.SetPositionAndRotation(new Vector3(192, 40, 150), Quaternion.Euler(25f, 0f, 0f));
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = camGo.transform;
            sv = go.AddComponent<SimView>();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        void PondOrigin(out int wx0, out int wz0)
        {
            var plain = sv.Plain.Value;
            wx0 = plain.X0 + plain.Size / 2 + SimDemo.PlainPondMin.X;
            wz0 = plain.Z0 + plain.Size / 2 + SimDemo.PlainPondMin.Z;
        }

        [UnityTest]
        public IEnumerator StartsWithL1PondAndPanel()
        {
            Assert.IsNotNull(sv.Habitat);
            Assert.AreEqual(1, sv.Sim.State.Creatures.Count);
            Assert.AreEqual("L1", sv.Sim.State.Creatures[0].SpeciesId);
            Assert.IsTrue(sv.Habitat.IsUnlocked(0));
            Assert.IsFalse(sv.Habitat.IsUnlocked(1));
            Assert.IsFalse(sv.Habitat.IsUnlocked(2));
            PondOrigin(out int wx0, out int wz0);
            Assert.AreEqual(BlockMaterial.Water, view.World.GetMaterial(wx0 + 1, sv.Plain.Value.Ground, wz0 + 1));
            var panel = go.GetComponent<HabitatPanelView>();
            Assert.IsNotNull(panel);
            Assert.IsFalse(panel.IsOpen);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DigUpdatesRowSameFrame()
        {
            var panel = go.GetComponent<HabitatPanelView>();
            string before = panel.RowText(0, 0);
            var plain = sv.Plain.Value;
            int x = plain.X0 + 3, z = plain.Z0 + 3;
            Assert.AreEqual(EditResult.Ok, view.Editor.TryDig(x, plain.Ground, z));
            string after = panel.RowText(0, 0);
            Assert.AreNotEqual(before, after);
            int Cur(string t) => int.Parse(t.Split(':')[1].Trim().Split('/')[0]);
            Assert.AreEqual(Cur(before) - 1, Cur(after), before + " -> " + after);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpeciesReturnsInScene()
        {
            PondOrigin(out int wx0, out int wz0);
            int y = sv.Plain.Value.Ground;
            view.Editor.Bag.Add(BlockMaterial.Grass, 10);
            var cells = new System.Collections.Generic.List<(int, int)>();
            for (int i = 0; i < 4; i++) { cells.Add((wx0 - 1, wz0 + i)); cells.Add((wx0 + 4, wz0 + i)); }
            cells.Add((wx0, wz0 - 1));
            cells.Add((wx0 + 1, wz0 - 1));
            Assert.AreEqual(10, cells.Count);
            foreach (var (x, z) in cells)
            {
                Assert.AreEqual(EditResult.Ok, view.Editor.TryDig(x, y, z));
                Assert.AreEqual(EditResult.Ok, view.Editor.TryPlace(x, y, z, BlockMaterial.Grass));
            }
            for (int f = 0; f < 30 && sv.ReturnedSpecies.Count == 0; f++) yield return null;
            CollectionAssert.Contains(sv.ReturnedSpecies, "L2");
            Assert.AreEqual(3, sv.Sim.State.Creatures.Count);
            yield return null;
            Assert.AreEqual(3, sv.BodyCount);
            StringAssert.Contains("L2", go.GetComponent<HabitatPanelView>().Notice);
        }

        [UnityTest]
        public IEnumerator ToggleKeyAndEscClose()
        {
            var panel = go.GetComponent<HabitatPanelView>();
            panel.SetOpen(true);
            Assert.IsTrue(HabitatPanelView.IsAnyOpen);
            var settings = go.GetComponent<SettingsView>();
            Assert.IsTrue(settings.OtherPanelOpen());
            settings.HandleKeys(false, true); // Esc khi bảng môi trường sống mở: không mở Cài đặt
            Assert.IsFalse(settings.IsOpen);
            panel.SetOpen(false);
            Assert.IsFalse(HabitatPanelView.IsAnyOpen);
            Assert.IsFalse(settings.OtherPanelOpen());
            yield return null;
        }
    }
}
