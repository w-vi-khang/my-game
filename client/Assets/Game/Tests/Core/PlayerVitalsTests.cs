using System;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class PlayerVitalsTests
    {
        static PlayerVitals New() => new PlayerVitals(new PlayerConfig());

        [Test]
        public void SpendMana()
        {
            var v = New();
            Assert.AreEqual(100, v.Hp);
            Assert.AreEqual(100, v.Mana);
            Assert.IsTrue(v.TrySpend(ManaAction.Dig));
            Assert.AreEqual(95, v.Mana);
            for (int i = 0; i < 6; i++) Assert.IsTrue(v.TrySpend(ManaAction.Rain));
            Assert.AreEqual(5, v.Mana);
            Assert.IsFalse(v.TrySpend(ManaAction.Rain));
            Assert.AreEqual(5, v.Mana);
            Assert.AreEqual(3, v.Cost(ManaAction.Place));
        }

        [Test]
        public void ManaRegenPerCell()
        {
            var v = New();
            v.TrySpend(ManaAction.Rain);
            Assert.AreEqual(85, v.Mana);
            v.AddMoved(0.6f);
            Assert.AreEqual(85, v.Mana);
            v.AddMoved(0.6f);
            Assert.AreEqual(87, v.Mana);
            v.AddMoved(10f);
            Assert.AreEqual(100, v.Mana);
            Assert.Throws<ArgumentOutOfRangeException>(() => v.AddMoved(-1f));
        }

        [Test]
        public void FallDamage()
        {
            var v = New();
            Assert.AreEqual(0, v.Land(4f));
            Assert.AreEqual(0, v.Land(4.9f));
            Assert.AreEqual(10, v.Land(5f));
            Assert.AreEqual(20, v.Land(6.5f));
            Assert.AreEqual(70, v.Hp);
            Assert.Throws<ArgumentOutOfRangeException>(() => v.Land(-0.1f));
        }

        [Test]
        public void DrownDamage()
        {
            var v = New();
            v.Tick(999, true);
            Assert.AreEqual(100, v.Hp);
            v.Tick(1, true);
            Assert.AreEqual(95, v.Hp);
            v.Tick(500, true);
            v.Tick(100, false);
            v.Tick(500, true);
            Assert.AreEqual(95, v.Hp);
        }

        [Test]
        public void HpRegenAfterDelay()
        {
            var v = New();
            v.Land(5f);
            Assert.AreEqual(90, v.Hp);
            v.Tick(5000, false);
            Assert.AreEqual(90, v.Hp);
            v.Tick(1000, false);
            Assert.AreEqual(91, v.Hp);
            v.Tick(3000, false);
            Assert.AreEqual(94, v.Hp);
            v.Land(6f);
            Assert.AreEqual(74, v.Hp);
            v.Tick(5999, false);
            Assert.AreEqual(74, v.Hp);
            v.Tick(1, false);
            Assert.AreEqual(75, v.Hp);
        }

        [Test]
        public void DiesOnceAndRespawns()
        {
            var v = New();
            int died = 0, changed = 0;
            v.Died += () => died++;
            v.Changed += () => changed++;
            v.Land(20f);
            Assert.AreEqual(0, v.Hp);
            Assert.AreEqual(1, died);
            v.Land(20f);
            Assert.AreEqual(1, died);
            Assert.IsFalse(v.TrySpend(ManaAction.Dig));
            int c = changed;
            v.AddMoved(5f);
            v.Tick(10000, false);
            v.Tick(10000, true);
            Assert.AreEqual(c, changed);
            Assert.AreEqual(0, v.Hp);
            Assert.AreEqual(100, v.Mana);
            v.Respawn();
            Assert.AreEqual(100, v.Hp);
            Assert.AreEqual(100, v.Mana);
            Assert.IsFalse(v.IsDead);
            Assert.Greater(changed, c);
        }

        [Test]
        public void InvalidArgs()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerVitals(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => New().Tick(-1, false));
        }
    }
}
