using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class PlayerRulesTests
    {
        [TearDown]
        public void TearDown() => PlayerRules.Configure(new Game.Core.Sim.PlayerConfig());

        [Test]
        public void Configure_ChangesReach()
        {
            PlayerRules.Configure(new Game.Core.Sim.PlayerConfig { targetReach = 12, collectReach = 3 });
            Assert.IsTrue(PlayerRules.InTargetReach(0.5f, -0.5f, 0.5f, 12, 0, 0));
            Assert.IsFalse(PlayerRules.InTargetReach(0.5f, -0.5f, 0.5f, 13, 0, 0));
            Assert.IsTrue(PlayerRules.InCollectReach(0.5f, -0.5f, 0.5f, 3, 0, 0));
            Assert.IsFalse(PlayerRules.InCollectReach(0.5f, -0.5f, 0.5f, 4, 0, 0));
            Assert.Throws<System.ArgumentNullException>(() => PlayerRules.Configure(null));
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

        [Test]
        public void HeadUnderwater()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int x = 0; x < 16; x++)
                for (int z = 0; z < 16; z++)
                    w.Set(x, 0, z, Cell.Make(BlockMaterial.Stone));
            for (int y = 1; y <= 3; y++) w.Set(2, y, 2, Cell.Make(BlockMaterial.Water, 7));
            w.Set(8, 1, 8, Cell.Make(BlockMaterial.Water, 7));
            Assert.IsTrue(PlayerRules.HeadUnderwater(w, 2.5f, 1f, 2.5f, 0.1f)); // đầu ở y = 2,5, trong cột nước 3 ô
            Assert.IsFalse(PlayerRules.HeadUnderwater(w, 8.5f, 1f, 8.5f, 0.1f)); // chỉ 1 ô nước, đầu ở y = 2,5 trên mặt nước
            Assert.IsFalse(PlayerRules.HeadUnderwater(w, 2.5f, 2.5f, 2.5f, 0.1f)); // đầu ở y = 4,0 ngoài nước
        }

        [Test]
        public void NewReaches_Boundary()
        {
            Assert.IsTrue(PlayerRules.InTargetReach(0.5f, -0.5f, 0.5f, 10, 0, 0));
            Assert.IsFalse(PlayerRules.InTargetReach(0.5f, -0.5f, 0.5f, 11, 0, 0));
            Assert.IsTrue(PlayerRules.InActReach(0.5f, -0.5f, 0.5f, 2, 0, 0));
            Assert.IsFalse(PlayerRules.InActReach(0.5f, -0.5f, 0.5f, 3, 0, 0));
            Assert.IsTrue(PlayerRules.InActReach(0.5f, -0.5f, 0.5f, 1, 0, 1));
            Assert.IsFalse(PlayerRules.InActReach(0.5f, -0.5f, 0.5f, 2, 0, 1));
            Assert.IsTrue(PlayerRules.InDigReach(0.5f, -0.5f, 0.5f, 2, 0, 0));
            Assert.IsFalse(PlayerRules.InDigReach(0.5f, -0.5f, 0.5f, 3, 0, 0));
            Assert.IsTrue(PlayerRules.InDigReach(0.5f, -0.5f, 0.5f, 1, 0, 1));
            Assert.IsFalse(PlayerRules.InDigReach(0.5f, -0.5f, 0.5f, 2, 0, 1));
            Assert.IsTrue(PlayerRules.InCollectReach(0.5f, -0.5f, 0.5f, 2, 0, 0));
            Assert.IsFalse(PlayerRules.InCollectReach(0.5f, -0.5f, 0.5f, 3, 0, 0));
            Assert.IsTrue(PlayerRules.InCollectReach(0.5f, -0.5f, 0.5f, 1, 0, 1));
            Assert.IsFalse(PlayerRules.InCollectReach(0.5f, -0.5f, 0.5f, 2, 0, 1));
        }

        [Test]
        public void Configure_ChangesNewReaches()
        {
            PlayerRules.Configure(new Game.Core.Sim.PlayerConfig { targetReach = 6, actReach = 3 });
            Assert.IsTrue(PlayerRules.InTargetReach(0.5f, -0.5f, 0.5f, 6, 0, 0));
            Assert.IsFalse(PlayerRules.InTargetReach(0.5f, -0.5f, 0.5f, 7, 0, 0));
            Assert.IsTrue(PlayerRules.InActReach(0.5f, -0.5f, 0.5f, 3, 0, 0));
            Assert.IsFalse(PlayerRules.InActReach(0.5f, -0.5f, 0.5f, 4, 0, 0));
        }
    }
}
