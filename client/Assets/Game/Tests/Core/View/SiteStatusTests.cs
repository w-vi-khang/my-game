using Game.Core.Sim;
using Game.Core.View;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SiteStatusTests
    {
        [Test]
        public void StorageTextFormat()
        {
            var s = new Stockpile(2) { Capacity = 50, Reserved = 3 };
            s.Seeds[0] = 10;
            s.Produce[0] = 4;
            Assert.AreEqual("Kho · nông sản 4 · hạt 10 · 14/50", SiteStatus.StorageText(s));
        }

        [Test]
        public void PlotTextPerPhase()
        {
            var p = new FarmPlot(2, 0, new CellPos(5, 0, 5));
            const int growTicks = 1200;
            p.Phase = PlotPhase.Idle;
            Assert.AreEqual("Ruộng 2 · trống", SiteStatus.PlotText(p, 100, growTicks));
            p.Phase = PlotPhase.Till;
            Assert.AreEqual("Ruộng 2 · xới", SiteStatus.PlotText(p, 100, growTicks));
            p.Phase = PlotPhase.Sow;
            Assert.AreEqual("Ruộng 2 · gieo", SiteStatus.PlotText(p, 100, growTicks));
            p.Phase = PlotPhase.Water;
            Assert.AreEqual("Ruộng 2 · tưới", SiteStatus.PlotText(p, 100, growTicks));
            p.Phase = PlotPhase.Harvest;
            Assert.AreEqual("Ruộng 2 · chín", SiteStatus.PlotText(p, 100, growTicks));
            p.Phase = PlotPhase.Growing;
            p.RipeTick = 1300;
            Assert.AreEqual("Ruộng 2 · lớn 0%", SiteStatus.PlotText(p, 100, growTicks));
            Assert.AreEqual("Ruộng 2 · lớn 37%", SiteStatus.PlotText(p, 544, growTicks));
            Assert.AreEqual("Ruộng 2 · lớn 99%", SiteStatus.PlotText(p, 1299, growTicks));
            Assert.AreEqual("Ruộng 2 · lớn 100%", SiteStatus.PlotText(p, 1300, growTicks));
        }

        [Test]
        public void RemovedPlotShowsCancelled()
        {
            var p = new FarmPlot(2, 0, new CellPos(5, 0, 5));
            p.Removed = true;
            foreach (PlotPhase ph in System.Enum.GetValues(typeof(PlotPhase)))
            {
                p.Phase = ph;
                Assert.AreEqual("Ruộng 2 · đã huỷ", SiteStatus.PlotText(p, 100, 1200));
            }
        }

        [Test]
        public void GrowPercentClamps()
        {
            var p = new FarmPlot(2, 0, new CellPos(5, 0, 5));
            p.Phase = PlotPhase.Growing;
            p.RipeTick = 1300;
            Assert.AreEqual(0, SiteStatus.GrowPercent(p, 50, 1200));
            Assert.AreEqual(100, SiteStatus.GrowPercent(p, 2000, 1200));
        }

        [Test]
        public void IsCarryingOnlyHaulCarried()
        {
            var st = new SimState(new SimRng(1UL, 1UL));
            st.Stages.Add(new WorkStage(0, 0, WorkAbility.Haul, new CellPos(5, 1, 5), true, new CellPos(8, 1, 8), 20, 0));
            st.Stages.Add(new WorkStage(1, 0, WorkAbility.Till, new CellPos(5, 1, 5), false, default, 60, 0));
            var c = new Creature(0, "L1", WorkAbility.Till, WorkAbility.Sow, 100, 100, 150, 1, 1, 1, new CellPos(3, 1, 4));
            st.Creatures.Add(c);
            c.StageId = -1;
            Assert.IsFalse(SiteStatus.IsCarrying(st, c));
            c.StageId = 0;
            st.Stages[0].Carried = false;
            Assert.IsFalse(SiteStatus.IsCarrying(st, c));
            st.Stages[0].Carried = true;
            Assert.IsTrue(SiteStatus.IsCarrying(st, c));
            c.StageId = 1;
            st.Stages[1].Carried = true;
            Assert.IsFalse(SiteStatus.IsCarrying(st, c));
        }
    }
}
