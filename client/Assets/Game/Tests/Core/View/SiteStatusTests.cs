using Game.Core.Sim;
using Game.Core.Text;
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
            Assert.AreEqual("Kho · nông sản 4 · hạt 10 · 14/50", SiteStatus.StorageText(ViText.Load(), s));
        }

        [Test]
        public void CropNameUsesConfigId()
        {
            var cfg = new SimConfig();
            Assert.AreEqual("Cây A", SiteStatus.CropName(ViText.Load(), cfg, 0));
            Assert.AreEqual("Cây B", SiteStatus.CropName(ViText.Load(), cfg, 1));
        }

        [Test]
        public void CropNameUsesTable()
        {
            var t = new Strings(new StringFile { language = "x", entries = new[] { new StringEntry("crop.name", "Plant {0}") } }, ViText.Load());
            var cfg = new SimConfig();
            Assert.AreEqual("Plant A", SiteStatus.CropName(t, cfg, 0));
            Assert.AreEqual("Kho · nông sản 0 · hạt 0 · 0/50", SiteStatus.StorageText(t, new Stockpile(2) { Capacity = 50 }));
        }

        [Test]
        public void StockPanelTextFormat()
        {
            var s = new Stockpile(2) { Capacity = 100, Reserved = 5 };
            s.Seeds[0] = 9;
            s.Produce[0] = 4;
            s.Seeds[1] = 10;
            s.Produce[1] = 3;
            Assert.AreEqual("Kho chung · đã dùng 26/100\nCây A · nông sản 4 · hạt 9\nCây B · nông sản 3 · hạt 10",
                SiteStatus.StockPanelText(ViText.Load(), s, new SimConfig()));
        }

        [Test]
        public void SlotCountTextFormat()
        {
            var p = new FarmPlot(0, 0, new CellPos(0, 0, 0));
            p.SlotMax = 3;
            p.Slots.Add(new CropSlot(0, 2));
            Assert.AreEqual("2/3", SiteStatus.SlotCountText(p, 0));
            Assert.AreEqual("0/3", SiteStatus.SlotCountText(p, 1));
        }

        [Test]
        public void ProgressTextStates()
        {
            var cfg = new SimConfig();
            var st = new Simulation(cfg, TestWorlds.Flat(1)).State;
            var p = new FarmPlot(0, 0, new CellPos(0, 0, 0));
            p.StageId = -1;
            p.Phase = PlotPhase.Till;
            Assert.AreEqual("Xới 0%", SiteStatus.ProgressText(ViText.Load(), p, st, cfg));
            p.TillPaused = true;
            Assert.AreEqual("Xới 0% · tạm dừng", SiteStatus.ProgressText(ViText.Load(), p, st, cfg));
            p.TillPaused = false;
            p.CurrentCrop = 0;
            p.Phase = PlotPhase.Growing;
            st.Tick = 0;
            p.RipeTick = cfg.GrowTicks(0) / 2;
            Assert.AreEqual("Cây A · lớn 50%", SiteStatus.ProgressText(ViText.Load(), p, st, cfg));
            p.Phase = PlotPhase.Idle;
            Assert.AreEqual("Trống", SiteStatus.ProgressText(ViText.Load(), p, st, cfg));
            p.Removed = true;
            Assert.AreEqual("Đã huỷ", SiteStatus.ProgressText(ViText.Load(), p, st, cfg));
        }

        [Test]
        public void PlotTextPerPhase()
        {
            var p = new FarmPlot(2, 0, new CellPos(5, 0, 5));
            const int growTicks = 1200;
            p.Phase = PlotPhase.Idle;
            Assert.AreEqual("Ruộng 2 · trống", SiteStatus.PlotText(ViText.Load(), p, 100, growTicks));
            p.Phase = PlotPhase.Till;
            Assert.AreEqual("Ruộng 2 · xới", SiteStatus.PlotText(ViText.Load(), p, 100, growTicks));
            p.Phase = PlotPhase.Sow;
            Assert.AreEqual("Ruộng 2 · gieo", SiteStatus.PlotText(ViText.Load(), p, 100, growTicks));
            p.Phase = PlotPhase.Water;
            Assert.AreEqual("Ruộng 2 · tưới", SiteStatus.PlotText(ViText.Load(), p, 100, growTicks));
            p.Phase = PlotPhase.Harvest;
            Assert.AreEqual("Ruộng 2 · chín", SiteStatus.PlotText(ViText.Load(), p, 100, growTicks));
            p.Phase = PlotPhase.Growing;
            p.RipeTick = 1300;
            Assert.AreEqual("Ruộng 2 · lớn 0%", SiteStatus.PlotText(ViText.Load(), p, 100, growTicks));
            Assert.AreEqual("Ruộng 2 · lớn 37%", SiteStatus.PlotText(ViText.Load(), p, 544, growTicks));
            Assert.AreEqual("Ruộng 2 · lớn 99%", SiteStatus.PlotText(ViText.Load(), p, 1299, growTicks));
            Assert.AreEqual("Ruộng 2 · lớn 100%", SiteStatus.PlotText(ViText.Load(), p, 1300, growTicks));
        }

        [Test]
        public void RemovedPlotShowsCancelled()
        {
            var p = new FarmPlot(2, 0, new CellPos(5, 0, 5));
            p.Removed = true;
            foreach (PlotPhase ph in System.Enum.GetValues(typeof(PlotPhase)))
            {
                p.Phase = ph;
                Assert.AreEqual("Ruộng 2 · đã huỷ", SiteStatus.PlotText(ViText.Load(), p, 100, 1200));
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
