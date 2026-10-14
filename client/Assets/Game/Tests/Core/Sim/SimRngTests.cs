using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SimRngTests
    {
        [Test]
        public void MatchesPcgReferenceSequence()
        {
            var r = new SimRng(42, 54);
            uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], r.NextUInt(), "số thứ " + i);
        }

        [Test]
        public void SameSeedSameSequence()
        {
            var a = new SimRng(20261014, 1);
            var b = new SimRng(20261014, 1);
            for (int i = 0; i < 1000; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void NextIntStaysInRange()
        {
            var r = new SimRng(20261014, 1);
            var seen = new bool[7];
            for (int i = 0; i < 10000; i++)
            {
                int v = r.NextInt(7);
                Assert.That(v, Is.InRange(0, 6));
                seen[v] = true;
            }
            for (int i = 0; i < 7; i++) Assert.IsTrue(seen[i], "thiếu giá trị " + i);
        }
    }
}
