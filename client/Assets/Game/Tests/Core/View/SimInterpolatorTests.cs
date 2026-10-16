using Game.Core.Sim;
using Game.Core.View;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SimInterpolatorTests
    {
        static SimState NewState(out Creature c)
        {
            var st = new SimState(new SimRng(1UL, 1UL));
            c = new Creature(0, "L1", WorkAbility.Till, WorkAbility.Sow, 100, 100, 150, 1, 1, 1, new CellPos(3, 1, 4));
            st.Creatures.Add(c);
            return st;
        }

        static void MovingTwoCaptures(SimState st, Creature c, SimInterpolator ip)
        {
            c.State = CreatureState.Moving;
            c.Path.Add(new CellPos(4, 1, 4));
            c.PathIndex = 0;
            c.MoveProgress = 300;
            ip.Capture(st);
            c.MoveProgress = 450;
            ip.Capture(st);
        }

        [Test]
        public void FirstCaptureHasNoMotion()
        {
            var st = NewState(out _);
            var ip = new SimInterpolator();
            ip.Capture(st);
            ip.Sample(0, 0.7, out double x, out double y, out double z);
            Assert.AreEqual(3, x, 1e-9); Assert.AreEqual(1, y, 1e-9); Assert.AreEqual(4, z, 1e-9);
            Assert.AreEqual(1, ip.Count);
            ip.Heading(0, out int hx, out int hz);
            Assert.AreEqual(0, hx); Assert.AreEqual(1, hz);
        }

        [Test]
        public void MovingInterpolatesProgress()
        {
            var st = NewState(out var c);
            var ip = new SimInterpolator();
            MovingTwoCaptures(st, c, ip);
            ip.Sample(0, 0.5, out double x, out double y, out double z);
            Assert.AreEqual(3.375, x, 1e-9); Assert.AreEqual(1, y, 1e-9); Assert.AreEqual(4, z, 1e-9);
            ip.Heading(0, out int hx, out int hz);
            Assert.AreEqual(150, hx); Assert.AreEqual(0, hz);
        }

        [Test]
        public void AlphaIsClamped()
        {
            var st = NewState(out var c);
            var ip = new SimInterpolator();
            MovingTwoCaptures(st, c, ip);
            ip.Sample(0, 1.5, out double x, out _, out _);
            Assert.AreEqual(3.45, x, 1e-9);
            ip.Sample(0, -1, out x, out _, out _);
            Assert.AreEqual(3.3, x, 1e-9);
        }

        [Test]
        public void PoseMilliClimb()
        {
            NewState(out var c);
            c.State = CreatureState.Moving;
            c.Path.Add(new CellPos(4, 2, 4));
            c.MoveProgress = 500;
            SimInterpolator.PoseMilli(c, out int x, out int y, out int z);
            Assert.AreEqual(3500, x); Assert.AreEqual(1500, y); Assert.AreEqual(4000, z);
        }

        [Test]
        public void PoseMilliIgnoresProgressWhenNotMoving()
        {
            NewState(out var c);
            c.State = CreatureState.Working;
            c.Path.Add(new CellPos(4, 2, 4));
            c.MoveProgress = 500;
            SimInterpolator.PoseMilli(c, out int x, out int y, out int z);
            Assert.AreEqual(3000, x); Assert.AreEqual(1000, y); Assert.AreEqual(4000, z);
        }

        [Test]
        public void JumpOverOneCellSnaps()
        {
            var st = NewState(out var c);
            var ip = new SimInterpolator();
            ip.Capture(st);
            c.Cell = new CellPos(5, 1, 4);
            ip.Capture(st);
            ip.Sample(0, 0.5, out double x, out _, out _);
            Assert.AreEqual(5.0, x, 1e-9);
            ip.Heading(0, out int hx, out int hz);
            Assert.AreEqual(0, hx); Assert.AreEqual(1, hz);
        }

        [Test]
        public void OneCellDropInterpolates()
        {
            var st = NewState(out var c);
            c.Cell = new CellPos(3, 2, 4);
            var ip = new SimInterpolator();
            ip.Capture(st);
            c.Cell = new CellPos(3, 1, 4);
            ip.Capture(st);
            ip.Sample(0, 0.5, out _, out double y, out _);
            Assert.AreEqual(1.5, y, 1e-9);
        }

        [Test]
        public void RespawnEventSnaps()
        {
            var st = NewState(out var c);
            var ip = new SimInterpolator();
            ip.Capture(st);
            c.Cell = new CellPos(4, 1, 4);
            ip.Capture(st);
            ip.ApplyEvents(new[] { new SimEvent(5, SimEventKind.CreatureRespawned, -1, 0, new CellPos(4, 1, 4), StallReason.None) });
            ip.Sample(0, 0.5, out double x, out _, out _);
            Assert.AreEqual(4.0, x, 1e-9);
        }

        [Test]
        public void LateCreatureStartsWithoutMotion()
        {
            var st = NewState(out _);
            var ip = new SimInterpolator();
            ip.Capture(st);
            st.Creatures.Add(new Creature(1, "L1", WorkAbility.Till, WorkAbility.Sow, 100, 100, 150, 1, 1, 1, new CellPos(7, 1, 7)));
            ip.Capture(st);
            Assert.AreEqual(2, ip.Count);
            ip.Sample(1, 0.5, out double x, out double y, out double z);
            Assert.AreEqual(7, x, 1e-9); Assert.AreEqual(1, y, 1e-9); Assert.AreEqual(7, z, 1e-9);
        }
    }
}
