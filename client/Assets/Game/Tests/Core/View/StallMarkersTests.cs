using Game.Core.Sim;
using Game.Core.View;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class StallMarkersTests
    {
        static SimEvent Ev(SimEventKind k, int stage, StallReason r) => new SimEvent(1, k, stage, -1, new CellPos(5, 1, 5), r);

        [Test]
        public void StartThenClear()
        {
            var m = new StallMarkers();
            m.ApplyEvents(new[] { Ev(SimEventKind.StallStarted, 3, StallReason.NoPath) });
            Assert.AreEqual(1, m.Count);
            Assert.IsTrue(m.TryGet(3, out var e));
            Assert.AreEqual(StallReason.NoPath, e.Reason);
            Assert.AreEqual(new CellPos(5, 1, 5), e.Cell);
            m.ApplyEvents(new[] { Ev(SimEventKind.StallCleared, 3, StallReason.None) });
            Assert.AreEqual(0, m.Count);
        }

        [Test]
        public void ReasonChangeReplaces()
        {
            var m = new StallMarkers();
            m.ApplyEvents(new[] { Ev(SimEventKind.StallStarted, 3, StallReason.NoSeeds), Ev(SimEventKind.StallStarted, 3, StallReason.StorageFull) });
            Assert.AreEqual(1, m.Count);
            Assert.IsTrue(m.TryGet(3, out var e));
            Assert.AreEqual(StallReason.StorageFull, e.Reason);
        }

        [Test]
        public void RespawnIgnored()
        {
            var m = new StallMarkers();
            m.ApplyEvents(new[] { new SimEvent(1, SimEventKind.CreatureRespawned, -1, 0, new CellPos(5, 1, 5), StallReason.None) });
            Assert.AreEqual(0, m.Count);
        }
    }
}
