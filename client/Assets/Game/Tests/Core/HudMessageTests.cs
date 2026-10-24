using NUnit.Framework;

namespace Game.Core.Tests
{
    public class HudMessageTests
    {
        [Test]
        public void HiddenBeforeAnyText()
        {
            Assert.IsFalse(new HudMessage(4f).Visible(0f));
        }

        [Test]
        public void ShowsThenHidesAfterSeconds()
        {
            var m = new HudMessage(4f);
            m.Update("a", 10f);
            Assert.IsTrue(m.Visible(10f));
            Assert.IsTrue(m.Visible(13.9f));
            Assert.IsFalse(m.Visible(14f));
        }

        [Test]
        public void SameTextDoesNotRestart()
        {
            var m = new HudMessage(4f);
            m.Update("a", 10f);
            m.Update("a", 13f);
            Assert.IsFalse(m.Visible(14.5f));
        }

        [Test]
        public void NewTextRestarts()
        {
            var m = new HudMessage(4f);
            m.Update("a", 10f);
            m.Update("b", 13f);
            Assert.IsTrue(m.Visible(16.5f));
        }

        [Test]
        public void EmptyHidden()
        {
            var m = new HudMessage(4f);
            m.Update("", 10f);
            Assert.IsFalse(m.Visible(10f));
            m.Update("x", 10f);
            m.Update(null, 11f);
            Assert.AreEqual("", m.Text);
        }
    }
}
