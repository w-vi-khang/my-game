using System;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class PlayerMotionTests
    {
        [TearDown]
        public void TearDown() => PlayerMotion.Configure(new PlayerConfig());

        static WorldData Flat()
        {
            var w = new WorldData(new WorldSize(32, 16, 32));
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
                w.Set(x, 0, z, Cell.Make(BlockMaterial.Stone));
            return w;
        }

        static void Solid(WorldData w, int x, int y, int z) => w.Set(x, y, z, Cell.Make(BlockMaterial.Stone));

        [Test]
        public void ConfigureConvertsMs()
        {
            PlayerMotion.Configure(new PlayerConfig { dashMs = 200, dashCooldownMs = 4000, stuckMs = 500, clickMarkMs = 700, dashCells = 5f, cameraPitch = 40f, cameraDistance = 10f, firstPersonPitch = 0f, observationHeight = 3f });
            Assert.AreEqual(0.2f, PlayerMotion.DashSeconds, 1e-5f);
            Assert.AreEqual(4f, PlayerMotion.DashCooldownSeconds, 1e-5f);
            Assert.AreEqual(0.5f, PlayerMotion.StuckSeconds, 1e-5f);
            Assert.AreEqual(0.7f, PlayerMotion.ClickMarkSeconds, 1e-5f);
            Assert.AreEqual(5f, PlayerMotion.DashCells);
            Assert.AreEqual(40f, PlayerMotion.CameraPitch);
            Assert.AreEqual(10f, PlayerMotion.CameraDistance);
            Assert.AreEqual(0f, PlayerMotion.FirstPersonPitch);
            Assert.AreEqual(3f, PlayerMotion.ObservationHeight);
            Assert.Throws<ArgumentNullException>(() => PlayerMotion.Configure(null));
            PlayerMotion.Configure(new PlayerConfig());
            Assert.AreEqual(2f, PlayerMotion.ObservationHeight);
            Assert.AreEqual(0.15f, PlayerMotion.DashSeconds, 1e-5f);
            Assert.AreEqual(3f, PlayerMotion.DashCooldownSeconds, 1e-5f);
        }

        [Test]
        public void ShouldStepUp()
        {
            var w = Flat();
            Assert.IsFalse(PlayerMotion.ShouldStepUp(w, 5.5f, 1f, 5.5f, 0f, 1f));
            Solid(w, 5, 1, 6);
            Assert.IsTrue(PlayerMotion.ShouldStepUp(w, 5.5f, 1f, 5.5f, 0f, 1f));
            Solid(w, 5, 2, 6);
            Assert.IsFalse(PlayerMotion.ShouldStepUp(w, 5.5f, 1f, 5.5f, 0f, 1f));
            w.Set(5, 2, 6, Cell.Make(BlockMaterial.Empty));
            Solid(w, 5, 3, 6);
            Assert.IsFalse(PlayerMotion.ShouldStepUp(w, 5.5f, 1f, 5.5f, 0f, 1f));
            Assert.IsFalse(PlayerMotion.ShouldStepUp(w, 5.5f, 1f, 5.5f, 0f, 0f));
            Assert.Throws<ArgumentNullException>(() => PlayerMotion.ShouldStepUp(null, 5.5f, 1f, 5.5f, 0f, 1f));
        }

        [Test]
        public void StuckTimer()
        {
            var t = new StuckTimer(0.3f);
            Assert.IsFalse(t.Update(0.2f, false));
            Assert.IsTrue(t.Update(0.2f, false));
            Assert.IsFalse(t.Update(0.2f, true));
            Assert.AreEqual(0f, t.Elapsed);
            Assert.Throws<ArgumentOutOfRangeException>(() => new StuckTimer(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Update(-1f, false));
        }

        [Test]
        public void DashState()
        {
            var d = new DashState(4f, 0.15f, 3f);
            Assert.IsTrue(d.TryStart(true));
            Assert.IsTrue(d.Active);
            Assert.AreEqual(1.3333f, d.Step(0.05f), 1e-3f);
            Assert.AreEqual(1.3333f, d.Step(0.05f), 1e-3f);
            Assert.AreEqual(1.3333f, d.Step(0.1f), 1e-3f);
            Assert.IsFalse(d.Active);
            Assert.IsFalse(d.TryStart(true));
            d.Tick(2.5f, true);
            Assert.IsFalse(d.TryStart(true));
            d.Tick(0.5f, true);
            Assert.IsTrue(d.TryStart(true));

            var a = new DashState(4f, 0.15f, 3f);
            Assert.IsTrue(a.TryStart(false));
            Assert.IsTrue(a.AirUsed);
            a.Stop();
            a.Tick(3f, false);
            Assert.IsFalse(a.TryStart(false));
            a.Tick(0f, true);
            Assert.IsTrue(a.TryStart(false));

            var n = new DashState(4f, 0.15f, 3f);
            Assert.AreEqual(0f, n.Step(0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DashState(0f, 0.15f, 3f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DashState(4f, 0f, 3f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DashState(4f, 0.15f, 0f));
        }
    }
}
