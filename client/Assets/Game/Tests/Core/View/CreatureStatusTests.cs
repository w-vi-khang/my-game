using Game.Core.Sim;
using Game.Core.View;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class CreatureStatusTests
    {
        static SimState NewState(out Creature c, out WorkStage stage)
        {
            var st = new SimState(new SimRng(1UL, 1UL));
            c = new Creature(0, "L1", WorkAbility.Till, WorkAbility.Sow, 100, 100, 150, 1, 1, 1, new CellPos(3, 1, 4));
            st.Creatures.Add(c);
            stage = new WorkStage(0, 0, WorkAbility.Till, new CellPos(5, 1, 5), false, default, 60, 0);
            st.Stages.Add(stage);
            return st;
        }

        [Test]
        public void TextPerState()
        {
            var st = NewState(out var c, out var stage);
            c.State = CreatureState.Idle;
            Assert.AreEqual("L1 · rảnh", CreatureStatus.Text(ViText.Load(), st, c));
            c.State = CreatureState.Moving; c.StageId = -1;
            Assert.AreEqual("L1 · đi", CreatureStatus.Text(ViText.Load(), st, c));
            c.StageId = 0;
            Assert.AreEqual("L1 · đi → xới", CreatureStatus.Text(ViText.Load(), st, c));
            c.State = CreatureState.Working; stage.WorkDone = 27500;
            Assert.AreEqual("L1 · xới 45%", CreatureStatus.Text(ViText.Load(), st, c));
        }

        [Test]
        public void PercentRoundsDown()
        {
            var st = NewState(out var c, out var stage);
            c.State = CreatureState.Working; c.StageId = 0;
            stage.WorkDone = 0;
            Assert.AreEqual("L1 · xới 0%", CreatureStatus.Text(ViText.Load(), st, c));
            stage.WorkDone = 59999;
            Assert.AreEqual("L1 · xới 99%", CreatureStatus.Text(ViText.Load(), st, c));
            stage.WorkDone = 60000;
            Assert.AreEqual("L1 · xới 100%", CreatureStatus.Text(ViText.Load(), st, c));
        }

        [Test]
        public void ReasonTexts()
        {
            Assert.AreEqual("không có đường", CreatureStatus.ReasonText(ViText.Load(), StallReason.NoPath));
            Assert.AreEqual("hết hạt", CreatureStatus.ReasonText(ViText.Load(), StallReason.NoSeeds));
            Assert.AreEqual("kho đầy", CreatureStatus.ReasonText(ViText.Load(), StallReason.StorageFull));
            Assert.AreEqual("thiếu năng lực", CreatureStatus.ReasonText(ViText.Load(), StallReason.NoAbility));
            Assert.AreEqual("vận chuyển", CreatureStatus.AbilityName(ViText.Load(), WorkAbility.Haul));
            Assert.AreEqual("thu hoạch", CreatureStatus.AbilityName(ViText.Load(), WorkAbility.Harvest));
        }
    }
}
