using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class PlayerRulesTests
    {
        [Test]
        public void InToolReach_BoundaryExactly8()
        {
            Assert.IsTrue(PlayerRules.InToolReach(0.5f, -0.5f, 0.5f, 8, 0, 0));
            Assert.IsFalse(PlayerRules.InToolReach(0.5f, -0.5f, 0.5f, 9, 0, 0));
            Assert.IsTrue(PlayerRules.InToolReach(0.5f, -0.5f, 0.5f, 0, 8, 0));
            Assert.IsTrue(PlayerRules.InToolReach(0.5f, -0.5f, 0.5f, 0, 0, -8));
        }

        [Test]
        public void InToolReach_8Point1Outside()
        {
            Assert.IsFalse(PlayerRules.InToolReach(0.4f, -0.5f, 0.5f, 8, 0, 0));
        }

        [Test]
        public void InHandReach_BoundaryExactly8()
        {
            Assert.IsTrue(PlayerRules.InHandReach(0.5f, -0.5f, 0.5f, 8, 0, 0));
            Assert.IsFalse(PlayerRules.InHandReach(0.5f, -0.5f, 0.5f, 9, 0, 0));
            Assert.IsTrue(PlayerRules.InHandReach(0.5f, -0.5f, 0.5f, 0, 8, 0));
            Assert.IsTrue(PlayerRules.InHandReach(0.5f, -0.5f, 0.5f, 0, 0, -8));
        }

        [Test]
        public void InHandReach_8Point1Outside()
        {
            Assert.IsFalse(PlayerRules.InHandReach(0.4f, -0.5f, 0.5f, 8, 0, 0));
        }

        [Test]
        public void Occupies_TwoCellsWhenCentered()
        {
            Assert.IsTrue(PlayerRules.Occupies(5.5f, 3f, 5.5f, 5, 3, 5));
            Assert.IsTrue(PlayerRules.Occupies(5.5f, 3f, 5.5f, 5, 4, 5));
            Assert.IsFalse(PlayerRules.Occupies(5.5f, 3f, 5.5f, 5, 2, 5));
            Assert.IsFalse(PlayerRules.Occupies(5.5f, 3f, 5.5f, 5, 5, 5));
            Assert.IsFalse(PlayerRules.Occupies(5.5f, 3f, 5.5f, 6, 3, 5));
            Assert.IsFalse(PlayerRules.Occupies(5.5f, 3f, 5.5f, 4, 3, 5));
        }

        [Test]
        public void Occupies_StraddlingColumns()
        {
            Assert.IsTrue(PlayerRules.Occupies(5.95f, 3f, 5.5f, 5, 3, 5));
            Assert.IsTrue(PlayerRules.Occupies(5.95f, 3f, 5.5f, 6, 3, 5));
            Assert.IsFalse(PlayerRules.Occupies(5.5f, 3f, 5.5f, 6, 3, 5));
        }

        [Test]
        public void ClampToWorld()
        {
            var s = new WorldSize(32, 16, 32);
            float dx = -1f, dz = 0.05f;
            PlayerRules.ClampToWorld(s, 0.5f, 10f, ref dx, ref dz);
            Assert.AreEqual(-0.1f, dx, 1e-5f);
            Assert.AreEqual(0.05f, dz, 1e-6f);
            dx = 1f; dz = -1f;
            PlayerRules.ClampToWorld(s, 31.5f, 0.5f, ref dx, ref dz);
            Assert.AreEqual(0.1f, dx, 1e-5f);
            Assert.AreEqual(-0.1f, dz, 1e-5f);
            dx = 0.05f; dz = 1f;
            PlayerRules.ClampToWorld(s, 16f, 31.5f, ref dx, ref dz);
            Assert.AreEqual(0.05f, dx, 1e-6f);
            Assert.AreEqual(0.1f, dz, 1e-5f);
        }

        [Test]
        public void InWater()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(2, 1, 2, Cell.Make(BlockMaterial.Water, 7));
            Assert.IsTrue(PlayerRules.InWater(w, 2.5f, 1f, 2.5f));
            Assert.IsFalse(PlayerRules.InWater(w, 2.5f, 2f, 2.5f));
            Assert.IsFalse(PlayerRules.InWater(w, -1f, 1f, 2.5f));
        }
    }
}
