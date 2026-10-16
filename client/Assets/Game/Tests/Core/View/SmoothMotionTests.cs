using System;
using System.Collections.Generic;
using Game.Core.Sim;
using Game.Core.View;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Sim bước 6 — "Xong khi": đi mượt ở 30 / 60 FPS và khung lệch (mỗi khung dời đúng 1,5 ô/giây × dt, không khung đứng yên).</summary>
    public class SmoothMotionTests
    {
        static void RunStraightWalk(double[] frameTimes)
        {
            var sim = new Simulation(new SimConfig(), TestWorlds.Flat(0));
            sim.AddCreature("L1", new CellPos(1, 1, 1));
            sim.AddStage(sim.AddOrder(new List<CellPos>()), WorkAbility.Till, new CellPos(13, 1, 1));
            TestWorlds.HomeAll(sim);
            var clock = new SimClock(10);
            var interp = new SimInterpolator();
            interp.Capture(sim.State);
            interp.Sample(0, clock.Alpha, out double px, out _, out _);
            int ticks = 0, window = 0;
            for (int f = 0; ticks < 100; f++)
            {
                double dt = frameTimes[f % frameTimes.Length];
                int before = ticks;
                int n = clock.Advance(dt);
                for (int i = 0; i < n; i++)
                {
                    sim.Tick();
                    interp.Capture(sim.State);
                    interp.ApplyEvents(sim.State.Events);
                    sim.State.Events.Clear();
                    ticks++;
                }
                interp.Sample(0, clock.Alpha, out double x, out double y, out double z);
                Assert.AreEqual(1.0, y, 1e-9, "khung " + f);
                Assert.AreEqual(1.0, z, 1e-9, "khung " + f);
                if (before >= 2 && ticks <= 80)
                {
                    Assert.AreEqual(1.5e-6 * Math.Round(dt * 1e6), x - px, 1e-9, "khung " + f);
                    Assert.Greater(x - px, 0, "khung " + f);
                    window++;
                }
                px = x;
            }
            Assert.GreaterOrEqual(window, 200);
            Assert.AreEqual(13.0, px, 1e-9);
            var c = sim.State.Creatures[0];
            Assert.AreEqual(new CellPos(13, 1, 1), c.Cell);
            Assert.AreEqual(CreatureState.Working, c.State);
        }

        [Test] public void Smooth30Fps() => RunStraightWalk(new[] { 1.0 / 30 });
        [Test] public void Smooth60Fps() => RunStraightWalk(new[] { 1.0 / 60 });
        [Test] public void SmoothUnevenFrames() => RunStraightWalk(new[] { 1.0 / 30, 1.0 / 60, 1.0 / 45, 1.0 / 90, 1.0 / 24 });
    }
}
