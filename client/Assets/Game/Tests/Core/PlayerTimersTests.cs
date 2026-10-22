using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class PlayerTimersTests
    {
        [Test]
        public void WorkTimerCompletesOnce()
        {
            var t = new WorkTimer(6f);
            t.Start();
            Assert.IsFalse(t.Advance(5.9f));
            Assert.IsTrue(t.Running);
            Assert.IsTrue(t.Advance(0.2f));
            Assert.AreEqual(6f, t.Elapsed);
            Assert.IsFalse(t.Running);
            Assert.IsFalse(t.Advance(1f));
        }

        [Test]
        public void WorkTimerCancelAndRestart()
        {
            var t = new WorkTimer(6f);
            t.Start();
            t.Advance(3f);
            t.Cancel();
            Assert.AreEqual(0f, t.Elapsed);
            Assert.IsFalse(t.Running);
            Assert.IsFalse(t.Advance(10f));
            t.Start();
            Assert.AreEqual(0f, t.Elapsed);
            Assert.IsTrue(t.Running);
        }

        [Test]
        public void CooldownCountsDown()
        {
            var c = new Cooldown(10f);
            Assert.IsTrue(c.Ready);
            Assert.AreEqual(0, c.RemainingWholeSeconds);
            c.Start();
            Assert.IsFalse(c.Ready);
            Assert.AreEqual(10, c.RemainingWholeSeconds);
            c.Advance(3.5f);
            Assert.AreEqual(7, c.RemainingWholeSeconds);
            c.Advance(6.5f);
            Assert.IsTrue(c.Ready);
            Assert.AreEqual(0f, c.Remaining);
            c.Advance(1f);
            Assert.AreEqual(0f, c.Remaining);
        }

        [Test]
        public void BadArgumentsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorkTimer(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cooldown(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorkTimer(1f).Advance(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cooldown(1f).Advance(-0.1f));
        }
    }
}
